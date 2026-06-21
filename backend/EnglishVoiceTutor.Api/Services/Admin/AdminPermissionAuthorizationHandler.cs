using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminPermissionRequirement(string permissionName) : IAuthorizationRequirement
{
    public string PermissionName { get; } = permissionName;
}

public sealed class AdminPermissionAuthorizationHandler(
    IBootstrapAdminAccessService bootstrapAdminAccessService,
    IAdminRoleAssignmentReadService adminRoleAssignmentReadService,
    IAdminRolePermissionCatalogService adminRolePermissionCatalogService) : AuthorizationHandler<AdminPermissionRequirement>
{
    private readonly IBootstrapAdminAccessService _bootstrapAdminAccessService = bootstrapAdminAccessService;
    private readonly IAdminRoleAssignmentReadService _adminRoleAssignmentReadService = adminRoleAssignmentReadService;
    private readonly IAdminRolePermissionCatalogService _adminRolePermissionCatalogService = adminRolePermissionCatalogService;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminPermissionRequirement requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement.PermissionName))
        {
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (await HasPersistentRolePermissionAsync(context, requirement.PermissionName))
        {
            context.Succeed(requirement);
            return;
        }

        if (!_bootstrapAdminAccessService.IsBootstrapAdmin(context.User))
        {
            return;
        }

        if (_adminRolePermissionCatalogService.GetBootstrapAdminPermissions()
            .Contains(requirement.PermissionName, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
        }
    }

    private async Task<bool> HasPersistentRolePermissionAsync(
        AuthorizationHandlerContext context,
        string permissionName)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(context.User);
        if (userId.HasValue)
        {
            var userIdResult = await _adminRoleAssignmentReadService.GetEffectiveRolesByUserIdAsync(userId.Value);
            if (HasRequiredPermission(userIdResult, permissionName))
            {
                return true;
            }

            if (userIdResult.IsDisabled)
            {
                return false;
            }
        }

        var normalizedEmail = NormalizeEmail(ClaimsUserAccessor.TryGetUserEmail(context.User));
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return false;
        }

        var emailResult = await _adminRoleAssignmentReadService.GetEffectiveRolesByNormalizedEmailAsync(normalizedEmail);
        return HasRequiredPermission(emailResult, permissionName);
    }

    private bool HasRequiredPermission(AdminRoleAssignmentReadResult readResult, string permissionName)
    {
        if (readResult is not { IsAdminUserFound: true, IsDisabled: false } || readResult.RoleIds.Count == 0)
        {
            return false;
        }

        var productionRolePermissions = _adminRolePermissionCatalogService.GetProductionRolePermissions();
        return readResult.RoleIds.Any(roleId =>
            productionRolePermissions.TryGetValue(roleId, out var permissions)
            && permissions.Contains(permissionName, StringComparer.Ordinal));
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }
}
