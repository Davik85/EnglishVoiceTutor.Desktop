using EnglishVoiceTutor.Api.Contracts.Website;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Website;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace EnglishVoiceTutor.Api.Tests.Services.Website;

public sealed class WebsiteContentServiceRenderingTests
{
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
        Assert.Contains("download.js?v=manifest-download", html);
        Assert.Contains("id=\"current-version\"", html);
        Assert.Contains("id=\"download-button\"", html);
        Assert.Contains("aria-disabled=\"true\"", html);
        Assert.Contains("id=\"manifest-status\"", html);
        Assert.Contains("id=\"detail-version\"", html);
        Assert.Contains("id=\"detail-installer\"", html);
        Assert.Contains("id=\"detail-backend-base-url\"", html);
        Assert.Contains("id=\"detail-minimum-supported-version\"", html);
        Assert.Contains("id=\"detail-update-mode\"", html);
        Assert.Contains("id=\"detail-size\"", html);
        Assert.Contains("id=\"detail-sha\"", html);
        Assert.Contains("Current version", html);
        Assert.Contains("Installer filename", html);
        Assert.Contains("Current release details", html);
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
        private string StorageJsonPath { get; }

        public WebsiteContentService CreateService()
        {
            var options = Microsoft.Extensions.Options.Options.Create(new WebsiteContentOptions
            {
                StorageJsonPath = StorageJsonPath,
                PublicSiteRoot = PublicSiteRoot
            });
            return new WebsiteContentService(options, new TestWebHostEnvironment(ContentRoot));
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
