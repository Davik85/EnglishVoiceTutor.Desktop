using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishVoiceTutor.Api.Contracts.Website;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Website;

public sealed partial class WebsiteContentService(IOptions<WebsiteContentOptions> options, IWebHostEnvironment environment) : IWebsiteContentService
{
    private const string HeaderStart = "<!-- website-home-header:start -->";
    private const string HeaderEnd = "<!-- website-home-header:end -->";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<WebsiteHomeHeaderResponse> GetAsync(CancellationToken cancellationToken)
    {
        var document = await ReadDocumentAsync(cancellationToken);
        return new WebsiteHomeHeaderResponse(document.ActiveHomeHeader, document.DraftHomeHeader);
    }

    public async Task<WebsiteHomeHeaderResponse> SaveDraftAsync(WebsiteHomeHeaderContent draft, CancellationToken cancellationToken)
    {
        var document = await ReadDocumentAsync(cancellationToken);
        document = document with { DraftHomeHeader = Normalize(draft) };
        await WriteDocumentAsync(document, cancellationToken);
        return new WebsiteHomeHeaderResponse(document.ActiveHomeHeader, document.DraftHomeHeader);
    }

    public async Task<WebsitePublishResponse> PublishAsync(WebsiteHomeHeaderContent content, CancellationToken cancellationToken)
    {
        var active = Normalize(content);
        var document = await ReadDocumentAsync(cancellationToken);
        document = document with { ActiveHomeHeader = active, DraftHomeHeader = active };

        var publicRoot = ResolvePath(options.Value.PublicSiteRoot);
        var indexPath = Path.Combine(publicRoot, "index.html");
        if (!Directory.Exists(publicRoot) || !File.Exists(indexPath))
        {
            throw new InvalidOperationException($"Configured WebsiteContent:PublicSiteRoot does not contain index.html: {publicRoot}");
        }

        var html = await File.ReadAllTextAsync(indexPath, cancellationToken);
        var renderedHeader = RenderHeader(active);
        string updated;
        var start = html.IndexOf(HeaderStart, StringComparison.Ordinal);
        var end = html.IndexOf(HeaderEnd, StringComparison.Ordinal);
        if (start >= 0 && end > start)
        {
            end += HeaderEnd.Length;
            updated = html[..start] + renderedHeader + html[end..];
        }
        else
        {
            var mainIndex = html.IndexOf("    <main class=\"landing-shell\"", StringComparison.Ordinal);
            if (mainIndex < 0) { throw new InvalidOperationException("Public index.html does not contain the expected landing shell marker."); }
            updated = html[..mainIndex] + renderedHeader + Environment.NewLine + html[mainIndex..];
        }

        await File.WriteAllTextAsync(indexPath, updated, cancellationToken);
        await WriteDocumentAsync(document, cancellationToken);
        return new WebsitePublishResponse(active, indexPath, DateTimeOffset.UtcNow);
    }

    private async Task<WebsiteContentDocument> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        var path = ResolvePath(options.Value.StorageJsonPath);
        if (!File.Exists(path))
        {
            var defaults = new WebsiteContentDocument(DefaultHeader(), DefaultHeader());
            await WriteDocumentAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<WebsiteContentDocument>(stream, JsonOptions, cancellationToken);
        return document is null ? new WebsiteContentDocument(DefaultHeader(), DefaultHeader()) : new WebsiteContentDocument(Normalize(document.ActiveHomeHeader), Normalize(document.DraftHomeHeader));
    }

