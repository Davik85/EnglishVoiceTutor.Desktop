using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Tests.AdminUi;

public sealed class AdminActivityUiStaticTests
{
    private static readonly string AdminJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));
    private static readonly string AdminIndex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/index.html"));
    private static readonly string ApiConstantsSource = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/Constants/ApiConstants.cs"));
    private static readonly string AdminEndpointsSource = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs"));

    [Fact]
    public void EndpointExistsAndUsesAuditReadPolicy()
    {
        Assert.Contains("/api/admin/activity", ApiConstantsSource);
        Assert.Contains("ApiConstants.AdminActivityRoute", AdminEndpointsSource);
        Assert.Contains("AuditLogViewPermissionPolicyName", AdminEndpointsSource);
    }

    [Fact]
    public void AdminActivityTabIsAuditReadOnly()
    {
        Assert.Contains("Admin Activity", AdminIndex);
        Assert.Contains("data-tab-id=\"admin-activity\"", AdminIndex);
        Assert.Contains("[Tabs.adminActivity]: { anyPermissions: [AdminPermissionIds.auditRead] }", AdminJs);
        Assert.Contains("/api/admin/activity", AdminJs);
        Assert.Contains("if (response.status === HttpStatus.unauthorized) { handleAuthInvalidResponse(); }", AdminJs);
        Assert.Contains("if (response.status === HttpStatus.forbidden) { throw new Error(NotAvailableForRoleMessage); }", AdminJs);
        Assert.Contains("function isAuthErrorMessage(message) { return message === ErrorMessages.signInAgain || message === ErrorMessages.accessDenied || message === ErrorMessages.sessionExpired; }", AdminJs);
    }

    [Fact]
    public void ExistingTargetUserAuditLogRemainsPresent()
    {
        Assert.Contains("/api/admin/users/{userId}/audit-actions", AdminJs);
        Assert.Contains("Audit Log", AdminIndex);
    }
}
