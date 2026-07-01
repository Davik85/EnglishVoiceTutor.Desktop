using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Services.Admin;

namespace EnglishVoiceTutor.Api.Tests.AdminUi;

public sealed class AdminUiRbacStaticTests
{
    private static readonly string AdminJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));
    private static readonly string AdminIndex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/index.html"));

    [Fact]
    public void ForbiddenResponsesDoNotInvalidateOrDeleteAdminSession()
    {
        Assert.DoesNotContain("unauthorized || response.status === HttpStatus.forbidden", AdminJs);
        Assert.DoesNotContain("response.status === HttpStatus.forbidden) { handleAuthInvalidResponse();", AdminJs);
        Assert.Contains("if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); }", AdminJs);
        Assert.Contains("Not available for this role.", AdminJs);
    }

    [Fact]
    public void UnauthorizedResponsesStillInvalidateAdminSession()
    {
        Assert.Contains("if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }", AdminJs);
        Assert.Contains("fetch(ApiPaths.adminSession, {", AdminJs);
        Assert.Contains("method: \"DELETE\"", AdminJs);
    }

    [Fact]
    public void UserLookupAvailabilityMatchesEndpointPermissionsAndSupportRole()
    {
        Assert.Contains("userLookupRead: \"users.lookup.read\"", AdminJs);
        Assert.Contains("userOverviewRead: \"users.overview.read\"", AdminJs);
        Assert.Contains("[Tabs.userLookup]: { allPermissions: [AdminPermissionIds.userLookupRead, AdminPermissionIds.userOverviewRead] }", AdminJs);
        Assert.Contains("User Lookup", AdminJs);
        Assert.Contains("anyPermissions: [AdminPermissionIds.userLookupRead, AdminPermissionIds.userOverviewRead]", AdminJs);

        var catalog = new AdminRolePermissionCatalogService();
        var supportPermissions = catalog.GetProductionRolePermissions()[AdminRoleConstants.Support];
        Assert.Contains(AdminPermissionConstants.UserLookupRead, supportPermissions);
        Assert.Contains(AdminPermissionConstants.UserOverviewRead, supportPermissions);
    }

    [Fact]
    public void SupportRoleTabsAreRoleAwareForSuperAdminOnlyWorkflows()
    {
        Assert.Contains("const TabPermissionDefinitions = Object.freeze", AdminJs);
        Assert.Contains("[Tabs.roleManagement]: { anyPermissions: [AdminPermissionIds.adminRolesManage] }", AdminJs);
        Assert.Contains("[Tabs.system]: { anyPermissions: [AdminPermissionIds.systemAiModelSettingsManage] }", AdminJs);
        Assert.Contains("[Tabs.website]: { bootstrapAdminOnly: true }", AdminJs);
        Assert.Contains("button.disabled = !canUseTab;", AdminJs);
        Assert.Contains("button.classList.toggle(\"hidden\", !canUseTab);", AdminJs);
    }

    [Fact]
    public void LoginWordingMentionsLinkedPersistentAdminUserRoles()
    {
        Assert.Contains("linked persistent Admin User role", AdminIndex);
    }
}