    private async Task WriteDocumentAsync(WebsiteContentDocument document, CancellationToken cancellationToken)
    {
        var path = ResolvePath(options.Value.StorageJsonPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? environment.ContentRootPath);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    private string ResolvePath(string configuredPath) => Path.IsPathRooted(configuredPath) ? configuredPath : Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", configuredPath));

    private static WebsiteHomeHeaderContent DefaultHeader() => new("", "Language Voice Tutor logo", false, "Language Voice Tutor", "Practice real conversations in:", "🇬🇧 English · 🇫🇷 French · 🇩🇪 German · 🇪🇸 Spanish · 🇮🇹 Italian · 🇵🇹 Portuguese", "system-ui, -apple-system, BlinkMacSystemFont, \"Segoe UI\", sans-serif", 18, 700, "#dce9f7", "#0d2b4c", 18, 64);

    private static WebsiteHomeHeaderContent Normalize(WebsiteHomeHeaderContent? value)
    {
        var defaults = DefaultHeader();
        if (value is null) { return defaults; }
        return new WebsiteHomeHeaderContent(
            NormalizeLogoPath(value.LogoPath), Limit(value.LogoAltText, 120, defaults.LogoAltText), value.ShowLogo,
            Limit(value.FallbackLogoText, 80, defaults.FallbackLogoText), Limit(value.HeaderText, 120, defaults.HeaderText),
            Limit(value.LanguageLine, 240, defaults.LanguageLine), NormalizeFontFamily(value.FontFamily, defaults.FontFamily),
            Math.Clamp(value.FontSizePx, 12, 48), AllowedFontWeights().Contains(value.FontWeight) ? value.FontWeight : defaults.FontWeight,
            NormalizeHex(value.TextColor, defaults.TextColor), NormalizeHex(value.HeaderBackgroundColor, defaults.HeaderBackgroundColor),
            Math.Clamp(value.PaddingBlockPx, 8, 48), Math.Clamp(value.PaddingInlinePx, 16, 96));
    }

    private static string RenderHeader(WebsiteHomeHeaderContent h)
    {
        var logo = h.ShowLogo && !string.IsNullOrWhiteSpace(h.LogoPath)
            ? $"<img class=\"site-header__logo-image\" src=\"{HtmlEncoder.Default.Encode(h.LogoPath)}\" alt=\"{HtmlEncoder.Default.Encode(h.LogoAltText)}\">"
            : $"<span class=\"site-header__logo-text\">{WebUtility.HtmlEncode(h.FallbackLogoText)}</span>";
        return $"    {HeaderStart}\n    <header class=\"site-header\" style=\"--site-header-bg: {h.HeaderBackgroundColor}; --site-header-text: {h.TextColor}; --site-header-font-family: {HtmlEncoder.Default.Encode(h.FontFamily)}; --site-header-font-size: {h.FontSizePx}px; --site-header-font-weight: {h.FontWeight}; --site-header-padding-block: {h.PaddingBlockPx}px; --site-header-padding-inline: {h.PaddingInlinePx}px;\">\n        <div class=\"site-header__logo\">{logo}</div>\n        <div class=\"site-header__copy\">\n            <p class=\"site-header__headline\">{WebUtility.HtmlEncode(h.HeaderText)}</p>\n            <p class=\"site-header__languages\">{WebUtility.HtmlEncode(h.LanguageLine)}</p>\n        </div>\n    </header>\n    {HeaderEnd}";
    }

    private static string Limit(string? value, int max, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string NormalizeHex(string? value, string fallback) => value is not null && HexColorRegex().IsMatch(value.Trim()) ? value.Trim() : fallback;
    private static string NormalizeFontFamily(string? value, string fallback) => value is not null && SafeFontRegex().IsMatch(value.Trim()) ? Limit(value, 120, fallback) : fallback;
    private static string NormalizeLogoPath(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0) { return string.Empty; }
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)) { return uri.Scheme == Uri.UriSchemeHttps ? trimmed : string.Empty; }
        return SafeRelativePathRegex().IsMatch(trimmed) && !trimmed.Contains("..", StringComparison.Ordinal) ? trimmed : string.Empty;
    }
    private static HashSet<int> AllowedFontWeights() => [400, 500, 600, 700, 800];
    [GeneratedRegex("^#[0-9a-fA-F]{6}$")] private static partial Regex HexColorRegex();
    [GeneratedRegex(@"^[a-zA-Z0-9 ,""\-]+$")] private static partial Regex SafeFontRegex();
    [GeneratedRegex("^[a-zA-Z0-9_./%#?=&:+-]+$")] private static partial Regex SafeRelativePathRegex();
}
