namespace EnglishVoiceTutor.Api.Tests.AdminUi;

public sealed class AdminHealthStatusUiStaticTests
{
    private static readonly string AdminIndex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/index.html"));
    private static readonly string AdminJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));
    private static readonly string AdminCss = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.css"));

    [Fact]
    public void HeaderContainsFourIndicatorsAndManualRefresh()
    {
        var header = Slice(AdminIndex, "<header class=\"dashboard-header\">", "</header>");
        foreach (var id in new[] { "health-backend", "health-database", "health-cms-runtime", "health-ai-config" })
        {
            Assert.Contains($"id=\"{id}\"", header);
        }
        Assert.Contains("id=\"health-checked-at\"", header);
        Assert.Contains("id=\"health-refresh-button\"", header);
        Assert.Contains("Backend/API", header);
        Assert.Contains("Database", header);
        Assert.Contains("CMS Runtime", header);
        Assert.Contains("AI Config", header);
    }

    [Fact]
    public void RefreshUsesOnlyExistingReadOnlyEndpointsAndIndependentResults()
    {
        var refresh = Slice(AdminJs, "async function refreshHealthStatus()", "function startHealthStatusPolling()");
        Assert.Contains("health: \"/api/health\"", AdminJs);
        Assert.Contains("databaseHealth: \"/api/health/database\"", AdminJs);
        Assert.Contains("cmsRuntimeStatus: \"/api/admin/dev/cms/runtime-status\"", AdminJs);
        Assert.Contains("backendConfigStatus: \"/api/backend/config-status\"", AdminJs);
        Assert.Contains("method: \"GET\"", Slice(AdminJs, "async function readHealthPayload(", "async function refreshHealthStatus()"));
        Assert.Contains("Promise.allSettled(updates)", refresh);
        Assert.Contains("catch (_) { status = { state: \"unhealthy\", label: \"Failed\" }; }", refresh);
        Assert.Contains("payload?.status === \"Healthy\"", refresh);
        Assert.Contains("payload?.canConnect === true", refresh);
    }

    [Fact]
    public void CmsPermissionAndFallbackHaveDistinctSafeStates()
    {
        var refresh = Slice(AdminJs, "async function refreshHealthStatus()", "function startHealthStatusPolling()");
        Assert.Contains("hasAdminPermission(AdminPermissionIds.cmsRuntimeStatusRead)", refresh);
        Assert.Contains("Promise.resolve({ state: \"unknown\", label: \"Not available\" })", refresh);
        Assert.Contains("payload?.success !== true", refresh);
        Assert.Contains("payload?.fallbackUsed === true ? { state: \"warning\"", refresh);
        Assert.Contains("payload?.validationSuccess === true ? { state: \"healthy\"", refresh);
        Assert.DoesNotContain("payload?.message", refresh);
        Assert.DoesNotContain("payload?.errors", refresh);
    }

    [Fact]
    public void AiIndicatorOnlyReadsConfiguration()
    {
        var refresh = Slice(AdminJs, "async function refreshHealthStatus()", "function startHealthStatusPolling()");
        Assert.Contains("readHealthPayload(ApiPaths.backendConfigStatus, controller.signal)", refresh);
        Assert.Contains("payload?.openAiStatus === \"configured\"", refresh);
        Assert.Contains("payload?.openAiStatus === \"not_configured\" ? { state: \"warning\"", refresh);
        Assert.DoesNotContain("ApiPaths.aiModelSettingsProviderTest", refresh);
        Assert.DoesNotContain("testAiModelProviderAccess", refresh);
    }

    [Fact]
    public void PollingFollowsAuthenticatedSessionAndVisibility()
    {
        var polling = Slice(AdminJs, "function startHealthStatusPolling()", "function hasAnyAdminPermission(");
        var shell = Slice(AdminJs, "async function showAdminShellAfterAuth(", "async function restoreAdminSessionFromCookie(");
        var reset = Slice(AdminJs, "function resetDashboard()", "function resetSession()");
        Assert.Contains("setInterval(() =>", polling);
        Assert.Contains("}, 30000)", polling);
        Assert.Contains("if (healthRefreshInFlight) { healthRefreshQueued = true; return; }", AdminJs);
        Assert.Contains("startHealthStatusPolling();", shell);
        Assert.Contains("setDashboardVisible(true);", shell);
        Assert.Contains("stopHealthStatusPolling();", reset);
        Assert.Contains("document.addEventListener(\"visibilitychange\"", AdminJs);
        Assert.Contains("if (document.visibilityState === \"visible\")", AdminJs);
        Assert.Contains("healthRefreshButton.addEventListener(\"click\", () => { void refreshHealthStatus(); });", AdminJs);
    }

    [Fact]
    public void StatusUsesPlainTextAndHasResponsiveStylesWithoutPersistence()
    {
        var healthCode = Slice(AdminJs, "function renderHealthIndicator(", "function hasAnyAdminPermission(");
        Assert.Contains("indicator.querySelector(\".health-state\").textContent = label", healthCode);
        Assert.Contains("healthCheckedAtElement.textContent", healthCode);
        Assert.DoesNotContain("innerHTML", healthCode);
        Assert.DoesNotContain("localStorage", healthCode);
        Assert.DoesNotContain("sessionStorage", healthCode);
        Assert.Contains(".health-strip {", AdminCss);
        Assert.Contains(".health-indicators {", AdminCss);
        Assert.Contains(".health-indicator[data-state=\"warning\"]", AdminCss);
        Assert.Contains("@media (max-width: 720px)", AdminCss);
        Assert.Contains(".health-strip { flex-basis: 100%; order: 2; }", AdminCss);
    }

    private static string Slice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return source[start..end];
    }
}
