using System.Diagnostics;

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
        Assert.Contains("FeedbackReportStatuses = Object.freeze([\"new\", \"reviewed\", \"needs_information\", \"processing\", \"resolved\", \"rejected\"])", AdminJs);
        Assert.Contains("FeedbackReportCategories = Object.freeze([\"suggestion\", \"app_issue\", \"ai_response\", \"account_deletion\"])", AdminJs);
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
        Assert.Contains("[\"reviewed\", \"needs_information\", \"processing\", \"resolved\", \"rejected\"].includes(targetStatus)", AdminJs);
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
    public void AdminScriptParsesSoLoginAndFeedbackHandlersCanInitialize()
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "node",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            ArgumentList = { "--check", Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js") }
        });

        Assert.NotNull(process);
        process!.WaitForExit();
        var standardError = process.StandardError.ReadToEnd();
        Assert.True(process.ExitCode == 0, standardError);
    }

    [Fact]
    public void AccountDeletionRequestsAreFilterableAndShowTheirReasonAsPlainText()
    {
        Assert.Contains("<option value=\"account_deletion\">Account deletion request</option>", AdminIndex);
        Assert.Contains("account_deletion: \"Account deletion request\"", AdminJs);
        Assert.Contains("report?.category === \"account_deletion\" ? \"Deletion reason\" : \"Report message\"", AdminJs);
        Assert.Contains("String(report?.message || \"No reason provided.\")", AdminJs);
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

    [Fact]
    public void AccountAnonymizationPreflightPanelIsPermissionGatedAndUsesOnlyApprovedRoutes()
    {
        Assert.Contains("accountAnonymizationPreflightRead: \"account_anonymization.preflight.read\"", AdminJs);
        Assert.Contains("accountAnonymizationStatusTemplate: \"/api/admin/feedback-reports/{reportId}/account-anonymization\"", AdminJs);
        Assert.Contains("accountAnonymizationPreflightTemplate: \"/api/admin/feedback-reports/{reportId}/account-anonymization/preflight\"", AdminJs);
        Assert.Contains("report?.category === \"account_deletion\" && hasAdminPermission(AdminPermissionIds.accountAnonymizationPreflightRead)", AdminJs);
        Assert.Contains("async function loadAccountAnonymizationPreflight", AdminJs);
        Assert.Contains("method: \"GET\"", AdminJs);
        Assert.Contains("account_anonymization_preflight_not_found", AdminJs);
        Assert.Contains("Preflight has not been run.", AdminJs);
    }

    [Fact]
    public void PreflightActionsAndExecutionControlsArePlainTextAndPermissionGated()
    {
        var selectionLoad = AdminJs.Substring(AdminJs.IndexOf("async function loadFeedbackReportDetails"), AdminJs.IndexOf("async function adminFetch") - AdminJs.IndexOf("async function loadFeedbackReportDetails"));
        var preflightRenderer = AdminJs.Substring(AdminJs.IndexOf("function renderAccountAnonymizationPreflight"), AdminJs.IndexOf("function updateFeedbackReportReplyLength") - AdminJs.IndexOf("function renderAccountAnonymizationPreflight"));
        var preflightRequest = AdminJs.Substring(AdminJs.IndexOf("async function runAccountAnonymizationPreflight"), AdminJs.IndexOf("async function loadFeedbackReportDetails") - AdminJs.IndexOf("async function runAccountAnonymizationPreflight"));

        Assert.DoesNotContain("method: \"POST\"", selectionLoad);
        Assert.Contains("JSON.stringify({ refresh })", preflightRequest);
        Assert.Contains("runAccountAnonymizationPreflight(preflight ? true : false)", preflightRenderer);
        Assert.Contains("status === \"resolved\" || status === \"rejected\"", preflightRenderer);
        Assert.Contains("action.disabled = terminal || operationState === \"executing\" || operationState === \"completed\"", preflightRenderer);
        Assert.Contains("textContent", preflightRenderer);
        Assert.DoesNotContain("innerHTML", preflightRenderer);
        Assert.Contains("hasAdminPermission(AdminPermissionIds.accountAnonymizationExecute)", preflightRenderer);
        Assert.Contains("status === \"processing\"", preflightRenderer);
        Assert.Contains("account_anonymization_active_premium", preflightRenderer);
        Assert.Contains("Refresh the preflight before deleting the account", preflightRenderer);
        Assert.Contains("execute.disabled = executeUnavailable", preflightRenderer);
        Assert.Contains("account_anonymization_admin_cms_dependency_unclassified", preflightRenderer);
        Assert.Contains("linked to Admin CMS", preflightRenderer);
    }

    [Fact]
    public void AccountDeletionStatusAndPaidPeriodGuidanceAvoidFalseResolveOrPremiumSignals()
    {
        var statusRenderer = AdminJs.Substring(AdminJs.IndexOf("function renderFeedbackReportActions"), AdminJs.IndexOf("function applyFeedbackReportMutation") - AdminJs.IndexOf("function renderFeedbackReportActions"));
        var paidGuidance = AdminJs.Substring(AdminJs.IndexOf("function appendPaidPeriodGuidance"), AdminJs.IndexOf("const renderAuditLog") - AdminJs.IndexOf("function appendPaidPeriodGuidance"));

        Assert.Contains("accountDeletion && !anonymizationCompleted", statusRenderer);
        Assert.Contains("targetStatus !== \"resolved\"", statusRenderer);
        Assert.Contains("Resolved automatically after successful deletion", statusRenderer);
        Assert.Contains("Reopen as reviewed", statusRenderer);
        Assert.Contains("hasActivePaidProviderSubscription !== true", paidGuidance);
        Assert.Contains("paidAccessUntilUtc", paidGuidance);
        Assert.Contains("Cancellation may already be scheduled", paidGuidance);
        Assert.DoesNotContain("paddle", paidGuidance, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AccountAnonymizationExecutionUsesOneExplicitConfirmationAndExactContract()
    {
        var execution = AdminJs.Substring(AdminJs.IndexOf("function showAccountAnonymizationConfirmation"), AdminJs.IndexOf("function clearAccountAnonymizationPreflight") - AdminJs.IndexOf("function showAccountAnonymizationConfirmation"));

        Assert.Contains("accountAnonymizationExecute: \"account_anonymization.execute\"", AdminJs);
        Assert.Contains("accountAnonymizationExecuteTemplate: \"/api/admin/feedback-reports/{reportId}/account-anonymization/execute\"", AdminJs);
        Assert.Contains("dialog.showModal()", execution);
        Assert.Contains("Cancel", execution);
        Assert.Contains("Delete account permanently", execution);
        Assert.Contains("operationId: preflight.operationId, preflightFingerprint: preflight.preflightFingerprint", execution);
        Assert.Contains("feedbackReportsState.executionRequestPending", execution);
        Assert.Contains("feedbackReportsState.selectedReportId !== reportId", execution);
        Assert.Contains("await loadFeedbackReports()", execution);
        Assert.Contains("await loadFeedbackReportDetails(reportId)", execution);
        Assert.Contains("textContent", execution);
        Assert.DoesNotContain("innerHTML", execution);
        Assert.DoesNotContain("password", execution, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("typed", execution, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("checkbox", execution, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("second", execution, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("paddle", execution, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("billing", execution, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refund", execution, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cancellation", execution, StringComparison.OrdinalIgnoreCase);
    }
}
