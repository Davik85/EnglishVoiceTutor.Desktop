using EnglishVoiceTutor.Api.Contracts.Website;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Website;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services.Website;

public sealed class WebsiteContentServiceRenderingTests
{
    [Fact]
    public async Task PublishedHomeHtmlContainsDefaultLogoAndFlagImageAssets()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();

        await service.PublishAsync(CancellationToken.None);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "index.html"));

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

        await service.PublishAsync(CancellationToken.None);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "index.html"));

        Assert.Contains("<img class=\"site-header__logo-image\" src=\"assets/brand/lvt-logo.png\"", html);
        Assert.DoesNotContain("<a class=\"site-header__brand\" href=\"index.html\" aria-label=\"Language Voice Tutor home\"><span class=\"site-header__logo-fallback\">", html);
    }


    [Fact]
    public async Task PublishedDownloadHtmlKeepsManifestDrivenReleaseHooks()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();

        await service.PublishAsync(CancellationToken.None);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "download.html"));

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
        var content = (await service.GetAsync(CancellationToken.None)).Draft;

        var preview = await service.PreviewAsync(new WebsitePreviewRequest(content, "home"), CancellationToken.None);

        Assert.Contains("<base href=\"https://languagevoicetutor.com/\">", preview.Html);
        Assert.Contains("assets/images/landing/windows-desktop.webp", preview.Html);
        Assert.Contains("assets/brand/lvt-logo.png", preview.Html);
        Assert.Contains("assets/flags/gb.webp", preview.Html);
    }

    [Fact]
    public async Task PublishedPagesIncludeFooterLinkToStatusPageAndPublishStatusHtml()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();

        var response = await service.PublishAsync(CancellationToken.None);
        var htmlFiles = Directory.GetFiles(fixture.PublicSiteRoot, "*.html");

        Assert.Contains(response.PublishedFiles, file => Path.GetFileName(file) == "status.html");
        foreach (var file in htmlFiles)
        {
            var html = await File.ReadAllTextAsync(file);
            Assert.Contains("href=\"status.html\"", html);
            Assert.Contains("Service Status", html);
        }
    }

    [Fact]
    public async Task PreviewStatusPageUsesPublicBaseHrefAndLegalPageShell()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(CancellationToken.None)).Draft;

        var preview = await service.PreviewAsync(new WebsitePreviewRequest(content, "status"), CancellationToken.None);

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

        await service.PublishAsync(CancellationToken.None);
        var html = await File.ReadAllTextAsync(Path.Combine(fixture.PublicSiteRoot, "index.html"));

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
    public async Task MarkdownLinksSafeUrlsAndEmailsAndRejectsUnsafeSchemes()
    {
        using var fixture = new WebsiteContentServiceFixture();
        var service = fixture.CreateService();
        var content = (await service.GetAsync(CancellationToken.None)).Draft;
        content.Pages["status"]["bodyMarkdown"] = "Visit https://example.com/docs. Email support@languagevoicetutor.com. [Contact](mailto:support@languagevoicetutor.com) [Bad](javascript:alert(1)) <script>alert(1)</script>";

        var preview = await service.PreviewAsync(new WebsitePreviewRequest(content, "status"), CancellationToken.None);

        Assert.Contains("<a href=\"https://example.com/docs\" rel=\"noopener noreferrer\">https://example.com/docs</a>.", preview.Html);
        Assert.Contains("<a href=\"mailto:support@languagevoicetutor.com\">support@languagevoicetutor.com</a>", preview.Html);
        Assert.Contains("<a href=\"mailto:support@languagevoicetutor.com\">Contact</a>", preview.Html);
        Assert.Contains("Bad", preview.Html);
        Assert.DoesNotContain("href=\"javascript:", preview.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script>", preview.Html, StringComparison.OrdinalIgnoreCase);
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
            var options = Options.Create(new WebsiteContentOptions
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
