using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminRoleAssignmentWriteService(
    AppDbContext dbContext,
    IAdminRoleAssignmentSafetyService safetyService,
    IAdminRoleAssignmentAuditService auditService) : IAdminRoleAssignmentWriteService
{
    private const string ActiveStatus = "active";

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IAdminRoleAssignmentSafetyService _safetyService = safetyService;
    private readonly IAdminRoleAssignmentAuditService _auditService = auditService;

    public async Task<AdminRoleAssignmentWriteResult> AssignRoleAsync(
        AdminRoleAssignmentWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var safetyResult = await _safetyService.ValidateAssignRoleAsync(ToSafetyRequest(request), cancellationToken);
        if (!safetyResult.IsAllowed)
        {
            return await ReturnDeniedAsync(request, AdminRoleAssignmentAuditConstants.ActionTypes.AssignRole, safetyResult, cancellationToken);
        }

        var target = await _dbContext.AdminUsers
            .SingleOrDefaultAsync(adminUser => adminUser.Id == request.TargetAdminUserId, cancellationToken);
        if (target is null || target.DisabledAtUtc.HasValue || !string.Equals(target.Status, ActiveStatus, StringComparison.Ordinal))
        {
            return await ReturnConflictAsync(request, AdminRoleAssignmentAuditConstants.ActionTypes.AssignRole, "admin_role_assignment_target_unavailable", "Target admin user does not exist or is disabled.", cancellationToken);
        }

        var activeRoleExists = await _dbContext.AdminUserRoles.AnyAsync(
            role => role.AdminUserId == request.TargetAdminUserId &&
                role.RoleId == request.RoleId &&
                role.RevokedAtUtc == null,
            cancellationToken);
        if (activeRoleExists)
        {
            return await ReturnConflictAsync(request, AdminRoleAssignmentAuditConstants.ActionTypes.AssignRole, "admin_role_assignment_duplicate_active_role", "Target admin user already has an active assignment for this role.", cancellationToken);
        }

        var occurredAtUtc = DateTimeOffset.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var roleAssignment = new AdminUserRoleEntity
        {
            Id = Guid.NewGuid(),
            AdminUserId = request.TargetAdminUserId,
            RoleId = request.RoleId!,
            AssignedAtUtc = occurredAtUtc,
            AssignedByAdminUserId = request.ActorAdminUserId,
            Reason = request.Reason!.Trim()
        };
        await _dbContext.AdminUserRoles.AddAsync(roleAssignment, cancellationToken);

        var auditResult = await _auditService.AppendAuditEventAsync(new AdminRoleAssignmentAuditRequest(
            request.ActorAdminUserId,
            request.TargetAdminUserId,
            AdminRoleAssignmentAuditConstants.ActionTypes.AssignRole,
            request.RoleId,
            request.Reason,
            null,
            [request.RoleId!],
            AdminRoleAssignmentAuditConstants.Results.Succeeded,
            request.SafeMetadataJson), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return Success(request, auditResult, "Admin role assignment created.");
    }

    public async Task<AdminRoleAssignmentWriteResult> RevokeRoleAsync(
        AdminRoleAssignmentWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var safetyResult = await _safetyService.ValidateRevokeRoleAsync(ToSafetyRequest(request), cancellationToken);
        if (!safetyResult.IsAllowed)
        {
            return await ReturnDeniedAsync(request, AdminRoleAssignmentAuditConstants.ActionTypes.RevokeRole, safetyResult, cancellationToken);
        }

        if (!await _dbContext.AdminUsers.AnyAsync(adminUser => adminUser.Id == request.TargetAdminUserId, cancellationToken))
        {
            return await ReturnConflictAsync(request, AdminRoleAssignmentAuditConstants.ActionTypes.RevokeRole, "admin_role_assignment_target_not_found", "Target admin user does not exist.", cancellationToken);
        }

        var activeRole = await _dbContext.AdminUserRoles
            .SingleOrDefaultAsync(role => role.AdminUserId == request.TargetAdminUserId && role.RoleId == request.RoleId && role.RevokedAtUtc == null, cancellationToken);
        if (activeRole is null)
        {
            return await ReturnConflictAsync(request, AdminRoleAssignmentAuditConstants.ActionTypes.RevokeRole, "admin_role_assignment_active_role_not_found", "Target admin user does not have an active assignment for this role.", cancellationToken);
        }

        var occurredAtUtc = DateTimeOffset.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        activeRole.RevokedAtUtc = occurredAtUtc;
        activeRole.RevokedByAdminUserId = request.ActorAdminUserId;
        activeRole.RevokeReason = request.Reason!.Trim();

        var auditResult = await _auditService.AppendAuditEventAsync(new AdminRoleAssignmentAuditRequest(
            request.ActorAdminUserId,
            request.TargetAdminUserId,
            AdminRoleAssignmentAuditConstants.ActionTypes.RevokeRole,
            request.RoleId,
            request.Reason,
            [request.RoleId!],
            null,
            AdminRoleAssignmentAuditConstants.Results.Succeeded,
            request.SafeMetadataJson), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return Success(request, auditResult, "Admin role assignment revoked.");
    }

    public async Task<AdminRoleAssignmentWriteResult> DisableAdminAsync(
        AdminRoleAssignmentWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var safetyResult = await _safetyService.ValidateDisableAdminAsync(ToSafetyRequest(request with { RoleId = null }), cancellationToken);
        if (!safetyResult.IsAllowed)
        {
            return await ReturnDeniedAsync(request, AdminRoleAssignmentAuditConstants.ActionTypes.DisableAdmin, safetyResult, cancellationToken);
        }

        var target = await _dbContext.AdminUsers.SingleOrDefaultAsync(adminUser => adminUser.Id == request.TargetAdminUserId, cancellationToken);
        if (target is null)
        {
            return await ReturnConflictAsync(request, AdminRoleAssignmentAuditConstants.ActionTypes.DisableAdmin, "admin_role_assignment_target_not_found", "Target admin user does not exist.", cancellationToken);
        }

        if (target.DisabledAtUtc.HasValue || !string.Equals(target.Status, ActiveStatus, StringComparison.Ordinal))
        {
            return await ReturnConflictAsync(request, AdminRoleAssignmentAuditConstants.ActionTypes.DisableAdmin, "admin_role_assignment_target_already_disabled", "Target admin user is already disabled or inactive.", cancellationToken);
        }

        var occurredAtUtc = DateTimeOffset.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        target.Status = "disabled";
        target.DisabledAtUtc = occurredAtUtc;
        target.UpdatedAtUtc = occurredAtUtc;

        var auditResult = await _auditService.AppendAuditEventAsync(new AdminRoleAssignmentAuditRequest(
            request.ActorAdminUserId,
            request.TargetAdminUserId,
            AdminRoleAssignmentAuditConstants.ActionTypes.DisableAdmin,
            null,
            request.Reason,
            null,
            null,
            AdminRoleAssignmentAuditConstants.Results.Succeeded,
            request.SafeMetadataJson), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return Success(request with { RoleId = null }, auditResult, "Admin user disabled.");
    }

    private static AdminRoleAssignmentSafetyCheckRequest ToSafetyRequest(AdminRoleAssignmentWriteRequest request) => new(
        request.ActorAdminUserId,
        request.TargetAdminUserId,
        request.RoleId,
        request.ActorRoleIds,
        request.Reason);

    private async Task<AdminRoleAssignmentWriteResult> ReturnDeniedAsync(AdminRoleAssignmentWriteRequest request, string actionType, AdminRoleAssignmentSafetyCheckResult safetyResult, CancellationToken cancellationToken)
    {
        var auditResult = await _auditService.AppendAuditEventAsync(new AdminRoleAssignmentAuditRequest(
            request.ActorAdminUserId,
            request.TargetAdminUserId,
            AdminRoleAssignmentAuditConstants.ActionTypes.ValidationDenied,
            request.RoleId,
            request.Reason ?? safetyResult.Message,
            null,
            null,
            AdminRoleAssignmentAuditConstants.Results.FailedValidation,
            request.SafeMetadataJson), cancellationToken);

        return new AdminRoleAssignmentWriteResult(false, safetyResult.ErrorCode, safetyResult.Message, auditResult.EventId, request.TargetAdminUserId, request.RoleId, auditResult.OccurredAtUtc);
    }

    private async Task<AdminRoleAssignmentWriteResult> ReturnConflictAsync(AdminRoleAssignmentWriteRequest request, string actionType, string errorCode, string message, CancellationToken cancellationToken)
    {
        var auditResult = await _auditService.AppendAuditEventAsync(new AdminRoleAssignmentAuditRequest(
            request.ActorAdminUserId,
            request.TargetAdminUserId,
            actionType,
            request.RoleId,
            request.Reason ?? message,
            null,
            null,
            AdminRoleAssignmentAuditConstants.Results.FailedConflict,
            request.SafeMetadataJson), cancellationToken);

        return new AdminRoleAssignmentWriteResult(false, errorCode, message, auditResult.EventId, request.TargetAdminUserId, request.RoleId, auditResult.OccurredAtUtc);
    }

    private static AdminRoleAssignmentWriteResult Success(AdminRoleAssignmentWriteRequest request, AdminRoleAssignmentAuditResult auditResult, string message) => new(
        true,
        null,
        message,
        auditResult.EventId,
        request.TargetAdminUserId,
        request.RoleId,
        auditResult.OccurredAtUtc);
}
