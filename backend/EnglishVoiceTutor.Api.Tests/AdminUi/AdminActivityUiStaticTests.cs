using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Tests.AdminUi;

public sealed class AdminActivityUiStaticTests
{
    private static readonly string AdminJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));
    private static readonly string AdminIndex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/index.html"));
    private static readonly string AdminCss = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.css"));
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
        Assert.Contains("<option value=\"admin_auth_audit_events\">admin_auth_audit_events</option>", AdminIndex);
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

    [Fact]
    public void AdminActivityFiltersUseResponsiveWrappingGrid()
    {
        Assert.Contains("audit-controls admin-activity-controls", AdminIndex);
        Assert.Contains(".admin-activity-controls", AdminCss);
        Assert.Contains("display: grid", AdminCss);
        Assert.Contains("repeat(auto-fit, minmax(170px, 1fr))", AdminCss);
        Assert.Contains("max-width: 100%", AdminCss);
        Assert.DoesNotContain("admin-activity-controls { align-items: end; display: flex", AdminCss);
    }

    [Fact]
    public void AdminActivityTableIncludesAdminNoteColumn()
    {
        Assert.Contains("key: \"adminNote\", label: \"Admin note\"", AdminJs);
        Assert.Contains("admin-note-cell", AdminJs);
        Assert.Contains(".admin-note-cell", AdminCss);
        Assert.Contains("min-width: 360px", AdminCss);
        Assert.Contains("max-width: 520px", AdminCss);
        Assert.Contains("overflow-wrap: anywhere", AdminCss);
        Assert.Contains("white-space: normal", AdminCss);
        Assert.Contains("word-break: break-word", AdminCss);
    }

    [Fact]
    public void AdminActivityTableKeepsAdminNoteAndSafeMetadataSeparate()
    {
        Assert.Contains("{ key: \"adminNote\", label: \"Admin note\", className: \"admin-note-cell\" }, \"safeMetadataJson\"", AdminJs);
        Assert.DoesNotContain("safeMetadataJson: item.adminNote", AdminJs);
        Assert.Contains("\"safeMetadataJson\"", AdminJs);
    }

    [Fact]
    public void AdminActivityTableHasSynchronizedTopHorizontalScrollbar()
    {
        Assert.Contains("const ActivityTableOptions = Object.freeze({ wrapClassName: \"table-wrap admin-activity-table-wrapper\", topScroll: true })", AdminJs);
        Assert.Contains("admin-activity-top-scroll", AdminJs);
        Assert.Contains("admin-activity-top-scroll-inner", AdminJs);
        Assert.Contains("function syncAdminActivityTopScroll(topScroll, tableWrap, topScrollInner)", AdminJs);
        Assert.Contains("topScrollInner.style.width = `${tableWrap.scrollWidth}px`", AdminJs);
        Assert.Contains("tableWrap.scrollLeft = topScroll.scrollLeft", AdminJs);
        Assert.Contains("topScroll.scrollLeft = tableWrap.scrollLeft", AdminJs);
        Assert.Contains("window.requestAnimationFrame(updateTopScrollWidth)", AdminJs);
        Assert.Contains("new ResizeObserver(updateTopScrollWidth)", AdminJs);
        Assert.Contains("ActivityTableOptions", AdminJs);
        Assert.Contains(".admin-activity-table-wrapper", AdminCss);
        Assert.Contains(".admin-activity-top-scroll", AdminCss);
        Assert.Contains(".admin-activity-top-scroll-inner", AdminCss);
    }
}
