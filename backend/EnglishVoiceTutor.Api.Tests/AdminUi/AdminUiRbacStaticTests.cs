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
    public void BillingSupportPremiumGrantAndSupportDiagnosticsMatchRoleCatalog()
    {
        Assert.Contains("premiumGrant: \"premium.grant\"", AdminJs);
        Assert.Contains("{ label: \"Premium Grant\", statusWhenAvailable: \"available\", anyPermissions: [AdminPermissionIds.premiumGrant] }", AdminJs);
        Assert.Contains("setGrantVisible(Boolean(selectedUserId) && hasAdminPermission(AdminPermissionIds.premiumGrant));", AdminJs);
        Assert.Contains("{ label: \"User Diagnostics\", statusWhenAvailable: \"read-only / available\", anyPermissions: [AdminPermissionIds.usersDiagnosticsRead] }", AdminJs);
        Assert.Contains("{ label: \"Billing Diagnostics\", statusWhenAvailable: \"read-only / available\", anyPermissions: [AdminPermissionIds.billingDiagnosticsRead] }", AdminJs);

        var catalog = new AdminRolePermissionCatalogService();
        var billingSupportPermissions = catalog.GetProductionRolePermissions()[AdminRoleConstants.BillingSupport];
        Assert.Contains(AdminPermissionConstants.UserLookupRead, billingSupportPermissions);
        Assert.Contains(AdminPermissionConstants.UserOverviewRead, billingSupportPermissions);
        Assert.Contains(AdminPermissionConstants.SubscriptionsDiagnosticsRead, billingSupportPermissions);
        Assert.Contains(AdminPermissionConstants.PremiumDiagnosticsRead, billingSupportPermissions);
        Assert.Contains(AdminPermissionConstants.BillingDiagnosticsRead, billingSupportPermissions);
        Assert.Contains(AdminPermissionConstants.PremiumGrant, billingSupportPermissions);
        Assert.DoesNotContain(AdminPermissionConstants.PremiumRevoke, billingSupportPermissions);
        Assert.DoesNotContain(AdminPermissionConstants.AdminRolesManage, billingSupportPermissions);
        Assert.DoesNotContain(AdminPermissionConstants.SystemAiModelSettingsManage, billingSupportPermissions);
        Assert.DoesNotContain(AdminPermissionConstants.CmsContentWriteDraft, billingSupportPermissions);
        Assert.DoesNotContain(AdminPermissionConstants.CmsContentPublish, billingSupportPermissions);
        Assert.DoesNotContain(AdminPermissionConstants.CmsContentRestore, billingSupportPermissions);

        var supportPermissions = catalog.GetProductionRolePermissions()[AdminRoleConstants.Support];
        Assert.Contains(AdminPermissionConstants.UserLookupRead, supportPermissions);
        Assert.Contains(AdminPermissionConstants.UserOverviewRead, supportPermissions);
        Assert.Contains(AdminPermissionConstants.FreeLessonAllowanceReset, supportPermissions);
        Assert.Contains(AdminPermissionConstants.UsersDiagnosticsRead, supportPermissions);
        Assert.Contains(AdminPermissionConstants.LessonHistoryDiagnosticsRead, supportPermissions);
        Assert.Contains(AdminPermissionConstants.AuditRead, supportPermissions);
        Assert.DoesNotContain(AdminPermissionConstants.PremiumGrant, supportPermissions);
        Assert.DoesNotContain(AdminPermissionConstants.PremiumRevoke, supportPermissions);
        Assert.DoesNotContain(AdminPermissionConstants.BillingCancelRenewal, supportPermissions);
        Assert.DoesNotContain(AdminPermissionConstants.AdminRolesManage, supportPermissions);
        Assert.DoesNotContain(AdminPermissionConstants.SystemAiModelSettingsManage, supportPermissions);
    }

    [Fact]
    public void LoginWordingMentionsLinkedPersistentAdminUserRoles()
    {
        Assert.Contains("linked persistent Admin User role", AdminIndex);
    }
}
