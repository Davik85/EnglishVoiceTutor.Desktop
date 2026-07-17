namespace EnglishVoiceTutor.Api.Tests.AdminUi;

public sealed class AdminFeedbackReportsUiStaticTests
{
    private static readonly string AdminJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));
    private static readonly string AdminIndex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/index.html"));

    [Fact]
    public void FeedbackReportsTabUsesReadPermission()
    {
        Assert.Contains("Feedback &amp; reports", AdminIndex);
        Assert.Contains("data-tab-id=\"feedback-reports\"", AdminIndex);
        Assert.Contains("feedbackReportsRead: \"feedback_reports.read\"", AdminJs);
        Assert.Contains("[Tabs.feedbackReports]: { anyPermissions: [AdminPermissionIds.feedbackReportsRead] }", AdminJs);
    }

    [Fact]
    public void FeedbackReportsUseReadAndMutationPathsWithIndependentPermissions()
    {
        Assert.Contains("feedbackReports: \"/api/admin/feedback-reports\"", AdminJs);
        Assert.Contains("feedbackReportTemplate: \"/api/admin/feedback-reports/{reportId}\"", AdminJs);
        Assert.Contains("adminFetch(`${ApiPaths.feedbackReports}?${query.toString()}`)", AdminJs);
        Assert.Contains("adminFetch(path)", AdminJs);
        Assert.Contains("feedbackReportsStatusManage: \"feedback_reports.status.manage\"", AdminJs);
        Assert.Contains("feedbackReportsReply: \"feedback_reports.reply\"", AdminJs);
        Assert.Contains("feedbackReportStatusTemplate: \"/api/admin/feedback-reports/{reportId}/status\"", AdminJs);
        Assert.Contains("feedbackReportRepliesTemplate: \"/api/admin/feedback-reports/{reportId}/replies\"", AdminJs);
        Assert.Contains("method: \"PATCH\"", AdminJs);
        Assert.Contains("method: \"POST\"", AdminJs);
        Assert.Contains("JSON.stringify({ status: targetStatus })", AdminJs);
        Assert.Contains("JSON.stringify({ replyText })", AdminJs);
    }

    [Fact]
    public void FiltersUseOnlySupportedBackendValues()
    {
        Assert.Contains("FeedbackReportStatuses = Object.freeze([\"new\", \"reviewed\", \"resolved\"])", AdminJs);
        Assert.Contains("FeedbackReportCategories = Object.freeze([\"suggestion\", \"app_issue\", \"ai_response\"])", AdminJs);
        Assert.Contains("FeedbackReportStatuses.includes(feedbackReportsStatusFilter.value)", AdminJs);
        Assert.Contains("FeedbackReportCategories.includes(feedbackReportsCategoryFilter.value)", AdminJs);
        Assert.Contains("pageSize: String(FeedbackReportPageSize)", AdminJs);
    }

    [Fact]
    public void ReportContentIsPlainTextAndRemainsInMemoryOnly()
    {
        Assert.Contains("content.textContent = value;", AdminJs);
        Assert.Contains("feedbackReportsState = { page: 1, totalCount: 0, items: [], selectedReportId: null, selectedReport: null", AdminJs);
        Assert.DoesNotContain("localStorage.setItem(\"feedback", AdminJs);
        Assert.DoesNotContain("sessionStorage.setItem(\"feedback", AdminJs);
        Assert.Contains("feedbackReportReplyTextInput.value = \"\"", AdminJs);
        Assert.DoesNotContain("innerHTML", AdminJs.Substring(AdminJs.IndexOf("function appendFeedbackReportDetail"), AdminJs.IndexOf("async function adminFetch") - AdminJs.IndexOf("function appendFeedbackReportDetail")));
    }

    [Fact]
    public void FeedbackReportMutationControlsCoverSafeStates()
    {
        Assert.Contains("[\"reviewed\", \"resolved\"].includes(targetStatus)", AdminJs);
        Assert.Contains("button.disabled = feedbackReportsState.statusRequestPending", AdminJs);
        Assert.Contains("feedbackReportReplyTextInput.disabled = unavailable || feedbackReportsState.replyRequestPending", AdminJs);
        Assert.Contains("response.status === HttpStatus.forbidden", AdminJs);
        Assert.Contains("response.status === HttpStatus.unauthorized", AdminJs);
        Assert.Contains("response.status === HttpStatus.serviceUnavailable", AdminJs);
        Assert.Contains("recipient_email_unavailable", AdminJs);
        Assert.Contains("feedbackReportReplyTextInput.value = \"\"", AdminJs);
        Assert.Contains("feedbackReportReplyTextInput.value.trim()", AdminJs);
        Assert.Contains("clearFeedbackReportDetails()", AdminJs);
        var detailsLoad = AdminJs.Substring(AdminJs.IndexOf("async function loadFeedbackReportDetails"), AdminJs.IndexOf("async function adminFetch") - AdminJs.IndexOf("async function loadFeedbackReportDetails"));
        Assert.DoesNotContain("method: \"PATCH\"", detailsLoad);
        Assert.DoesNotContain("method: \"POST\"", detailsLoad);
    }

    [Fact]
    public void ReplyHistoryUsesReadPermissionAndSafePlainTextRendering()
    {
        Assert.Contains("id=\"feedback-report-reply-history\"", AdminIndex);
        Assert.Contains("Reply history", AdminIndex);
        Assert.Contains("const canRead = hasAdminPermission(AdminPermissionIds.feedbackReportsRead);", AdminJs);
        Assert.Contains("feedbackReportReplyHistoryElement.classList.toggle(\"hidden\", !canRead);", AdminJs);
        var historyRenderer = AdminJs.Substring(AdminJs.IndexOf("function renderFeedbackReportReplyHistory"), AdminJs.IndexOf("function renderFeedbackReportDetails") - AdminJs.IndexOf("function renderFeedbackReportReplyHistory"));
        Assert.DoesNotContain("feedbackReportsReply", historyRenderer);
        Assert.Contains("recipient.textContent", historyRenderer);
        Assert.Contains("text.textContent", historyRenderer);
        Assert.DoesNotContain("innerHTML", historyRenderer);
        Assert.Contains("No replies have been sent yet", AdminJs);
        Assert.Contains("email_not_configured", AdminJs);
        Assert.Contains("email_delivery_failed", AdminJs);
        Assert.DoesNotContain("localStorage.setItem(\"feedback", AdminJs);
        Assert.DoesNotContain("sessionStorage.setItem(\"feedback", AdminJs);
        Assert.DoesNotContain("Retry reply", AdminJs);
        Assert.DoesNotContain("Delete reply", AdminJs);
        Assert.DoesNotContain("Edit reply", AdminJs);
    }

    [Fact]
    public void ReplyOutcomesRefreshSelectedDetailsAndFailedDraftIsPreserved()
    {
        var replyFlow = AdminJs.Substring(AdminJs.IndexOf("async function sendFeedbackReportReply"), AdminJs.IndexOf("async function loadFeedbackReportDetails") - AdminJs.IndexOf("async function sendFeedbackReportReply"));
        Assert.Contains("await loadFeedbackReportDetails(reportId, true);", replyFlow);
        Assert.Contains("await loadFeedbackReportDetails(reportId);", replyFlow);
        Assert.Contains("if (!preserveReplyDraft) { feedbackReportReplyTextInput.value = \"\"; }", AdminJs);
    }

    [Fact]
    public void ExistingCmsPermissionsRemainUnchanged()
    {
        Assert.Contains("[Tabs.cmsContent]: { anyPermissions: [AdminPermissionIds.cmsContentRead] }", AdminJs);
        Assert.Contains("[Tabs.website]: { bootstrapAdminOnly: true }", AdminJs);
        Assert.Contains("[Tabs.roleManagement]: { anyPermissions: [AdminPermissionIds.adminRolesManage] }", AdminJs);
    }
}
