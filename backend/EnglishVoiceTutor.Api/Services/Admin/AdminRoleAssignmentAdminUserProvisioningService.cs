using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminRoleAssignmentAdminUserProvisioningService(
    AppDbContext dbContext,
    IAdminRoleAssignmentAuditService auditService) : IAdminRoleAssignmentAdminUserProvisioningService
{
    private const string ActiveStatus = "active";
    private const string OwnerRoleId = "owner";

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IAdminRoleAssignmentAuditService _auditService = auditService;

    public async Task<AdminRoleAssignmentAdminUserProvisioningResult> ProvisionAdminUserAsync(
        AdminRoleAssignmentAdminUserProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var occurredAtUtc = DateTimeOffset.UtcNow;
        var normalizedReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(request.TargetNormalizedEmail) ? null : request.TargetNormalizedEmail.Trim();

        if (request.ActorAdminUserId == Guid.Empty)
        {
            return Denied("admin_user_provisioning_actor_required", "Actor admin user id is required.", null, occurredAtUtc);
        }

        if (request.ActorRoleIds is null || request.ActorRoleIds.Count == 0)
        {
            return await DeniedWithAuditAsync(request, "admin_user_provisioning_actor_roles_required", "Actor role ids are required.", request.ActorAdminUserId, normalizedReason, cancellationToken);
        }

        if (!CanProvisionAdminUsers(request.ActorRoleIds))
        {
            return await DeniedWithAuditAsync(request, "admin_user_provisioning_actor_not_owner", "Only Owner or SuperAdmin actors may provision persistent Admin user mappings.", request.ActorAdminUserId, normalizedReason, cancellationToken);
        }

        if (request.TargetAppUserId == Guid.Empty)
        {
            return await DeniedWithAuditAsync(request, "admin_user_provisioning_target_app_user_required", "Target app user id is required.", request.ActorAdminUserId, normalizedReason, cancellationToken);
        }

        if (normalizedReason is null)
        {
            return await DeniedWithAuditAsync(request, "admin_user_provisioning_reason_required", "A non-empty provisioning reason is required.", request.ActorAdminUserId, normalizedReason, cancellationToken);
        }

        var targetAppUserExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == request.TargetAppUserId, cancellationToken);
        if (!targetAppUserExists)
        {
            return await DeniedWithAuditAsync(request, "admin_user_provisioning_target_app_user_not_found", "Target app user does not exist.", request.ActorAdminUserId, normalizedReason, cancellationToken);
        }

        var existingTargetAdminUsers = await _dbContext.AdminUsers
            .AsNoTracking()
            .Where(adminUser => adminUser.UserId == request.TargetAppUserId)
            .Select(adminUser => new ExistingAdminUser(adminUser.Id, adminUser.Status, adminUser.DisabledAtUtc))
            .ToListAsync(cancellationToken);

        var activeExistingTarget = existingTargetAdminUsers.FirstOrDefault(IsActive);
        if (activeExistingTarget is not null)
        {
            return await DeniedWithAuditAsync(request, "admin_user_provisioning_active_mapping_exists", "An active Admin user mapping already exists for the target app user.", activeExistingTarget.Id, normalizedReason, cancellationToken);
        }

        var inactiveExistingTarget = existingTargetAdminUsers.FirstOrDefault(adminUser => !IsActive(adminUser));
        if (inactiveExistingTarget is not null)
        {
            return await DeniedWithAuditAsync(request, "admin_user_provisioning_inactive_mapping_exists", "A disabled or inactive Admin user mapping already exists for the target app user.", inactiveExistingTarget.Id, normalizedReason, cancellationToken);
        }

        if (normalizedEmail is not null)
        {
            var conflictingAdminUserId = await _dbContext.AdminUsers
                .AsNoTracking()
                .Where(adminUser => adminUser.NormalizedEmail == normalizedEmail)
                .Where(adminUser => adminUser.UserId != request.TargetAppUserId)
                .Where(adminUser => adminUser.Status == ActiveStatus && adminUser.DisabledAtUtc == null)
                .Select(adminUser => (Guid?)adminUser.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (conflictingAdminUserId.HasValue)
            {
                return await DeniedWithAuditAsync(request, "admin_user_provisioning_email_conflict", "The normalized email maps to a different active Admin user.", conflictingAdminUserId.Value, normalizedReason, cancellationToken);
            }
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var adminUser = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            UserId = request.TargetAppUserId,
            NormalizedEmail = normalizedEmail,
            Status = ActiveStatus,
            CreatedAtUtc = occurredAtUtc,
            UpdatedAtUtc = occurredAtUtc,
            CreatedByAdminUserId = request.ActorAdminUserId
        };

        await _dbContext.AdminUsers.AddAsync(adminUser, cancellationToken);

        var auditResult = await _auditService.AppendAuditEventAsync(new AdminRoleAssignmentAuditRequest(
            request.ActorAdminUserId,
            adminUser.Id,
            AdminRoleAssignmentAuditConstants.ActionTypes.AdminUserProvisioned,
            null,
            normalizedReason,
            null,
            null,
            AdminRoleAssignmentAuditConstants.Results.Succeeded,
            request.SafeMetadataJson), cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new AdminRoleAssignmentAdminUserProvisioningResult(
            true,
            null,
            "Persistent Admin user mapping provisioned without assigning roles.",
            adminUser.Id,
            auditResult.EventId,
            auditResult.OccurredAtUtc);
    }

    private static bool CanProvisionAdminUsers(IReadOnlyList<string>? actorRoleIds)
    {
        return actorRoleIds is not null && actorRoleIds.Any(roleId =>
            string.Equals(roleId, AdminRoleConstants.SuperAdmin, StringComparison.Ordinal) ||
            string.Equals(roleId, OwnerRoleId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsActive(ExistingAdminUser adminUser)
    {
        return string.Equals(adminUser.Status, ActiveStatus, StringComparison.Ordinal) && adminUser.DisabledAtUtc is null;
    }

    private static AdminRoleAssignmentAdminUserProvisioningResult Denied(string errorCode, string message, Guid? adminUserId, DateTimeOffset occurredAtUtc) => new(
        false,
        errorCode,
        message,
        adminUserId,
        null,
        occurredAtUtc);

    private async Task<AdminRoleAssignmentAdminUserProvisioningResult> DeniedWithAuditAsync(
        AdminRoleAssignmentAdminUserProvisioningRequest request,
        string errorCode,
        string message,
        Guid targetAdminUserId,
        string? normalizedReason,
        CancellationToken cancellationToken)
    {
        var auditResult = await _auditService.AppendAuditEventAsync(new AdminRoleAssignmentAuditRequest(
            request.ActorAdminUserId == Guid.Empty ? null : request.ActorAdminUserId,
            targetAdminUserId,
            AdminRoleAssignmentAuditConstants.ActionTypes.AdminUserProvisioningDenied,
            null,
            normalizedReason ?? message,
            null,
            null,
            AdminRoleAssignmentAuditConstants.Results.FailedValidation,
            request.SafeMetadataJson), cancellationToken);

        return new AdminRoleAssignmentAdminUserProvisioningResult(
            false,
            errorCode,
            message,
            targetAdminUserId,
            auditResult.EventId,
            auditResult.OccurredAtUtc);
    }

    private sealed record ExistingAdminUser(Guid Id, string Status, DateTimeOffset? DisabledAtUtc);
}
