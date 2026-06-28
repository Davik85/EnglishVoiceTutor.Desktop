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
