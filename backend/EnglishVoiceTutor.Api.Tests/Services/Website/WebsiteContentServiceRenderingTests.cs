using EnglishVoiceTutor.Api.Contracts.Website;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Website;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace EnglishVoiceTutor.Api.Tests.Services.Website;

public sealed class WebsiteContentServiceRenderingTests
{
    private const string GooglePlayListingUrl = "https://play.google.com/store/apps/details?id=com.languagevoicetutor.mobile";
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
    public async Task PublishPreservesIndependentHomeAndMobileFilesWhileRenderingCmsPages()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        Directory.CreateDirectory(fixture.PublicSiteRoot);
        var indexPath = Path.Combine(fixture.PublicSiteRoot, "index.html");
        var mobilePath = Path.Combine(fixture.PublicSiteRoot, "mobile.html");
        const string indexSentinel = "independent static homepage sentinel";
        const string mobileSentinel = "independent mobile redirect sentinel";
        await File.WriteAllTextAsync(indexPath, indexSentinel, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(mobilePath, mobileSentinel, TestContext.Current.CancellationToken);

        var response = await service.PublishAsync(TestContext.Current.CancellationToken);

        Assert.Equal(indexSentinel, await File.ReadAllTextAsync(indexPath, TestContext.Current.CancellationToken));
        Assert.Equal(mobileSentinel, await File.ReadAllTextAsync(mobilePath, TestContext.Current.CancellationToken));
        Assert.DoesNotContain(response.PublishedFiles, file => Path.GetFileName(file) is "index.html" or "mobile.html");
        foreach (var fileName in new[] { "download.html", "pricing.html", "support.html", "terms.html", "privacy.html", "refunds.html", "cancellation.html", "seller.html", "ai-data.html", "status.html" })
        {
            Assert.Contains(response.PublishedFiles, file => Path.GetFileName(file) == fileName);
            Assert.True(File.Exists(Path.Combine(fixture.PublicSiteRoot, fileName)));
        }
    }

