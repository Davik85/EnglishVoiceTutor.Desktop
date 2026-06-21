using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminRoleAssignmentBootstrapService(
    AppDbContext dbContext,
    IAdminRoleAssignmentAuditService auditService) : IAdminRoleAssignmentBootstrapService
{
    private const string ActiveStatus = "active";
    private const string InitialOwnerRoleId = AdminRoleConstants.SuperAdmin;

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IAdminRoleAssignmentAuditService _auditService = auditService;

    public async Task<AdminRoleAssignmentBootstrapResult> BootstrapFirstOwnerAsync(
        AdminRoleAssignmentBootstrapRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var occurredAtUtc = DateTimeOffset.UtcNow;
        var normalizedReason = string.IsNullOrWhiteSpace(request.ActorReason) ? null : request.ActorReason.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(request.NormalizedEmail) ? null : request.NormalizedEmail.Trim();

        if (request.AppUserId == Guid.Empty)
        {
            return Denied("admin_role_assignment_bootstrap_app_user_required", "Authenticated app user id is required.", null, occurredAtUtc);
        }

        if (normalizedReason is null)
        {
            return Denied("admin_role_assignment_bootstrap_reason_required", "A non-empty bootstrap reason is required.", null, occurredAtUtc);
        }

        var existingOwnerAdminUserId = await _dbContext.AdminUserRoles
            .Where(role => role.RoleId == InitialOwnerRoleId && role.RevokedAtUtc == null)
            .Where(role => role.AdminUser.Status == ActiveStatus && role.AdminUser.DisabledAtUtc == null)
            .Select(role => (Guid?)role.AdminUserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingOwnerAdminUserId.HasValue)
        {
            return await DeniedWithAuditAsync(
                "admin_role_assignment_bootstrap_owner_exists",
                "An active persistent owner-equivalent Admin user already exists.",
                existingOwnerAdminUserId.Value,
                normalizedReason,
                request.SafeMetadataJson,
                cancellationToken);
        }

        var sameAppUserAdminUsers = await _dbContext.AdminUsers
            .Include(adminUser => adminUser.RoleAssignments)
            .Where(adminUser => adminUser.UserId == request.AppUserId)
            .ToListAsync(cancellationToken);

        var disabledSameAppUser = sameAppUserAdminUsers.FirstOrDefault(adminUser =>
            adminUser.DisabledAtUtc.HasValue || !string.Equals(adminUser.Status, ActiveStatus, StringComparison.Ordinal));
        if (disabledSameAppUser is not null)
        {
            return await DeniedWithAuditAsync(
                "admin_role_assignment_bootstrap_disabled_mapping_exists",
                "A disabled or inactive Admin user mapping already exists for this authenticated app user.",
                disabledSameAppUser.Id,
                normalizedReason,
                request.SafeMetadataJson,
                cancellationToken);
        }

        var activeSameAppUserWithRoles = sameAppUserAdminUsers.FirstOrDefault(adminUser =>
            string.Equals(adminUser.Status, ActiveStatus, StringComparison.Ordinal) &&
            adminUser.DisabledAtUtc == null &&
            adminUser.RoleAssignments.Any(role => role.RevokedAtUtc == null));
        if (activeSameAppUserWithRoles is not null)
        {
            return await DeniedWithAuditAsync(
                "admin_role_assignment_bootstrap_active_mapping_has_roles",
                "An active Admin user mapping with active roles already exists for this authenticated app user.",
                activeSameAppUserWithRoles.Id,
                normalizedReason,
                request.SafeMetadataJson,
                cancellationToken);
        }

        if (normalizedEmail is not null)
        {
            var differentActiveEmailAdminUserId = await _dbContext.AdminUsers
                .Where(adminUser => adminUser.NormalizedEmail == normalizedEmail)
                .Where(adminUser => adminUser.UserId != request.AppUserId)
                .Where(adminUser => adminUser.Status == ActiveStatus && adminUser.DisabledAtUtc == null)
                .Select(adminUser => (Guid?)adminUser.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (differentActiveEmailAdminUserId.HasValue)
            {
                return await DeniedWithAuditAsync(
                    "admin_role_assignment_bootstrap_email_conflict",
                    "The normalized email maps to a different active Admin user.",
                    differentActiveEmailAdminUserId.Value,
                    normalizedReason,
                    request.SafeMetadataJson,
                    cancellationToken);
            }
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var adminUser = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            UserId = request.AppUserId,
            NormalizedEmail = normalizedEmail,
            Status = ActiveStatus,
            CreatedAtUtc = occurredAtUtc,
            UpdatedAtUtc = occurredAtUtc
        };
        adminUser.CreatedByAdminUserId = adminUser.Id;

        var roleAssignment = new AdminUserRoleEntity
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUser.Id,
            RoleId = InitialOwnerRoleId,
            AssignedAtUtc = occurredAtUtc,
            AssignedByAdminUserId = adminUser.Id,
            Reason = normalizedReason
        };

        await _dbContext.AdminUsers.AddAsync(adminUser, cancellationToken);
        await _dbContext.AdminUserRoles.AddAsync(roleAssignment, cancellationToken);

        var auditResult = await _auditService.AppendAuditEventAsync(new AdminRoleAssignmentAuditRequest(
            adminUser.Id,
            adminUser.Id,
            AdminRoleAssignmentAuditConstants.ActionTypes.FirstOwnerBootstrap,
            InitialOwnerRoleId,
            normalizedReason,
            null,
            [InitialOwnerRoleId],
            AdminRoleAssignmentAuditConstants.Results.Succeeded,
            request.SafeMetadataJson), cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new AdminRoleAssignmentBootstrapResult(
            true,
            null,
            "First persistent owner-equivalent Admin user mapping created.",
            adminUser.Id,
            InitialOwnerRoleId,
            auditResult.EventId,
            auditResult.OccurredAtUtc);
    }

    private static AdminRoleAssignmentBootstrapResult Denied(string errorCode, string message, Guid? adminUserId, DateTimeOffset occurredAtUtc) => new(
        false,
        errorCode,
        message,
        adminUserId,
        InitialOwnerRoleId,
        null,
        occurredAtUtc);

    private async Task<AdminRoleAssignmentBootstrapResult> DeniedWithAuditAsync(
        string errorCode,
        string message,
        Guid targetAdminUserId,
        string reason,
        string? safeMetadataJson,
        CancellationToken cancellationToken)
    {
        var auditResult = await _auditService.AppendAuditEventAsync(new AdminRoleAssignmentAuditRequest(
            null,
            targetAdminUserId,
            AdminRoleAssignmentAuditConstants.ActionTypes.ValidationDenied,
            InitialOwnerRoleId,
            reason,
            null,
            null,
            AdminRoleAssignmentAuditConstants.Results.FailedValidation,
            safeMetadataJson), cancellationToken);

        return new AdminRoleAssignmentBootstrapResult(
            false,
            errorCode,
            message,
            targetAdminUserId,
            InitialOwnerRoleId,
            auditResult.EventId,
            auditResult.OccurredAtUtc);
    }
}
