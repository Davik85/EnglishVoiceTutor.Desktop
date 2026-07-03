using EnglishVoiceTutor.Api.Contracts.Website;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Website;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace EnglishVoiceTutor.Api.Tests.Services.Website;

public sealed class WebsiteContentServiceRenderingTests
{
    private const string LegacyDownloadBodyMarkdown = """
A Windows desktop app for practicing spoken languages with an AI tutor.

Current version details are loaded from the release manifest.

Windows may show a SmartScreen warning because code signing is deferred.
""";

    private const string ReleaseReadyDownloadBodyMarkdown = """
Download Language Voice Tutor for Windows. Practice real conversations by text or voice with an AI tutor, choose practical topics, start guided lessons, and improve step by step.

Current version and installer size are loaded from the release manifest.

Windows may show a SmartScreen warning because code signing is deferred.

Need help? Email support@languagevoicetutor.com.
""";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    [Fact]
    public async Task PublishedHomeHtmlContainsDefaultLogoAndFlagImageAssets()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();

        await service.PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "index.html"), TestContext.Current.CancellationToken);

        Assert.Contains("assets/brand/lvt-logo.png", html);
        Assert.Contains("site-header__logo-image", html);
        Assert.Contains("Language Voice Tutor logo", html);
        Assert.Contains("assets/flags/gb.webp", html);
        Assert.Contains("assets/flags/fr.webp", html);
        Assert.Contains("assets/flags/de.webp", html);
        Assert.Contains("assets/flags/es.webp", html);
        Assert.Contains("assets/flags/it.webp", html);
        Assert.Contains("assets/flags/pt.webp", html);
        Assert.True(CountOccurrences(html, "<img ") > 2);
    }

    [Fact]
    public async Task PublishedHomeHeaderDoesNotRenderOnlyTextFallbackWhenDefaultLogoAssetIsAvailable()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();

        await service.PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "index.html"), TestContext.Current.CancellationToken);

        Assert.Contains("<img class=\"site-header__logo-image\" src=\"assets/brand/lvt-logo.png\"", html);
        Assert.DoesNotContain("<a class=\"site-header__brand\" href=\"index.html\" aria-label=\"Language Voice Tutor home\"><span class=\"site-header__logo-fallback\">", html);
    }


    [Fact]
    public async Task PublishedDownloadHtmlKeepsManifestDrivenReleaseHooks()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();

        await service.PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);

        Assert.Contains("/releases/windows/direct/latest.json", html);
        Assert.Contains("download.js?v=20260703-lightbox", html);
        Assert.Contains("id=\"current-version\"", html);
        Assert.Contains("id=\"download-button\"", html);
        Assert.Contains("href=\"/releases/windows/direct/LanguageVoiceTutorSetup-1.0.exe\"", html);
        Assert.Contains("download=\"LanguageVoiceTutorSetup-1.0.exe\"", html);
        Assert.DoesNotContain("href=\"LanguageVoiceTutorSetup-1.0.exe\"", html);
        Assert.Contains("id=\"manifest-status\"", html);
        Assert.Contains("id=\"installer-size\"", html);
        Assert.DoesNotContain("id=\"detail-version\"", html);
        Assert.DoesNotContain("id=\"detail-installer\"", html);
        Assert.DoesNotContain("id=\"detail-backend-base-url\"", html);
        Assert.DoesNotContain("id=\"detail-minimum-supported-version\"", html);
        Assert.DoesNotContain("id=\"detail-update-mode\"", html);
        Assert.DoesNotContain("id=\"detail-size\"", html);
        Assert.DoesNotContain("id=\"detail-sha\"", html);
        Assert.Contains("Current version", html);
        Assert.Contains("Installer size", html);
        Assert.DoesNotContain("Technical release details", html);
        Assert.Contains("Language Voice Tutor for Windows", html);
        Assert.Contains("Start quickly", html);
        Assert.Contains("Choose practical topics", html);
        Assert.Contains("Learn step by step", html);
        Assert.Contains("Practice real conversation", html);
        Assert.Contains("assets/images/download/quick-start.webp", html);
        Assert.Contains("assets/images/download/topics.webp", html);
        Assert.Contains("assets/images/download/guided-lesson.webp", html);
        Assert.Contains("assets/images/download/conversation.webp", html);
        Assert.Contains("data-download-lightbox-src=\"/assets/images/download/quick-start.webp\"", html);
        Assert.Contains("role=\"button\" tabindex=\"0\"", html);
        Assert.Contains("href=\"mailto:support@languagevoicetutor.com\"", html);
        Assert.DoesNotContain("class=\"download-content-shell\"", html);
        Assert.DoesNotContain("<section class=\"support-card\" aria-label=\"Support\">", html);
        Assert.DoesNotContain("tester download", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Available for testers", html);

        var supportIndex = html.IndexOf("support@languagevoicetutor.com", StringComparison.Ordinal);
        var firstSectionCloseAfterSupport = html.IndexOf("</section>", supportIndex, StringComparison.Ordinal);
        var secondSectionCloseAfterSupport = html.IndexOf("</section>", firstSectionCloseAfterSupport + "</section>".Length, StringComparison.Ordinal);
        var footerIndex = html.IndexOf("<footer class=\"site-footer\">", StringComparison.Ordinal);
        Assert.InRange(supportIndex, 0, firstSectionCloseAfterSupport);
        Assert.InRange(secondSectionCloseAfterSupport, 0, footerIndex);
    }

    [Fact]
    public async Task PublishedDownloadHtmlUsesCmsTitleAsMainCtaHeading()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        content.Pages["download"]["pageTitle"] = "Custom Windows Download Heading";

        await service.SaveDraftAsync(content, TestContext.Current.CancellationToken);
        await service.PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);

        Assert.Contains("<h1 id=\"product-title\">Custom Windows Download Heading</h1>", html);
        Assert.DoesNotContain("<h1 id=\"product-title\">Language Voice Tutor for Windows</h1>", html);
    }

    [Fact]
    public async Task PublishedDownloadHtmlUsesCmsBodyInsideMainCtaCard()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        content.Pages["download"]["bodyMarkdown"] = """
Custom main CTA copy for testing.

Need help? Email support@languagevoicetutor.com.
""";

        await service.SaveDraftAsync(content, TestContext.Current.CancellationToken);
        await service.PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);
        var ctaStart = html.IndexOf("<section class=\"download-cta-panel\"", StringComparison.Ordinal);
        var ctaEnd = html.IndexOf("</section>", ctaStart, StringComparison.Ordinal);
        var ctaHtml = html[ctaStart..ctaEnd];

        Assert.Contains("Custom main CTA copy for testing.", ctaHtml);
        Assert.Contains("Need help? Email <a href=\"mailto:support@languagevoicetutor.com\">support@languagevoicetutor.com</a>.", ctaHtml);
        Assert.DoesNotContain("Practice real conversations by text or voice with an AI tutor, choose practical topics", html);
        Assert.Equal(1, CountOccurrences(html, "Custom main CTA copy for testing."));
    }

    [Fact]
    public async Task BlankDownloadTitleAndBodyFallbackToReleaseReadyDefaults()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var seeded = await service.GetAsync(TestContext.Current.CancellationToken);
        seeded.Draft.Pages["download"]["pageTitle"] = "";
        seeded.Draft.Pages["download"]["bodyMarkdown"] = "   ";
        await fixture.WriteDocumentAsync(new WebsiteContentDocument(seeded.Active, seeded.Draft));

        await fixture.CreateService().PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);

        Assert.Contains("<h1 id=\"product-title\">Language Voice Tutor for Windows</h1>", html);
        Assert.Contains("Download Language Voice Tutor for Windows. Practice real conversations by text or voice with an AI tutor", html);
        Assert.Contains("Windows may show a SmartScreen warning because code signing is deferred.", html);
        Assert.Contains("Need help? Email <a href=\"mailto:support@languagevoicetutor.com\">support@languagevoicetutor.com</a>.", html);
    }

    [Fact]
    public async Task PublishedDownloadHtmlDoesNotDuplicateMainCtaMarkdownOutsideCard()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        content.Pages["download"]["bodyMarkdown"] = "Unique duplicate guard CTA body.";

        await service.SaveDraftAsync(content, TestContext.Current.CancellationToken);
        await service.PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);

        Assert.Equal(1, CountOccurrences(html, "Unique duplicate guard CTA body."));
        Assert.DoesNotContain("details-card legal-section markdown-content", html);
        Assert.DoesNotContain("class=\"download-content-shell\"", html);
        Assert.DoesNotContain("<section class=\"support-card\" aria-label=\"Support\">", html);
    }


    [Fact]
    public async Task PublishPreservesPublicAssetsAndReleaseArtifacts()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var downloadAsset = Path.Combine(fixture.PublicSiteRoot, "assets", "images", "download", "quick-start.webp");
        var landingAsset = Path.Combine(fixture.PublicSiteRoot, "assets", "images", "landing", "windows-desktop.webp");
        var brandAsset = Path.Combine(fixture.PublicSiteRoot, "assets", "brand", "lvt-logo.png");
        var flagAsset = Path.Combine(fixture.PublicSiteRoot, "assets", "flags", "gb.webp");
        var releaseManifest = Path.Combine(fixture.PublicSiteRoot, "releases", "windows", "direct", "latest.json");
        var installerArtifact = Path.Combine(fixture.PublicSiteRoot, "releases", "windows", "direct", "LanguageVoiceTutorSetup-1.0.exe");
        foreach (var path in new[] { downloadAsset, landingAsset, brandAsset, flagAsset, releaseManifest, installerArtifact })
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        }
        await File.WriteAllTextAsync(downloadAsset, "download screenshot", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(landingAsset, "landing screenshot", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(brandAsset, "brand", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(flagAsset, "flag", TestContext.Current.CancellationToken);
        const string releaseManifestSentinel = "{\"version\":\"1.0\",\"installerRelativeUrl\":\"LanguageVoiceTutorSetup-1.0.exe\",\"installerFileName\":\"LanguageVoiceTutorSetup-1.0.exe\",\"installerSizeBytes\":123}";
        await File.WriteAllTextAsync(releaseManifest, releaseManifestSentinel, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(installerArtifact, "installer", TestContext.Current.CancellationToken);

        var response = await service.PublishAsync(TestContext.Current.CancellationToken);

        Assert.Contains(response.PublishedFiles, file => Path.GetFileName(file) == "download.html");
        Assert.Contains(response.PublishedFiles, file => Path.GetFileName(file) == "index.html");
        Assert.True(File.Exists(downloadAsset));
        Assert.True(File.Exists(landingAsset));
        Assert.True(File.Exists(brandAsset));
        Assert.True(File.Exists(flagAsset));
        Assert.Equal("download screenshot", await File.ReadAllTextAsync(downloadAsset, TestContext.Current.CancellationToken));
        Assert.Equal(releaseManifestSentinel, await File.ReadAllTextAsync(releaseManifest, TestContext.Current.CancellationToken));
        Assert.Equal("installer", await File.ReadAllTextAsync(installerArtifact, TestContext.Current.CancellationToken));
        var downloadHtml = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);
        Assert.Contains("href=\"/releases/windows/direct/LanguageVoiceTutorSetup-1.0.exe\"", downloadHtml);
        Assert.DoesNotContain(response.PublishedFiles, file => file.Contains(Path.Combine("assets", "images", "download"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(response.PublishedFiles, file => file.Contains(Path.Combine("releases", "windows", "direct"), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAsyncUpgradesLegacyDownloadContentInActiveAndDraft()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var seeded = await service.GetAsync(TestContext.Current.CancellationToken);
        seeded.Active.Pages["download"]["pageTitle"] = "Language Voice Tutor tester download";
        seeded.Active.Pages["download"]["seoTitle"] = "Language Voice Tutor tester download";
        seeded.Active.Pages["download"]["bodyMarkdown"] = LegacyDownloadBodyMarkdown;
        seeded.Draft.Pages["download"]["pageTitle"] = "LANGUAGE VOICE TUTOR TESTER DOWNLOAD";
        seeded.Draft.Pages["download"]["seoTitle"] = "Tester Download";
        seeded.Draft.Pages["download"]["bodyMarkdown"] = LegacyDownloadBodyMarkdown;
        await fixture.WriteDocumentAsync(new WebsiteContentDocument(seeded.Active, seeded.Draft));

        var upgraded = await fixture.CreateService().GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Language Voice Tutor for Windows", upgraded.Active.Pages["download"]["pageTitle"]);
        Assert.Equal("Language Voice Tutor for Windows Download", upgraded.Active.Pages["download"]["seoTitle"]);
        Assert.Equal(ReleaseReadyDownloadBodyMarkdown, upgraded.Active.Pages["download"]["bodyMarkdown"]);
        Assert.Equal("Language Voice Tutor for Windows", upgraded.Draft.Pages["download"]["pageTitle"]);
        Assert.Equal("Language Voice Tutor for Windows Download", upgraded.Draft.Pages["download"]["seoTitle"]);
        Assert.Equal(ReleaseReadyDownloadBodyMarkdown, upgraded.Draft.Pages["download"]["bodyMarkdown"]);
        Assert.DoesNotContain("tester-era", upgraded.Draft.Pages["download"]["bodyMarkdown"], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Current version details are loaded from the release manifest.", upgraded.Draft.Pages["download"]["bodyMarkdown"]);

        var persistedJson = await File.ReadAllTextAsync(fixture.StorageJsonPath, TestContext.Current.CancellationToken);
        Assert.DoesNotContain("tester download", persistedJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Current version details are loaded from the release manifest.", persistedJson);
    }

    [Fact]
    public async Task GetAsyncDoesNotOverwriteNonLegacyCustomContentOrOtherPages()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var seeded = await service.GetAsync(TestContext.Current.CancellationToken);
        seeded.Draft.Pages["download"]["pageTitle"] = "Custom Windows download";
        seeded.Draft.Pages["download"]["seoTitle"] = "Custom SEO download";
        seeded.Draft.Pages["download"]["bodyMarkdown"] = "Custom release notes for the editor.";
        seeded.Draft.Pages["support"]["pageTitle"] = "Custom support page";
        seeded.Draft.Pages["support"]["bodyMarkdown"] = LegacyDownloadBodyMarkdown;
        await fixture.WriteDocumentAsync(new WebsiteContentDocument(seeded.Active, seeded.Draft));

        var loaded = await fixture.CreateService().GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Custom Windows download", loaded.Draft.Pages["download"]["pageTitle"]);
        Assert.Equal("Custom SEO download", loaded.Draft.Pages["download"]["seoTitle"]);
        Assert.Equal("Custom release notes for the editor.", loaded.Draft.Pages["download"]["bodyMarkdown"]);
        Assert.Equal("Custom support page", loaded.Draft.Pages["support"]["pageTitle"]);
        Assert.Equal(LegacyDownloadBodyMarkdown, loaded.Draft.Pages["support"]["bodyMarkdown"]);
    }


    [Fact]
    public async Task OlderDownloadContentWithoutFeatureCardsGetsDefaultCards()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var seeded = await service.GetAsync(TestContext.Current.CancellationToken);
        foreach (var key in seeded.Draft.Pages["download"].Keys.Where(key => key.StartsWith("featureCard", StringComparison.Ordinal)).ToList())
        {
            seeded.Draft.Pages["download"].Remove(key);
        }
        await fixture.WriteDocumentAsync(new WebsiteContentDocument(seeded.Active, seeded.Draft));

        var loaded = await fixture.CreateService().GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Quick Start", loaded.Draft.Pages["download"]["featureCard1Label"]);
        Assert.Equal("Start quickly", loaded.Draft.Pages["download"]["featureCard1Title"]);
        Assert.Equal("Open the app and jump into practical language practice in a few clicks.", loaded.Draft.Pages["download"]["featureCard1Description"]);
        Assert.Equal("/assets/images/download/quick-start.webp", loaded.Draft.Pages["download"]["featureCard1ImagePath"]);
        Assert.Equal("/assets/images/download/topics.webp", loaded.Draft.Pages["download"]["featureCard2ImagePath"]);
        Assert.Equal("/assets/images/download/guided-lesson.webp", loaded.Draft.Pages["download"]["featureCard3ImagePath"]);
        Assert.Equal("/assets/images/download/conversation.webp", loaded.Draft.Pages["download"]["featureCard4ImagePath"]);
        Assert.Equal("Language Voice Tutor", loaded.Draft.Pages["home"]["seoTitle"]);
    }

    [Fact]
    public async Task GetAsyncResponseIncludesDownloadFeatureCardsAndImagePaths()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();

        var response = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Quick Start", response.Draft.Pages["download"]["featureCard1Label"]);
        Assert.Equal("Topics", response.Draft.Pages["download"]["featureCard2Label"]);
        Assert.Equal("Guided Lesson", response.Draft.Pages["download"]["featureCard3Label"]);
        Assert.Equal("Conversation", response.Draft.Pages["download"]["featureCard4Label"]);
        Assert.Equal("/assets/images/download/quick-start.webp", response.Draft.Pages["download"]["featureCard1ImagePath"]);
        Assert.Equal("/assets/images/download/topics.webp", response.Draft.Pages["download"]["featureCard2ImagePath"]);
        Assert.Equal("/assets/images/download/guided-lesson.webp", response.Draft.Pages["download"]["featureCard3ImagePath"]);
        Assert.Equal("/assets/images/download/conversation.webp", response.Draft.Pages["download"]["featureCard4ImagePath"]);
    }

    [Fact]
    public async Task SavingEditedDownloadFeatureCardPersistsAndRenders()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        content.Pages["download"]["featureCard2Title"] = "Pick your scenario";
        content.Pages["download"]["featureCard2Description"] = "Custom topics description from CMS.";
        content.Pages["download"]["featureCard2ImagePath"] = "/assets/images/download/custom-topics.webp";

        await service.SaveDraftAsync(content, TestContext.Current.CancellationToken);
        var reloaded = await fixture.CreateService().GetAsync(TestContext.Current.CancellationToken);
        var preview = await fixture.CreateService().PreviewAsync(new WebsitePreviewRequest(reloaded.Draft, "download"), TestContext.Current.CancellationToken);

        Assert.Equal("Pick your scenario", reloaded.Draft.Pages["download"]["featureCard2Title"]);
        Assert.Equal("Custom topics description from CMS.", reloaded.Draft.Pages["download"]["featureCard2Description"]);
        Assert.Equal("/assets/images/download/custom-topics.webp", reloaded.Draft.Pages["download"]["featureCard2ImagePath"]);
        Assert.Contains("Pick your scenario", preview.Html);
        Assert.Contains("Custom topics description from CMS.", preview.Html);
        Assert.Contains("/assets/images/download/custom-topics.webp", preview.Html);
        Assert.Contains("data-download-lightbox-src=\"/assets/images/download/custom-topics.webp\"", preview.Html);
        Assert.DoesNotContain("tester download", preview.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Available for testers", preview.Html);
        Assert.DoesNotContain("Technical release details", preview.Html);
        Assert.DoesNotContain("<section class=\"support-card\" aria-label=\"Support\">", preview.Html);
    }


    [Fact]
    public async Task SaveDraftAndPublishPreserveDownloadFeatureCardsWhenPayloadIsPartial()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var seeded = await service.GetAsync(TestContext.Current.CancellationToken);
        var partialPages = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["download"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["pageTitle"] = "Partial CMS download save",
                ["bodyMarkdown"] = "Partial body from a generic editor payload.",
                ["seoTitle"] = "Partial SEO title",
                ["seoDescription"] = "Partial SEO description"
            }
        };
        var partialDraft = new WebsiteContentSet(partialPages, seeded.Draft.Design, seeded.Draft.Marketing);

        var saved = await service.SaveDraftAsync(partialDraft, TestContext.Current.CancellationToken);
        var published = await service.PublishAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Partial CMS download save", saved.Draft.Pages["download"]["pageTitle"]);
        AssertDownloadFeatureCardImagePaths(saved.Draft.Pages["download"]);
        AssertDownloadFeatureCardImagePaths(published.Active.Pages["download"]);
    }

    [Fact]
    public async Task BlankAndMissingDownloadFeatureCardImagePathsNormalizeToDefaultsAndRender()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var seeded = await service.GetAsync(TestContext.Current.CancellationToken);
        seeded.Draft.Pages["download"]["featureCard1ImagePath"] = "";
        seeded.Draft.Pages["download"]["featureCard2ImagePath"] = "   ";
        seeded.Draft.Pages["download"].Remove("featureCard3ImagePath");
        seeded.Draft.Pages["download"]["featureCard4ImagePath"] = null!;
        await fixture.WriteDocumentAsync(new WebsiteContentDocument(seeded.Active, seeded.Draft));

        var loaded = await fixture.CreateService().GetAsync(TestContext.Current.CancellationToken);
        var published = await fixture.CreateService().PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);
        var persistedJson = await File.ReadAllTextAsync(fixture.StorageJsonPath, TestContext.Current.CancellationToken);

        AssertDownloadFeatureCardImagePaths(loaded.Draft.Pages["download"]);
        AssertDownloadFeatureCardImagePaths(published.Active.Pages["download"]);
        AssertDownloadHtmlContainsDefaultFeatureCards(html);
        Assert.DoesNotContain("\"featureCard1ImagePath\": \"\"", persistedJson);
        Assert.DoesNotContain("\"featureCard2ImagePath\": \"   \"", persistedJson);
        Assert.DoesNotContain("\"featureCard4ImagePath\": null", persistedJson);
        Assert.DoesNotContain("empty url()", html);
        Assert.DoesNotContain("data-download-lightbox-src=\"\"", html);
        Assert.DoesNotContain("background-image: url(\"\")", html);
    }

    [Fact]
    public async Task PublishRegressionKeepsDownloadImagePathsAndReleaseSentinel()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var paths = DefaultDownloadImagePaths.Select(path => Path.Combine(fixture.PublicSiteRoot, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))).ToArray();
        foreach (var path in paths)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, "screenshot", TestContext.Current.CancellationToken);
        }
        var releaseManifest = Path.Combine(fixture.PublicSiteRoot, "releases", "windows", "direct", "latest.json");
        Directory.CreateDirectory(Path.GetDirectoryName(releaseManifest)!);
        const string latestJsonSentinel = "{\"version\":\"sentinel\"}";
        await File.WriteAllTextAsync(releaseManifest, latestJsonSentinel, TestContext.Current.CancellationToken);

        await fixture.CreateService().PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);

        AssertDownloadHtmlContainsDefaultFeatureCards(html);
        foreach (var path in paths)
        {
            Assert.True(File.Exists(path));
        }
        Assert.Equal(latestJsonSentinel, await File.ReadAllTextAsync(releaseManifest, TestContext.Current.CancellationToken));
        Assert.DoesNotContain("tester download", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Available for testers", html);
        Assert.DoesNotContain("Technical release details", html);
        Assert.DoesNotContain("href=\"LanguageVoiceTutorSetup-1.0.exe\"", html);
        Assert.DoesNotContain("empty url()", html);
        Assert.DoesNotContain("data-download-lightbox-src=\"\"", html);
        Assert.DoesNotContain("background-image: url(\"\")", html);
    }

    [Fact]
    public void AdminWebsiteEditorDefinesStructuredDownloadFeatureCardFieldsAndHelp()
    {
        var adminJs = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "backend", "EnglishVoiceTutor.Api", "wwwroot", "admin", "admin.js"));

        Assert.Contains("Download feature cards", adminJs);
        Assert.Contains("featureCard1Label", adminJs);
        Assert.Contains("featureCard2Title", adminJs);
        Assert.Contains("featureCard3Description", adminJs);
        Assert.Contains("featureCard4ImagePath", adminJs);
        Assert.Contains("Upload screenshots as WebP files to: /assets/images/download/", adminJs);
        Assert.Contains("/assets/images/download/quick-start.webp", adminJs);
        Assert.Contains("/assets/images/download/topics.webp", adminJs);
        Assert.Contains("/assets/images/download/guided-lesson.webp", adminJs);
        Assert.Contains("/assets/images/download/conversation.webp", adminJs);
        Assert.Contains("public website assets, not release artifacts", adminJs);
        Assert.Contains("preserveDownloadFeatureCardFields", adminJs);
        Assert.Contains("defaultDownloadFeatureCardValues", adminJs);
        Assert.Contains("JSON.stringify(websiteContentDraft)", adminJs);
    }

    [Fact]
    public async Task PreviewHtmlIncludesPublicBaseHrefSoAboutBlankCanResolveRelativeAssets()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;

        var preview = await service.PreviewAsync(new WebsitePreviewRequest(content, "home"), TestContext.Current.CancellationToken);

        Assert.Contains("<base href=\"https://languagevoicetutor.com/\">", preview.Html);
        Assert.Contains("assets/images/landing/windows-desktop.webp", preview.Html);
        Assert.Contains("assets/brand/lvt-logo.png", preview.Html);
        Assert.Contains("assets/flags/gb.webp", preview.Html);
    }

    [Fact]
    public async Task PublishedPagesIncludeGroupedFooterLinksAndPublishDisclosurePages()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();

        var response = await service.PublishAsync(TestContext.Current.CancellationToken);
        var htmlFiles = Directory.GetFiles(fixture.PublicSiteRoot, "*.html");

        Assert.Contains(response.PublishedFiles, file => Path.GetFileName(file) == "seller.html");
        Assert.Contains(response.PublishedFiles, file => Path.GetFileName(file) == "ai-data.html");
        Assert.Contains(response.PublishedFiles, file => Path.GetFileName(file) == "status.html");
        foreach (var file in htmlFiles)
        {
            var html = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
            Assert.Contains("site-footer__link-row site-footer__link-row--primary", html);
            Assert.Contains("site-footer__link-row site-footer__link-row--secondary", html);
            Assert.Contains("href=\"privacy.html\"", html);
            Assert.Contains("href=\"terms.html\"", html);
            Assert.Contains("href=\"refunds.html\"", html);
            Assert.Contains("href=\"cancellation.html\"", html);
            Assert.Contains("href=\"support.html\"", html);
            Assert.Contains("href=\"pricing.html\"", html);
            Assert.Contains("href=\"seller.html\"", html);
            Assert.Contains("Seller / Company Details", html);
            Assert.Contains("href=\"ai-data.html\"", html);
            Assert.Contains("AI &amp; Data Disclosure", html);
            Assert.Contains("href=\"status.html\"", html);
            Assert.Contains("Service Status", html);
        }
    }

    [Fact]
    public async Task PreviewStatusPageUsesPublicBaseHrefAndLegalPageShell()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;

        var preview = await service.PreviewAsync(new WebsitePreviewRequest(content, "status"), TestContext.Current.CancellationToken);

        Assert.Equal("status", preview.PageKey);
        Assert.Contains("<base href=\"https://languagevoicetutor.com/\">", preview.Html);
        Assert.Contains("<main class=\"page-shell legal-page\">", preview.Html);
        Assert.Contains("Platform availability / service status", preview.Html);
    }

    [Fact]
    public async Task PublishedHomeKeepsLandingAssetsAndResponsiveLayoutProtections()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();

        await service.PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "index.html"), TestContext.Current.CancellationToken);

        Assert.Contains("assets/brand/lvt-logo.png", html);
        Assert.Contains("assets/flags/gb.webp", html);
        Assert.Contains("assets/images/landing/windows-desktop.webp", html);
        Assert.Contains("assets/images/landing/mobile.webp", html);
        Assert.Contains("100svh", html);
        Assert.Contains("100dvh", html);
        Assert.Contains("flex: 1 1 auto", html);
        Assert.Contains("max-height: calc(100% - clamp", html);
    }

    [Fact]
    public async Task MarkdownLinksSafeUrlsEmailsAndBareDomainsAndRejectsUnsafeSchemes()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        content.Pages["status"]["bodyMarkdown"] = "Visit https://example.com/docs. Email support@languagevoicetutor.com. [Contact](mailto:support@languagevoicetutor.com) [Docs](https://example.com/help) Paddle.com www.paddle.com developer.paddle.com support.paddle.com Paddle.com. Paddle.com, Paddle.com) [Bad](javascript:alert(1)) javascript:alert(1) <script>alert(1)</script>";

        var preview = await service.PreviewAsync(new WebsitePreviewRequest(content, "status"), TestContext.Current.CancellationToken);

        Assert.Contains("<a href=\"https://example.com/docs\" rel=\"noopener noreferrer\">https://example.com/docs</a>.", preview.Html);
        Assert.Contains("<a href=\"mailto:support@languagevoicetutor.com\">support@languagevoicetutor.com</a>", preview.Html);
        Assert.Contains("<a href=\"mailto:support@languagevoicetutor.com\">Contact</a>", preview.Html);
        Assert.Contains("<a href=\"https://example.com/help\" rel=\"noopener noreferrer\">Docs</a>", preview.Html);
        Assert.Contains("<a href=\"https://paddle.com/\" rel=\"noopener noreferrer\">Paddle.com</a>", preview.Html);
        Assert.Contains("<a href=\"https://www.paddle.com/\" rel=\"noopener noreferrer\">www.paddle.com</a>", preview.Html);
        Assert.Contains("<a href=\"https://developer.paddle.com/\" rel=\"noopener noreferrer\">developer.paddle.com</a>", preview.Html);
        Assert.Contains("<a href=\"https://support.paddle.com/\" rel=\"noopener noreferrer\">support.paddle.com</a>", preview.Html);
        Assert.Contains("<a href=\"https://paddle.com/\" rel=\"noopener noreferrer\">Paddle.com</a>.", preview.Html);
        Assert.Contains("<a href=\"https://paddle.com/\" rel=\"noopener noreferrer\">Paddle.com</a>,", preview.Html);
        Assert.Contains("<a href=\"https://paddle.com/\" rel=\"noopener noreferrer\">Paddle.com</a>)", preview.Html);
        Assert.Contains("Bad", preview.Html);
        Assert.Contains("javascript:alert(1)", preview.Html);
        Assert.DoesNotContain("href=\"javascript:", preview.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script>alert(1)</script>", preview.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", preview.Html);
    }


    private static readonly string[] DefaultDownloadImagePaths =
    [
        "/assets/images/download/quick-start.webp",
        "/assets/images/download/topics.webp",
        "/assets/images/download/guided-lesson.webp",
        "/assets/images/download/conversation.webp"
    ];

    private static void AssertDownloadFeatureCardImagePaths(Dictionary<string, string> download)
    {
        for (var i = 0; i < DefaultDownloadImagePaths.Length; i++)
        {
            Assert.Equal(DefaultDownloadImagePaths[i], download[$"featureCard{i + 1}ImagePath"]);
        }
    }

    private static void AssertDownloadHtmlContainsDefaultFeatureCards(string html)
    {
        foreach (var path in DefaultDownloadImagePaths)
        {
            Assert.Contains(path, html);
            Assert.Contains($"data-download-lightbox-src=\"{path}\"", html);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var adminJsPath = Path.Combine(directory.FullName, "backend", "EnglishVoiceTutor.Api", "wwwroot", "admin", "admin.js");
            if (File.Exists(adminJsPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find repository root containing backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js.");
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }
        return count;
    }

    private sealed class WebsiteContentServiceFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "lvt-website-tests", Guid.NewGuid().ToString("N"));

        public WebsiteContentServiceFixture()
        {
            ContentRoot = Path.Combine(_root, "content-root");
            PublicSiteRoot = Path.Combine(_root, "public-site");
            StorageJsonPath = Path.Combine(_root, "content", "website-content.json");
            Directory.CreateDirectory(ContentRoot);
        }

        public string ContentRoot { get; }
        public string PublicSiteRoot { get; }
        public string StorageJsonPath { get; }

        public WebsiteContentService CreateService()
        {
            var options = Microsoft.Extensions.Options.Options.Create(new WebsiteContentOptions
            {
                StorageJsonPath = StorageJsonPath,
                PublicSiteRoot = PublicSiteRoot
            });
            return new WebsiteContentService(options, new TestWebHostEnvironment(ContentRoot));
        }

        public async Task WriteDocumentAsync(WebsiteContentDocument document)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StorageJsonPath)!);
            await using var stream = File.Create(StorageJsonPath);
            await JsonSerializer.SerializeAsync(stream, document, JsonOptions, TestContext.Current.CancellationToken);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }

    private sealed class TestWebHostEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "EnglishVoiceTutor.Api.Tests";
        public string WebRootPath { get; set; } = Path.Combine(contentRootPath, "wwwroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