    [Fact]
    public async Task RetiredHomeAndMobilePreviewRequestsRenderTheCmsManagedDownloadPage()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        foreach (var pageKey in new[] { "home", "mobile" })
        {
            var preview = await service.PreviewAsync(new WebsitePreviewRequest(content, pageKey), TestContext.Current.CancellationToken);
            Assert.Equal("download", preview.PageKey);
            Assert.Contains("<section class=\"download-hero\"", preview.Html);
            Assert.DoesNotContain("mobile-product-panel", preview.Html);
        }
    }

    [Fact]
    public async Task PublishedSitemapIncludesTheCanonicalRootExactlyOnceAndExcludesRetiredRoutes()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();

        var response = await service.PublishAsync(TestContext.Current.CancellationToken);
        var sitemapPath = Path.Combine(fixture.PublicSiteRoot, "sitemap.xml");
        var sitemap = XDocument.Load(sitemapPath);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = sitemap.Descendants(ns + "loc").Select(element => element.Value).ToList();

        Assert.Equal(1, urls.Count(url => url == "https://languagevoicetutor.com/"));
        Assert.DoesNotContain("https://languagevoicetutor.com/index.html", urls);
        Assert.DoesNotContain("https://languagevoicetutor.com/mobile.html", urls);
        Assert.DoesNotContain("https://languagevoicetutor.com/ai-language-tutor/", urls);
        Assert.DoesNotContain(response.PublishedFiles, file => file.Contains("ai-language-tutor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PublishedCmsHeaderUsesTheRootHomeLinkAndDefaultLogo()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();

        await service.PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);

        Assert.Contains("<img class=\"site-header__logo-image\" src=\"assets/brand/lvt-logo.png\"", html);
        Assert.Contains("<a class=\"site-header__brand\" href=\"/\" aria-label=\"Language Voice Tutor home\">", html);
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

        AssertInOrder(
            html,
            "Download Language Voice Tutor for Windows.",
            "Practice real conversations by text or voice with an AI tutor",
            "id=\"current-version\"",
            "id=\"installer-size\"",
            "id=\"download-button\"",
            "id=\"manifest-status\"",
            "Windows may show a SmartScreen warning because code signing is deferred.",
            "support@languagevoicetutor.com");
        Assert.DoesNotContain("Current version and installer size are loaded from the release manifest.", html);

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
        AssertInOrder(ctaHtml, "Custom main CTA copy for testing.", "id=\"current-version\"", "id=\"download-button\"", "id=\"manifest-status\"", "Need help? Email");
        Assert.DoesNotContain("Practice real conversations by text or voice with an AI tutor, choose practical topics", html);
        Assert.Equal(1, CountOccurrences(html, "Custom main CTA copy for testing."));
    }

    [Fact]
    public async Task PublishedDownloadHtmlSplitsCmsBodyAroundReleaseControls()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        content.Pages["download"]["bodyMarkdown"] = """
Download Language Voice Tutor for Windows.
Practice real conversations by text or voice with an AI tutor, choose practical topics, start guided lessons, and improve step by step.

Windows may show a SmartScreen warning because code signing is deferred.

Need help? Email support@languagevoicetutor.com.
""";

        await service.SaveDraftAsync(content, TestContext.Current.CancellationToken);
        await service.PublishAsync(TestContext.Current.CancellationToken);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);

        AssertInOrder(html, "Download Language Voice Tutor for Windows.", "Practice real conversations", "id=\"current-version\"", "id=\"download-button\"", "id=\"manifest-status\"", "Windows may show a SmartScreen warning", "href=\"mailto:support@languagevoicetutor.com\"");
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
        var assetLinksFile = Path.Combine(fixture.PublicSiteRoot, ".well-known", "assetlinks.json");
        var independentAiLanding = Path.Combine(fixture.PublicSiteRoot, "ai-language-tutor", "index.html");
        var unrelatedAsset = Path.Combine(fixture.PublicSiteRoot, "assets", "custom", "sentinel.txt");
        foreach (var path in new[] { downloadAsset, landingAsset, brandAsset, flagAsset, releaseManifest, installerArtifact, assetLinksFile, independentAiLanding, unrelatedAsset })
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        }
        await File.WriteAllTextAsync(downloadAsset, "download screenshot", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(landingAsset, "landing screenshot", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(brandAsset, "brand", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(flagAsset, "flag", TestContext.Current.CancellationToken);
        const string releaseManifestSentinel = "{\"version\":\"1.0\",\"installerRelativeUrl\":\"LanguageVoiceTutorSetup-1.0.exe\",\"installerFileName\":\"LanguageVoiceTutorSetup-1.0.exe\",\"installerSizeBytes\":123}";
        const string assetLinksSentinel = "[{\"relation\":[\"delegate_permission/common.get_login_creds\"],\"target\":{\"namespace\":\"android_app\",\"package_name\":\"com.example.sentinel\",\"sha256_cert_fingerprints\":[\"AA:BB\"]}}]";
        await File.WriteAllTextAsync(releaseManifest, releaseManifestSentinel, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(installerArtifact, "installer", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(assetLinksFile, assetLinksSentinel, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(independentAiLanding, "independent AI landing sentinel", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(unrelatedAsset, "unrelated asset sentinel", TestContext.Current.CancellationToken);

        var response = await service.PublishAsync(TestContext.Current.CancellationToken);

        Assert.Contains(response.PublishedFiles, file => Path.GetFileName(file) == "download.html");
        Assert.DoesNotContain(response.PublishedFiles, file => Path.GetFileName(file) is "index.html" or "mobile.html");
        Assert.True(File.Exists(downloadAsset));
        Assert.True(File.Exists(landingAsset));
        Assert.True(File.Exists(brandAsset));
        Assert.True(File.Exists(flagAsset));
        Assert.Equal("download screenshot", await File.ReadAllTextAsync(downloadAsset, TestContext.Current.CancellationToken));
        Assert.Equal(releaseManifestSentinel, await File.ReadAllTextAsync(releaseManifest, TestContext.Current.CancellationToken));
        Assert.Equal("installer", await File.ReadAllTextAsync(installerArtifact, TestContext.Current.CancellationToken));
        Assert.True(File.Exists(assetLinksFile));
        Assert.Equal(assetLinksSentinel, await File.ReadAllTextAsync(assetLinksFile, TestContext.Current.CancellationToken));
        Assert.Equal("independent AI landing sentinel", await File.ReadAllTextAsync(independentAiLanding, TestContext.Current.CancellationToken));
        Assert.Equal("unrelated asset sentinel", await File.ReadAllTextAsync(unrelatedAsset, TestContext.Current.CancellationToken));
        var downloadHtml = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);
        Assert.Contains("href=\"/releases/windows/direct/LanguageVoiceTutorSetup-1.0.exe\"", downloadHtml);
        Assert.DoesNotContain(response.PublishedFiles, file => file.Contains(Path.Combine("assets", "images", "download"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(response.PublishedFiles, file => file.Contains(Path.Combine("releases", "windows", "direct"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(response.PublishedFiles, file => file.Contains(Path.Combine(".well-known", "assetlinks.json"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(response.PublishedFiles, file => file.Contains(Path.Combine("ai-language-tutor", "index.html"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(response.PublishedFiles, file => file.Contains(Path.Combine("assets", "custom"), StringComparison.OrdinalIgnoreCase));
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
    public async Task GetAsyncPreservesRetiredMobileDataAndSharedCompanyChrome()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var seeded = await fixture.CreateService().GetAsync(TestContext.Current.CancellationToken);
        seeded.Active.Pages["home"]["mobileCardBadge"] = "In development";
        seeded.Active.Pages["home"]["mobileCardDescription"] = "Android and iOS apps are planned but are not currently available.";
        seeded.Active.Pages["home"]["windowsCardTitle"] = "Custom Windows title";
        seeded.Active.Pages["home"]["footerCopyrightText"] = "COMPANY-FOOTER-SENTINEL";
        seeded.Active.Pages["seller"]["sellerNameLegalEntityPlaceholder"] = "COMPANY-NAME-SENTINEL";
        seeded.Active.Pages["seller"]["addressPlaceholder"] = "COMPANY-ADDRESS-SENTINEL";
        seeded.Active.Pages["privacy"]["bodyMarkdown"] = "PRIVACY-LEGAL-SENTINEL";
        seeded.Active.Pages["terms"]["bodyMarkdown"] = "TERMS-LEGAL-SENTINEL";
        seeded.Draft.Pages["mobile"]["pageTitle"] = "Mobile app coming soon";
        seeded.Draft.Pages["mobile"]["introText"] = "Custom Android introduction";
        seeded.Draft.Pages["mobile"]["googlePlayUrl"] = "https://example.invalid/wrong";
        await fixture.WriteDocumentAsync(new WebsiteContentDocument(seeded.Active, seeded.Draft));

        var upgraded = await fixture.CreateService().GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal("In development", upgraded.Active.Pages["home"]["mobileCardBadge"]);
        Assert.Equal("Android and iOS apps are planned but are not currently available.", upgraded.Active.Pages["home"]["mobileCardDescription"]);
        Assert.Equal("Custom Windows title", upgraded.Active.Pages["home"]["windowsCardTitle"]);
        Assert.Equal("COMPANY-FOOTER-SENTINEL", upgraded.Active.Pages["home"]["footerCopyrightText"]);
        Assert.Equal("COMPANY-NAME-SENTINEL", upgraded.Active.Pages["seller"]["sellerNameLegalEntityPlaceholder"]);
        Assert.Equal("COMPANY-ADDRESS-SENTINEL", upgraded.Active.Pages["seller"]["addressPlaceholder"]);
        Assert.Equal("PRIVACY-LEGAL-SENTINEL", upgraded.Active.Pages["privacy"]["bodyMarkdown"]);
        Assert.Equal("TERMS-LEGAL-SENTINEL", upgraded.Active.Pages["terms"]["bodyMarkdown"]);
        Assert.Equal("COMPANY-FOOTER-SENTINEL", upgraded.Draft.Pages["home"]["footerCopyrightText"]);
        Assert.Equal("COMPANY-NAME-SENTINEL", upgraded.Draft.Pages["seller"]["sellerNameLegalEntityPlaceholder"]);
        Assert.Equal("COMPANY-ADDRESS-SENTINEL", upgraded.Draft.Pages["seller"]["addressPlaceholder"]);
        Assert.Equal("PRIVACY-LEGAL-SENTINEL", upgraded.Draft.Pages["privacy"]["bodyMarkdown"]);
        Assert.Equal("TERMS-LEGAL-SENTINEL", upgraded.Draft.Pages["terms"]["bodyMarkdown"]);
        Assert.Equal("Mobile app coming soon", upgraded.Draft.Pages["mobile"]["pageTitle"]);
        Assert.Equal("Custom Android introduction", upgraded.Draft.Pages["mobile"]["introText"]);
        Assert.Equal("https://example.invalid/wrong", upgraded.Draft.Pages["mobile"]["googlePlayUrl"]);
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
    public async Task DefaultAndLegacyWebsiteDesignUseSafeIndependentFooterTextColor()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var seeded = await service.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal("#dce9f7", seeded.Active.Design.FooterTextColor);
        Assert.Equal("#dce9f7", seeded.Draft.Design.FooterTextColor);

        var documentJson = JsonNode.Parse(JsonSerializer.Serialize(new WebsiteContentDocument(seeded.Active, seeded.Draft), JsonOptions))!.AsObject();
        foreach (var contentKey in new[] { "active", "draft" })
        {
            var design = documentJson[contentKey]?["design"]?.AsObject() ?? throw new InvalidOperationException($"Missing {contentKey} design JSON.");
            Assert.True(design.Remove("footerTextColor"));
        }

        await File.WriteAllTextAsync(fixture.StorageJsonPath, documentJson.ToJsonString(JsonOptions), TestContext.Current.CancellationToken);
        var loaded = await fixture.CreateService().GetAsync(TestContext.Current.CancellationToken);
        var persistedJson = await File.ReadAllTextAsync(fixture.StorageJsonPath, TestContext.Current.CancellationToken);

        Assert.Equal("#dce9f7", loaded.Active.Design.FooterTextColor);
        Assert.Equal("#dce9f7", loaded.Draft.Design.FooterTextColor);
        Assert.Contains("\"footerTextColor\": \"#dce9f7\"", persistedJson);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-color")]
    public async Task BlankOrInvalidFooterTextColorNormalizesToSafeDefault(string footerTextColor)
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        content = content with { Design = content.Design with { FooterTextColor = footerTextColor } };

        var saved = await service.SaveDraftAsync(content, TestContext.Current.CancellationToken);

        Assert.Equal("#dce9f7", saved.Draft.Design.FooterTextColor);
    }

    [Fact]
    public async Task FooterTextColorSurvivesDraftReloadAndRendersIndependentlyInPreviewAndPublish()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        content = content with
        {
            Design = content.Design with
            {
                HeaderTextColor = "#17324D",
                FooterTextColor = "#EDE7DC"
            }
        };

        var saved = await service.SaveDraftAsync(content, TestContext.Current.CancellationToken);
        var reloaded = await fixture.CreateService().GetAsync(TestContext.Current.CancellationToken);
        var preview = await fixture.CreateService().PreviewAsync(new WebsitePreviewRequest(reloaded.Draft, "download"), TestContext.Current.CancellationToken);
        var published = await fixture.CreateService().PublishAsync(TestContext.Current.CancellationToken);
        var publishedHtml = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"), TestContext.Current.CancellationToken);

        Assert.Equal("#EDE7DC", saved.Draft.Design.FooterTextColor);
        Assert.Equal("#EDE7DC", reloaded.Draft.Design.FooterTextColor);
        Assert.Contains("--header-text: #17324D", preview.Html);
        Assert.Contains("--footer-text: #EDE7DC", preview.Html);
        Assert.Contains("color: #17324D", preview.Html);
        Assert.Equal("#EDE7DC", published.Active.Design.FooterTextColor);
        Assert.Contains("--header-text: #17324D", publishedHtml);
        Assert.Contains("--footer-text: #EDE7DC", publishedHtml);
    }

    [Fact]
    public void AdminWebsiteDesignEditorAndPublicStylesExposeIndependentPaletteControlsAndDetails()
    {
        var repositoryRoot = FindRepositoryRoot();
        var adminJs = File.ReadAllText(Path.Combine(repositoryRoot, "backend", "EnglishVoiceTutor.Api", "wwwroot", "admin", "admin.js"));
        var styles = File.ReadAllText(Path.Combine(repositoryRoot, "site", "public", "styles.css")).Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("[\"footerTextColor\", \"Footer text color\"]", adminJs);
        Assert.Contains("websiteDesignColorFields", adminJs);
        Assert.Contains("data-website-design-key", adminJs);
        Assert.Contains("websiteContentDraft.design ||= {}", adminJs);
        Assert.Contains("JSON.stringify(websiteContentDraft)", adminJs);
        Assert.Contains("activeWebsiteSection === \"home\" || activeWebsiteSection === \"marketingSeo\" || activeWebsiteSection === \"design\" ? \"download\"", adminJs);
        Assert.Contains(".site-footer > p {\n    white-space: pre-line;\n}", styles);
        Assert.Contains("color: #102A43", styles);
        Assert.Contains("color: #8A7557", styles);
        Assert.Contains("color: #FFFFFF", styles);
        Assert.Contains("border: 0", styles);
        Assert.Contains("box-shadow: none", styles);
        Assert.DoesNotContain("border: 1px solid rgba(23, 50, 77, 0.28)", styles);
        Assert.DoesNotContain("box-shadow: 0 1px 2px rgba(23, 50, 77, 0.18)", styles);
        Assert.DoesNotContain("#F2E8D5", styles, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#1B2A3A", styles, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#EDE7DC", styles, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewHtmlIncludesPublicBaseHrefSoAboutBlankCanResolveRelativeAssets()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;

        var preview = await service.PreviewAsync(new WebsitePreviewRequest(content, "download"), TestContext.Current.CancellationToken);

        Assert.Contains("<base href=\"https://languagevoicetutor.com/\">", preview.Html);
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
    public async Task LegalBodyMarkdownUpTo64000CharactersSavesReloadsPreviewsAndPublishesWithoutTruncation()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        const string sentinel = "LEGAL-END-SENTINEL";
        var legalMarkdown = CreateMarkdown(64000, sentinel);
        var content = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        content.Pages["privacy"]["bodyMarkdown"] = legalMarkdown;

        await service.SaveDraftAsync(content, TestContext.Current.CancellationToken);
        var reloaded = (await fixture.CreateService().GetAsync(TestContext.Current.CancellationToken)).Draft;
        var preview = await fixture.CreateService().PreviewAsync(new WebsitePreviewRequest(reloaded, "privacy"), TestContext.Current.CancellationToken);
        await fixture.CreateService().PublishAsync(TestContext.Current.CancellationToken);
        var published = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "privacy.html"), TestContext.Current.CancellationToken);

        Assert.Equal(legalMarkdown, reloaded.Pages["privacy"]["bodyMarkdown"]);
        Assert.EndsWith(sentinel, reloaded.Pages["privacy"]["bodyMarkdown"]);
        Assert.Contains(sentinel, preview.Html);
        Assert.Contains(sentinel, published);
    }

    [Fact]
    public async Task OversizedLegalBodyMarkdownIsRejectedForSavePreviewAndPublishWithoutReplacingSavedDraft()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        const string savedSentinel = "SAVED-LEGAL-DRAFT";
        var saved = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        saved.Pages["privacy"]["bodyMarkdown"] = savedSentinel;
        await service.SaveDraftAsync(saved, TestContext.Current.CancellationToken);

        var oversized = CreateMarkdown(64001, "OVERSIZED-LEGAL-END");
        saved.Pages["privacy"]["bodyMarkdown"] = oversized;
        var saveError = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveDraftAsync(saved, TestContext.Current.CancellationToken));
        var previewError = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PreviewAsync(new WebsitePreviewRequest(saved, "privacy"), TestContext.Current.CancellationToken));

        Assert.Contains("privacy.bodyMarkdown", saveError.Message);
        Assert.Contains("64,000", saveError.Message);
        Assert.Contains("privacy.bodyMarkdown", previewError.Message);
        Assert.Equal(savedSentinel, (await fixture.CreateService().GetAsync(TestContext.Current.CancellationToken)).Draft.Pages["privacy"]["bodyMarkdown"]);

        var stored = await service.GetAsync(TestContext.Current.CancellationToken);
        stored.Draft.Pages["privacy"]["bodyMarkdown"] = oversized;
        await fixture.WriteDocumentAsync(new WebsiteContentDocument(stored.Active, stored.Draft));
        var publishError = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.CreateService().PublishAsync(TestContext.Current.CancellationToken));

        Assert.Contains("privacy.bodyMarkdown", publishError.Message);
        Assert.Contains("64,000", publishError.Message);
    }

    [Fact]
    public async Task NonLegalBodyMarkdownAndExistingShortTextLimitsRemainUnchanged()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var oversizedDownload = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        oversizedDownload.Pages["download"]["bodyMarkdown"] = CreateMarkdown(12001, "DOWNLOAD-END");

        var bodyError = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveDraftAsync(oversizedDownload, TestContext.Current.CancellationToken));
        Assert.Contains("download.bodyMarkdown", bodyError.Message);
        Assert.Contains("12,000", bodyError.Message);

        var shortFields = (await service.GetAsync(TestContext.Current.CancellationToken)).Draft;
        shortFields.Pages["support"]["seoTitle"] = new string('s', 181);
        shortFields.Pages["support"]["pageTitle"] = new string('p', 901);
        var saved = await service.SaveDraftAsync(shortFields, TestContext.Current.CancellationToken);

        Assert.Equal(180, saved.Draft.Pages["support"]["seoTitle"].Length);
        Assert.Equal(900, saved.Draft.Pages["support"]["pageTitle"].Length);
    }

    [Fact]
    public void AdminWebsiteEditorDefinesPageAwareBodyMarkdownLimitsAndLiveCounterBehavior()
    {
        var adminJs = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "backend", "EnglishVoiceTutor.Api", "wwwroot", "admin", "admin.js"));

        Assert.Contains("longFormWebsitePageKeys", adminJs);
        Assert.Contains("longFormBodyMarkdownLimit = 64000", adminJs);
        Assert.Contains("standardBodyMarkdownLimit = 12000", adminJs);
        Assert.Contains("bodyMarkdownLimitForPage", adminJs);
        Assert.Contains("website-body-markdown-counter", adminJs);
        Assert.Contains("updateBodyMarkdownCounter", adminJs);
        Assert.Contains("validateWebsiteBodyMarkdown", adminJs);
        Assert.Contains("website-body-markdown-over-limit", adminJs);
    }

    [Fact]
    public void AdminWebsiteEditorExposesSharedSiteChromeWithoutRetiredPublicPages()
    {
        var adminJs = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "backend", "EnglishVoiceTutor.Api", "wwwroot", "admin", "admin.js"));

        Assert.Contains("Shared site chrome", adminJs);
        Assert.Contains("activeWebsiteSection === \"home\" || activeWebsiteSection === \"marketingSeo\" || activeWebsiteSection === \"design\" ? \"download\"", adminJs);
        Assert.DoesNotContain("Android app / Google Play", adminJs);
        Assert.DoesNotContain("renderMobileProductEditor", adminJs);
        Assert.DoesNotContain("Home page", adminJs);
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

    private static void AssertInOrder(string value, params string[] tokens)
    {
        var previousIndex = -1;
        foreach (var token in tokens)
        {
            var index = value.IndexOf(token, previousIndex + 1, StringComparison.Ordinal);
            Assert.True(index > previousIndex, $"Expected '{token}' after index {previousIndex}, but found it at {index}.");
            previousIndex = index;
        }
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

    private static string CreateMarkdown(int length, string sentinel)
    {
        Assert.True(length > sentinel.Length + 1);
        return new string('x', length - sentinel.Length - 1) + "\n" + sentinel;
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
