namespace EnglishVoiceTutor.Api.Tests.AdminUi;

public sealed class AdminSetupLocalizationImportUiStaticTests
{
    private static readonly string AdminJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));
    private static readonly string AdminIndex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/index.html"));
    private static readonly string Endpoints = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs"));

    [Fact]
    public void OverviewRendersPreviewAndInitiallyDisabledApplyControls()
    {
        Assert.Contains("cms-preview-setup-localizations-import-button", AdminIndex);
        Assert.Contains("cms-apply-setup-localizations-import-button\" type=\"button\" disabled", AdminIndex);
        Assert.Contains("Localized lesson setup import", AdminIndex);
    }

    [Fact]
    public void NarrowFlowUsesOnlyPreviewAndSetupLocalizationImportEndpoints()
    {
        var flow = AdminJs.Substring(AdminJs.IndexOf("async function previewSetupLocalizationsImport", StringComparison.Ordinal), AdminJs.IndexOf("async function runCmsValidation", StringComparison.Ordinal) - AdminJs.IndexOf("async function previewSetupLocalizationsImport", StringComparison.Ordinal));
        Assert.Contains("ApiPaths.cmsSetupLocalizationsImportPreview", flow);
        Assert.Contains("ApiPaths.cmsSetupLocalizationsImport", flow);
        Assert.DoesNotContain("cmsStaticJsonV1Initialize", flow);
        Assert.DoesNotContain("cmsPublish", flow);
        Assert.DoesNotContain("cmsRestore", flow);
        Assert.Contains("clearSetupLocalizationImportPreview();", AdminJs);
        Assert.Contains("await refreshCmsContentPack(true);", flow);
    }

    [Fact]
    public void NewEndpointsKeepReadAndDraftWritePermissionsSeparate()
    {
        var preview = Endpoints.Substring(Endpoints.IndexOf("AdminDevCmsStaticJsonV1SetupLocalizationsImportPreviewRoute", StringComparison.Ordinal), 350);
        var apply = Endpoints.Substring(Endpoints.IndexOf("AdminDevCmsStaticJsonV1SetupLocalizationsImportRoute", StringComparison.Ordinal), 350);
        Assert.Contains("CmsContentReadPermissionPolicyName", preview);
        Assert.Contains("CmsDraftSavePermissionPolicyName", apply);
    }
}
