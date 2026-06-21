using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminRoleAssignmentSafetyService(
    AppDbContext dbContext,
    IAdminRolePermissionCatalogService adminRolePermissionCatalogService) : IAdminRoleAssignmentSafetyService
{
    private const string ActiveStatus = "active";
    private const string OwnerRoleId = "owner";

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IAdminRolePermissionCatalogService _adminRolePermissionCatalogService = adminRolePermissionCatalogService;

    public async Task<AdminRoleAssignmentSafetyCheckResult> ValidateAssignRoleAsync(
        AdminRoleAssignmentSafetyCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var violations = ValidateSharedRoleChangeRequirements(request, requireKnownRole: true);
        var target = await GetAdminUserSafetyStateAsync(request.TargetAdminUserId, cancellationToken);

        if (target is null)
        {
            violations = AppendViolation(violations, "Target admin user does not exist.");
        }
        else if (target.IsDisabled)
        {
            violations = AppendViolation(violations, "Cannot assign a role to a disabled admin user.");
        }

        return BuildResult(violations, "admin_role_assignment_assign_denied", "Admin role assignment safety check denied assigning the role.");
    }

    public async Task<AdminRoleAssignmentSafetyCheckResult> ValidateRevokeRoleAsync(
        AdminRoleAssignmentSafetyCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var violations = ValidateSharedRoleChangeRequirements(request, requireKnownRole: true);
        var target = await GetAdminUserSafetyStateAsync(request.TargetAdminUserId, cancellationToken);

        if (target is null)
        {
            violations = AppendViolation(violations, "Target admin user does not exist.");
        }
        else if (IsSuperAdminRole(request.RoleId) && await IsLastActiveSuperAdminAsync(request.TargetAdminUserId, cancellationToken))
        {
            violations = AppendViolation(violations, "Cannot revoke SuperAdmin from the last active SuperAdmin.");
        }

        return BuildResult(violations, "admin_role_assignment_revoke_denied", "Admin role assignment safety check denied revoking the role.");
    }

    public async Task<AdminRoleAssignmentSafetyCheckResult> ValidateDisableAdminAsync(
        AdminRoleAssignmentSafetyCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var violations = ValidateSharedActorAndReasonRequirements(request);
        var target = await GetAdminUserSafetyStateAsync(request.TargetAdminUserId, cancellationToken);

        if (target is null)
        {
            violations = AppendViolation(violations, "Target admin user does not exist.");
        }
        else if (target.HasActiveSuperAdminRole && await IsLastActiveSuperAdminAsync(request.TargetAdminUserId, cancellationToken))
        {
            violations = AppendViolation(violations, "Cannot disable the last active SuperAdmin.");
        }

        return BuildResult(violations, "admin_role_assignment_disable_denied", "Admin role assignment safety check denied disabling the admin user.");
    }

    private IReadOnlyList<string> ValidateSharedRoleChangeRequirements(
        AdminRoleAssignmentSafetyCheckRequest request,
        bool requireKnownRole)
    {
        var violations = ValidateSharedActorAndReasonRequirements(request);

        if (requireKnownRole && !IsKnownProductionRole(request.RoleId))
        {
            violations = AppendViolation(violations, "Role id is not a known production Admin role.");
        }

        if (IsElevatedRole(request.RoleId) && !CanManageRoles(request.ActorRoleIds))
        {
            violations = AppendViolation(violations, "Only Owner or SuperAdmin actors may grant elevated Admin roles.");
        }

        return violations;
    }

    private IReadOnlyList<string> ValidateSharedActorAndReasonRequirements(AdminRoleAssignmentSafetyCheckRequest request)
    {
        IReadOnlyList<string> violations = Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            violations = AppendViolation(violations, "A non-empty human-readable reason is required.");
        }

        if (!CanManageRoles(request.ActorRoleIds))
        {
            violations = AppendViolation(violations, "Only Owner or SuperAdmin actors may manage Admin roles.");
        }

        return violations;
    }

    private bool IsKnownProductionRole(string? roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return false;
        }

        return _adminRolePermissionCatalogService
            .GetProductionRolePermissions()
            .ContainsKey(roleId.Trim());
    }

    private static bool CanManageRoles(IReadOnlyList<string> actorRoleIds)
    {
        return actorRoleIds.Any(roleId =>
            string.Equals(roleId, AdminRoleConstants.SuperAdmin, StringComparison.Ordinal) ||
            string.Equals(roleId, OwnerRoleId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsElevatedRole(string? roleId)
    {
        return IsSuperAdminRole(roleId) || string.Equals(roleId, OwnerRoleId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSuperAdminRole(string? roleId)
    {
        return string.Equals(roleId, AdminRoleConstants.SuperAdmin, StringComparison.Ordinal);
    }

    private async Task<AdminUserSafetyState?> GetAdminUserSafetyStateAsync(
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AdminUsers
            .AsNoTracking()
            .Where(adminUser => adminUser.Id == adminUserId)
            .Select(adminUser => new AdminUserSafetyState(
                adminUser.Id,
                adminUser.DisabledAtUtc.HasValue || adminUser.Status != ActiveStatus,
                adminUser.RoleAssignments.Any(role =>
                    role.RoleId == AdminRoleConstants.SuperAdmin &&
                    role.RevokedAtUtc == null)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> IsLastActiveSuperAdminAsync(
        Guid targetAdminUserId,
        CancellationToken cancellationToken)
    {
        var activeSuperAdminIds = await _dbContext.AdminUserRoles
            .AsNoTracking()
            .Where(role => role.RoleId == AdminRoleConstants.SuperAdmin && role.RevokedAtUtc == null)
            .Where(role => role.AdminUser.Status == ActiveStatus && role.AdminUser.DisabledAtUtc == null)
            .Select(role => role.AdminUserId)
            .Distinct()
            .Take(2)
            .ToListAsync(cancellationToken);

        return activeSuperAdminIds.Count == 1 && activeSuperAdminIds[0] == targetAdminUserId;
    }

    private static AdminRoleAssignmentSafetyCheckResult BuildResult(
        IReadOnlyList<string> violations,
        string errorCode,
        string message)
    {
        return violations.Count == 0
            ? AdminRoleAssignmentSafetyCheckResult.Allowed()
            : AdminRoleAssignmentSafetyCheckResult.Denied(errorCode, message, violations);
    }

    private static IReadOnlyList<string> AppendViolation(IReadOnlyList<string> violations, string violation)
    {
        return violations.Concat([violation]).ToArray();
    }

    private sealed record AdminUserSafetyState(
        Guid AdminUserId,
        bool IsDisabled,
        bool HasActiveSuperAdminRole);
}
