namespace EnglishVoiceTutor.Api.Tests.AdminUi;

public sealed class AdminLoginSecurityUiStaticTests
{
    private static readonly string AdminJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));
    private static readonly string AdminIndex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/index.html"));

    [Fact]
    public void LoginFormFailsClosedWhenTheScriptCannotRegisterItsHandler()
    {
        Assert.Contains("<form id=\"login-form\" action=\"/admin/\" method=\"post\" novalidate>", AdminIndex);
        var loginForm = AdminIndex.Substring(AdminIndex.IndexOf("<form id=\"login-form\"", StringComparison.Ordinal), AdminIndex.IndexOf("</form>", AdminIndex.IndexOf("<form id=\"login-form\"", StringComparison.Ordinal), StringComparison.Ordinal));

        Assert.Contains("<input id=\"email\" type=\"email\" autocomplete=\"username\" required />", loginForm);
        Assert.Contains("<input id=\"password\" type=\"password\" autocomplete=\"current-password\" required />", loginForm);
        Assert.DoesNotContain("name=\"email\"", loginForm, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name=\"password\"", loginForm, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<form id=\"login-form\" method=\"get\"", AdminIndex, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LegacyCredentialParametersAreRemovedWithoutReloadAndOtherUrlStateIsPreserved()
    {
        var cleanup = AdminJs.Substring(0, AdminJs.IndexOf("const ApiPaths", StringComparison.Ordinal));

        Assert.Contains("function removeLegacySensitiveLoginParameters()", cleanup);
        Assert.Contains("parameterName.toLowerCase() === \"email\"", cleanup);
        Assert.Contains("parameterName.toLowerCase() === \"password\"", cleanup);
        Assert.Contains("currentUrl.searchParams.delete(parameterName);", cleanup);
        Assert.Contains("window.history.replaceState(window.history.state, \"\", `${currentUrl.pathname}${currentUrl.search}${currentUrl.hash}`);", cleanup);
        Assert.Contains("removeLegacySensitiveLoginParameters();", cleanup);
        Assert.DoesNotContain("formData.get(\"email\")", cleanup);
        Assert.DoesNotContain("formData.get(\"password\")", cleanup);
    }

    [Fact]
    public void LoginHandlerReadsCredentialsDirectlyAndUsesJsonPost()
    {
        var loginHandler = AdminJs.Substring(AdminJs.IndexOf("loginForm.addEventListener(\"submit\"", StringComparison.Ordinal));

        Assert.Contains("event.preventDefault();", loginHandler);
        Assert.Contains("const email = emailInput.value.trim();", loginHandler);
        Assert.Contains("const password = passwordInput.value;", loginHandler);
        Assert.DoesNotContain("new FormData(loginForm)", loginHandler);
        Assert.Contains("fetch(ApiPaths.login, { method: \"POST\"", loginHandler);
        Assert.Contains("headers: { \"Content-Type\": \"application/json\" }", loginHandler);
        Assert.Contains("body: JSON.stringify({ email, password })", loginHandler);
    }
}
