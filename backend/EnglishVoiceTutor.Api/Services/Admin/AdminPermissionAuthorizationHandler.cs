using Microsoft.AspNetCore.Authorization;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminPermissionRequirement(string permissionName) : IAuthorizationRequirement
{
    public string PermissionName { get; } = permissionName;
}

public sealed class AdminPermissionAuthorizationHandler(
    IBootstrapAdminAccessService bootstrapAdminAccessService,
    IAdminRolePermissionCatalogService adminRolePermissionCatalogService) : AuthorizationHandler<AdminPermissionRequirement>
{
    private readonly IBootstrapAdminAccessService _bootstrapAdminAccessService = bootstrapAdminAccessService;
    private readonly IAdminRolePermissionCatalogService _adminRolePermissionCatalogService = adminRolePermissionCatalogService;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminPermissionRequirement requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement.PermissionName))
        {
            return Task.CompletedTask;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (!_bootstrapAdminAccessService.IsBootstrapAdmin(context.User))
        {
            return Task.CompletedTask;
        }

        if (_adminRolePermissionCatalogService.GetBootstrapAdminPermissions()
            .Contains(requirement.PermissionName, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
