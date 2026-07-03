using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishVoiceTutor.Api.Contracts.Website;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Website;

public sealed partial class WebsiteContentService(IOptions<WebsiteContentOptions> options, IWebHostEnvironment environment) : IWebsiteContentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private const string DefaultLogoPath = "assets/brand/lvt-logo.png";
    private const string PreviewPublicBaseHref = "https://languagevoicetutor.com/";
    private const string PublicSiteBaseUrl = "https://languagevoicetutor.com";
    private const string RequiredLanguageLine = "🇬🇧 English · 🇫🇷 French · 🇩🇪 German · 🇪🇸 Spanish · 🇮🇹 Italian · 🇵🇹 Portuguese";
    private const string ReleaseReadyDownloadPageTitle = "Language Voice Tutor for Windows";
    private const string ReleaseReadyDownloadSeoTitle = "Language Voice Tutor for Windows Download";
    private const string ReleaseReadyDownloadSeoDescription = "Download Language Voice Tutor for Windows and practice real conversations by text or voice with an AI tutor.";
    private const string WindowsDirectReleaseBasePath = "/releases/windows/direct/";
    private const string DefaultWindowsInstallerFileName = "LanguageVoiceTutorSetup-1.0.exe";
    private const string DefaultWindowsInstallerUrl = WindowsDirectReleaseBasePath + DefaultWindowsInstallerFileName;
    private const string ReleaseReadyDownloadBodyMarkdown = """
Download Language Voice Tutor for Windows. Practice real conversations by text or voice with an AI tutor, choose practical topics, start guided lessons, and improve step by step.

Current version and installer size are loaded from the release manifest.

Windows may show a SmartScreen warning because code signing is deferred.

Need help? Email support@languagevoicetutor.com.
""";

    private static readonly (string Label, string Title, string Description, string ImagePath)[] DefaultDownloadFeatureCards =
    [
        ("Quick Start", "Start quickly", "Open the app and jump into practical language practice in a few clicks.", "/assets/images/download/quick-start.webp"),
        ("Topics", "Choose practical topics", "Pick real-life situations like travel, work, daily life, and more.", "/assets/images/download/topics.webp"),
        ("Guided Lesson", "Learn step by step", "Practice inside a guided lesson with clear prompts, hints, and feedback.", "/assets/images/download/guided-lesson.webp"),
        ("Conversation", "Practice real conversation", "Switch to conversation mode and train natural speaking in a realistic dialogue.", "/assets/images/download/conversation.webp")
    ];
    private static readonly IReadOnlyDictionary<string, string> DefaultLanguageFlagPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["English"] = "assets/flags/gb.webp",
        ["French"] = "assets/flags/fr.webp",
        ["German"] = "assets/flags/de.webp",
        ["Spanish"] = "assets/flags/es.webp",
        ["Italian"] = "assets/flags/it.webp",
        ["Portuguese"] = "assets/flags/pt.webp"
    };

    public async Task<WebsiteContentResponse> GetAsync(CancellationToken cancellationToken)
    {
        var document = await ReadDocumentAsync(cancellationToken);
        return new WebsiteContentResponse(document.Active, document.Draft);
    }

    public async Task<WebsiteContentResponse> SaveDraftAsync(WebsiteContentSet draft, CancellationToken cancellationToken)
    {
        var document = await ReadDocumentAsync(cancellationToken);
        document = document with { Draft = MergeDraft(document.Draft, draft) };
        await WriteDocumentAsync(document, cancellationToken);
        return new WebsiteContentResponse(document.Active, document.Draft);
    }

    public Task<WebsitePreviewResponse> PreviewAsync(WebsitePreviewRequest request, CancellationToken cancellationToken)
    {
        var normalized = Normalize(request.Content);
        var pageKey = NormalizePageKey(request.PageKey);
        var html = RenderPage(normalized, pageKey, includePublicBaseHref: true);
        return Task.FromResult(new WebsitePreviewResponse(pageKey, html, DateTimeOffset.UtcNow));
    }

    public async Task<WebsitePublishResponse> PublishAsync(CancellationToken cancellationToken)
    {
        var document = await ReadDocumentAsync(cancellationToken);
        var active = Normalize(document.Draft);
        var publicRoot = ResolvePath(options.Value.PublicSiteRoot);
        if (!Directory.Exists(publicRoot)) { Directory.CreateDirectory(publicRoot); }
        var files = await RenderAllAsync(active, publicRoot, cancellationToken);
        await WriteDocumentAsync(new WebsiteContentDocument(active, active), cancellationToken);
        return new WebsitePublishResponse(active, publicRoot, files, DateTimeOffset.UtcNow);
    }

    private async Task<WebsiteContentDocument> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        var path = ResolvePath(options.Value.StorageJsonPath);
        if (!File.Exists(path))
        {
            var defaults = DefaultSet();
            var doc = new WebsiteContentDocument(defaults, defaults);
            await WriteDocumentAsync(doc, cancellationToken);
            return doc;
        }
        WebsiteContentDocument? document;
        await using (var stream = File.OpenRead(path))
        {
            document = await JsonSerializer.DeserializeAsync<WebsiteContentDocument>(stream, JsonOptions, cancellationToken);
        }

        if (document is null) { var d = DefaultSet(); return new WebsiteContentDocument(d, d); }
        var hasLegacyDownloadContent = IsLegacyDownloadContent(document.Active) || IsLegacyDownloadContent(document.Draft);
        var normalized = new WebsiteContentDocument(Normalize(document.Active), Normalize(document.Draft));
        if (hasLegacyDownloadContent)
        {
            await WriteDocumentAsync(normalized, cancellationToken);
        }

        return normalized;
    }


    private static WebsiteContentSet MergeDraft(WebsiteContentSet? existingDraft, WebsiteContentSet? incomingDraft)
    {
        var merged = Normalize(existingDraft);
        if (incomingDraft is null)
        {
            return merged;
        }

        var pages = merged.Pages.ToDictionary(k => k.Key, v => new Dictionary<string, string>(v.Value));
        if (incomingDraft.Pages is not null)
        {
            foreach (var (page, fields) in incomingDraft.Pages)
            {
                if (!pages.TryGetValue(page, out var target) || fields is null)
                {
                    continue;
                }

                foreach (var (key, value) in fields)
                {
                    var fallback = target.GetValueOrDefault(key, string.Empty);
                    target[key] = key == "logoPath"
                        ? NormalizeLogoPath(value, fallback)
                        : LimitText(value, TextLimitFor(key), fallback);
                }
            }
        }

        var design = incomingDraft.Design is null ? merged.Design : NormalizeDesign(incomingDraft.Design, merged.Design);
        var marketing = MergeMarketing(merged.Marketing, incomingDraft.Marketing);
        return Normalize(new WebsiteContentSet(pages, design, marketing));
    }

    private async Task WriteDocumentAsync(WebsiteContentDocument document, CancellationToken cancellationToken)
    {
        var path = ResolvePath(options.Value.StorageJsonPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? environment.ContentRootPath);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    private string ResolvePath(string configuredPath) => Path.IsPathRooted(configuredPath) ? configuredPath : Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", configuredPath));

    private static WebsiteContentSet Normalize(WebsiteContentSet? input)
    {
        var defaults = DefaultSet();
        var pages = defaults.Pages.ToDictionary(k => k.Key, v => new Dictionary<string, string>(v.Value));
        if (input?.Pages is not null)
        {
            foreach (var (page, fields) in input.Pages)
            {
                if (!pages.TryGetValue(page, out var target) || fields is null) { continue; }
                foreach (var (key, value) in fields)
                {
                    var fallback = target.GetValueOrDefault(key, string.Empty);
                    target[key] = key == "logoPath"
                        ? NormalizeLogoPath(value)
                        : LimitText(value, TextLimitFor(key), fallback);
                }
            }
        }
        pages["home"]["supportedLanguageLine"] = RequiredLanguageLine;
        pages["home"]["mobileCardDescription"] = "Android and iOS apps are planned but are not currently available.";
        pages["home"]["mobileComingSoonButtonText"] = "Not currently available";
        UpgradeLegacyDownloadContent(pages);
        EnsureDownloadFeatureCards(pages);
        var design = NormalizeDesign(input?.Design, defaults.Design);
        var marketing = NormalizeMarketing(input?.Marketing, defaults.Marketing);
        return new WebsiteContentSet(pages, design, marketing);
    }

    private static void UpgradeLegacyDownloadContent(Dictionary<string, Dictionary<string, string>> pages)
    {
        if (!pages.TryGetValue("download", out var download) || !IsLegacyDownloadContent(download))
        {
            return;
        }

        download["pageTitle"] = ReleaseReadyDownloadPageTitle;
        download["seoTitle"] = ReleaseReadyDownloadSeoTitle;
        download["seoDescription"] = ReleaseReadyDownloadSeoDescription;
        download["introText"] = ReleaseReadyDownloadSeoDescription;
        download["bodyMarkdown"] = ReleaseReadyDownloadBodyMarkdown;
    }

    private static void EnsureDownloadFeatureCards(Dictionary<string, Dictionary<string, string>> pages)
    {
        if (!pages.TryGetValue("download", out var download))
        {
            return;
        }

        for (var i = 0; i < DefaultDownloadFeatureCards.Length; i++)
        {
            var card = DefaultDownloadFeatureCards[i];
            var prefix = $"featureCard{i + 1}";
            download.TryAdd($"{prefix}Label", card.Label);
            download.TryAdd($"{prefix}Title", card.Title);
            download.TryAdd($"{prefix}Description", card.Description);
            download.TryAdd($"{prefix}ImagePath", card.ImagePath);
        }
    }

    private static bool IsLegacyDownloadContent(Dictionary<string, string> download) =>
        ContainsTesterDownload(download.GetValueOrDefault("pageTitle"))
        || ContainsTesterDownload(download.GetValueOrDefault("seoTitle"))
        || ContainsLegacyTesterBody(download.GetValueOrDefault("bodyMarkdown"));

    private static bool IsLegacyDownloadContent(WebsiteContentSet? content) =>
        content?.Pages is not null
        && content.Pages.TryGetValue("download", out var download)
        && download is not null
        && IsLegacyDownloadContent(download);

    private static bool ContainsTesterDownload(string? value) =>
        value?.IndexOf("tester download", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool ContainsLegacyTesterBody(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains("A Windows desktop app for practicing spoken languages with an AI tutor.", StringComparison.OrdinalIgnoreCase)
        && value.Contains("Current version details are loaded from the release manifest.", StringComparison.OrdinalIgnoreCase)
        && value.Contains("Windows may show a SmartScreen warning because code signing is deferred.", StringComparison.OrdinalIgnoreCase);

    private async Task<IReadOnlyList<string>> RenderAllAsync(WebsiteContentSet c, string root, CancellationToken ct)
    {
        var files = new List<string>();
        async Task W(string file, string html) { var path = Path.Combine(root, file); await File.WriteAllTextAsync(path, html, ct); files.Add(path); }
        var release = ReadStaticReleaseManifest(root);
        foreach (var (pageKey, fileName) in PageFiles())
        {
            await W(fileName, RenderPage(c, pageKey, release: pageKey == "download" ? release : null));
        }
        await W("robots.txt", RenderRobotsTxt());
        await W("sitemap.xml", RenderSitemapXml(DateTimeOffset.UtcNow));
        if (IsEnabled(c.Marketing, "enableLlmsTxt")) { await W("llms.txt", RenderLlmsTxt()); }
        await W("marketing-consent.js", RenderMarketingConsentJs());
        return files;
    }


    private static string NormalizePageKey(string? pageKey)
    {
        var key = string.IsNullOrWhiteSpace(pageKey) ? "home" : pageKey.Trim();
        return PageFiles().Any(page => page.PageKey == key) ? key : "home";
    }

    private static IReadOnlyList<(string PageKey, string FileName)> PageFiles() =>
    [
        ("home", "index.html"),
        ("download", "download.html"),
        ("mobile", "mobile.html"),
        ("pricing", "pricing.html"),
        ("support", "support.html"),
        ("terms", "terms.html"),
        ("privacy", "privacy.html"),
        ("refunds", "refunds.html"),
        ("cancellation", "cancellation.html"),
        ("seller", "seller.html"),
        ("aiData", "ai-data.html"),
        ("status", "status.html")
    ];

    private static string RenderPage(WebsiteContentSet c, string pageKey, bool includePublicBaseHref = false, StaticReleaseManifest? release = null) => pageKey switch
    {
        "home" => RenderHome(c, includePublicBaseHref),
        "download" => RenderDownload(c, includePublicBaseHref, release),
        "mobile" => RenderSimple(c, "mobile", "mobile-title", [("Android", "androidComingSoonText"), ("iOS", "iosComingSoonText"), ("Contact", "emailSupportCtaText")], null, includePublicBaseHref),
        "pricing" => RenderSimple(c, "pricing", "pricing-title", [("Free plan", "freePlanText"), ("Premium plan", "premiumPlanText"), ("Trial", "trialText"), ("Checkout status", "paddleLiveCheckoutDisclaimerText")], null, includePublicBaseHref),
        "support" => RenderSimple(c, "support", "support-title", [("Support email", "supportEmailText"), ("Response time", "responseTimeText"), ("Accounts and deletion", "accountDeletionSupportText"), ("Billing", "billingSupportText")], null, includePublicBaseHref),
        "terms" => RenderSimple(c, "terms", "terms-title", [("Effective date", "effectiveDate"), ("Accounts and use", "accountUseTerms"), ("AI and learning disclaimer", "aiLearningDisclaimer"), ("Billing and subscriptions", "billingSubscriptionTermsPlaceholder"), ("Contact", "contactSupportText")], null, includePublicBaseHref),
        "privacy" => RenderSimple(c, "privacy", "privacy-title", [("Effective date", "effectiveDate"), ("Data collected", "dataCollected"), ("Audio and transcription", "audioTranscriptionText"), ("AI processing", "aiProcessingText"), ("Account and payment data", "accountPaymentDataText"), ("Retention and deletion", "dataRetentionDeletionText"), ("Contact", "contactText")], null, includePublicBaseHref),
        "refunds" => RenderSimple(c, "refunds", "refunds-title", [("Effective date", "effectiveDate"), ("Refund eligibility", "refundEligibilityText"), ("How to request a refund", "howToRequestRefundText"), ("Payment provider note", "paddlePaymentProviderNote"), ("Contact", "contactText")], null, includePublicBaseHref),
        "cancellation" => RenderSimple(c, "cancellation", "cancellation-title", [("Effective date", "effectiveDate"), ("How to cancel", "howToCancelText"), ("Access until period end", "accessUntilPeriodEndText"), ("Support", "supportText")], null, includePublicBaseHref),
        "seller" => RenderSimple(c, "seller", "seller-title", [("Seller name / legal entity", "sellerNameLegalEntityPlaceholder"), ("Address", "addressPlaceholder"), ("Contact email", "contactEmail"), ("Tax, VAT, company registration", "taxVatCompanyRegistrationPlaceholder"), ("Paddle live review note", "paddleLiveReviewNote")], null, includePublicBaseHref),
        "aiData" => RenderSimple(c, "aiData", "ai-data-title", [("AI tutor disclosure", "aiTutorDisclosureText"), ("Voice and transcription", "voiceTranscriptionDisclosureText"), ("Data processing", "dataProcessingText"), ("User control and deletion", "userControlDeletionText")], null, includePublicBaseHref),
        "status" => RenderSimple(c, "status", "status-title", [("Desktop availability", "desktopAvailabilityText"), ("Mobile", "mobileComingSoonText"), ("Service availability", "serviceAvailabilityDisclaimer"), ("Support", "supportContactText")], null, includePublicBaseHref),
        _ => RenderHome(c, includePublicBaseHref)
    };

    private static string RenderHome(WebsiteContentSet c, bool includePublicBaseHref)
    {
        var h = c.Pages["home"];
        var main = $"""
<main class="landing-shell" aria-label="Language Voice Tutor applications">
    <a class="app-panel app-panel--windows" href="download.html">
        <img class="app-panel__image" src="assets/images/landing/windows-desktop.webp" alt="Preview image for the Language Voice Tutor desktop app">
        <span class="app-panel__shade"></span>
        <section class="app-panel__content">
            <p class="app-panel__eyebrow">{E(h["windowsCardBadge"])}</p>
            <h1>{E(h["windowsCardTitle"])}</h1>
            <p>{E(h["windowsCardDescription"])}</p>
            <span class="app-panel__cue">{E(h["windowsDownloadButtonText"])}</span>
        </section>
    </a>
    <section class="app-panel app-panel--mobile app-panel--inactive">
        <img class="app-panel__image" src="assets/images/landing/mobile.webp" alt="Preview image for future Language Voice Tutor mobile apps">
        <span class="app-panel__shade"></span>
        <div class="app-panel__content">
            <span class="app-panel__badge">{E(h["mobileCardBadge"])}</span>
            <h2>{E(h["mobileCardTitle"])}</h2>
            <p>{E(h["mobileCardDescription"])}</p>
            <span class="app-panel__cue app-panel__cue--disabled">{E(h["mobileComingSoonButtonText"])}</span>
        </div>
    </section>
</main>
""";
        return Shell(c, E(h["seoTitle"]), E(h["seoDescription"]), main, true, includePublicBaseHref, pageFileName: "index.html", jsonLd: RenderSoftwareApplicationJsonLd(null));
    }


    private static string RenderDownload(WebsiteContentSet c, bool includePublicBaseHref, StaticReleaseManifest? release)
    {
        _ = c.Pages["download"];
        var currentVersion = release?.Version ?? "Release details load from the public manifest.";
        var installerSize = release?.InstallerSize ?? "Unavailable";
        var manifestStatus = release is not null
            ? "Ready to download. Latest Windows version is shown above."
            : "If release details do not load automatically, please contact support@languagevoicetutor.com.";
        var installerUrl = release is null ? DefaultWindowsInstallerUrl : WindowsDirectReleaseBasePath + release.InstallerRelativeUrl;
        var installerFileName = release?.InstallerFileName ?? DefaultWindowsInstallerFileName;
        var downloadAttributes = $" href=\"{E(installerUrl)}\" download=\"{E(installerFileName)}\"";
        var downloadClass = "download-button download-button--hero";
        var ariaDisabled = "false";

        var featureCards = RenderDownloadFeatureCards(c.Pages["download"]);
        var body = $$"""
    <main>
        <section class="download-hero" aria-labelledby="product-title">
            <div class="download-hero__shade" aria-hidden="true"></div>
            <div class="download-hero__inner">
                <section class="download-cta-panel" aria-label="Windows download" data-manifest-url="/releases/windows/direct/latest.json">
                    <p class="eyebrow">Windows desktop app</p>
                    <h1 id="product-title">Language Voice Tutor for Windows</h1>
                    <p class="download-hero__subtitle">Practice real conversations by text or voice with an AI tutor. Choose a topic, start a lesson, and improve step by step.</p>
                    <p class="version-line">Current version: <strong id="current-version">{{E(currentVersion)}}</strong> <span aria-hidden="true">·</span> Installer size: <strong id="installer-size">{{E(installerSize)}}</strong></p>
                    <a id="download-button" class="{{downloadClass}}" aria-disabled="{{ariaDisabled}}"{{downloadAttributes}}>Download for Windows</a>
                    <p id="manifest-status" class="download-hero__status" role="status">{{E(manifestStatus)}}</p>
                    <p class="download-hero__note">Windows may show a SmartScreen warning because code signing is deferred.</p>
                    <p class="download-cta-support">Need help? Email <a href="mailto:support@languagevoicetutor.com">support@languagevoicetutor.com</a>.</p>
                </section>

{{featureCards}}
            </div>
        </section>

    </main>
""";
        return Shell(c, "Language Voice Tutor for Windows Download", "Download Language Voice Tutor for Windows and practice real conversations by text or voice with an AI tutor.", body, false, includePublicBaseHref, "    <script src=\"download.js?v=20260703-lightbox\" defer></script>", pageFileName: "download.html", jsonLd: RenderSoftwareApplicationJsonLd(release));
    }

    private static string RenderDownloadFeatureCards(Dictionary<string, string> download)
    {
        var builder = new StringBuilder();
        builder.AppendLine("                <section class=\"download-feature-grid\" aria-label=\"Language Voice Tutor features\">");
        for (var i = 0; i < DefaultDownloadFeatureCards.Length; i++)
        {
            var card = DefaultDownloadFeatureCards[i];
            var prefix = $"featureCard{i + 1}";
            var label = ValueOrDefault(download, $"{prefix}Label", card.Label);
            var title = ValueOrDefault(download, $"{prefix}Title", card.Title);
            var description = ValueOrDefault(download, $"{prefix}Description", card.Description);
            var imagePath = NormalizeDownloadImagePath(ValueOrDefault(download, $"{prefix}ImagePath", card.ImagePath));
            var style = string.IsNullOrWhiteSpace(imagePath) ? string.Empty : $" style=\"--download-card-image: url('{E(imagePath)}');\"";
            var lightboxAttributes = string.IsNullOrWhiteSpace(imagePath)
                ? string.Empty
                : $" role=\"button\" tabindex=\"0\" data-download-lightbox-src=\"{E(imagePath)}\" data-download-lightbox-alt=\"{E(title)} screenshot\"";
            builder.AppendLine("                    <article class=\"download-feature-card\">");
            builder.AppendLine($"                        <div class=\"download-feature-card__visual\" aria-label=\"Open {E(title)} screenshot larger\"{style}{lightboxAttributes}><span>{E(label)}</span></div>");
            builder.AppendLine($"                        <h2>{E(title)}</h2>");
            builder.AppendLine($"                        <p>{E(description)}</p>");
            builder.AppendLine("                    </article>");
        }
        builder.Append("                </section>");
        return builder.ToString();
    }

    private static string ValueOrDefault(Dictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static string NormalizeDownloadImagePath(string? value)
    {
        var path = NormalizeLogoPath(value ?? string.Empty);
        return path.StartsWith("/", StringComparison.Ordinal) ? path[1..] : path;
    }

    private static string RenderSimple(WebsiteContentSet c, string page, string titleId, (string title, string key)[] sections, string? button, bool includePublicBaseHref)
    {
        var p = c.Pages[page];
        var body = new StringBuilder();
        body.Append($"""
<main class="page-shell legal-page">
    <section class="hero-card">
        <h1 id="{titleId}">{E(p["pageTitle"])}</h1>
""");
        if (!string.IsNullOrWhiteSpace(p.GetValueOrDefault("bodyMarkdown")))
        {
            body.AppendLine("    </section>");
            body.AppendLine("    <section class=\"details-card legal-section markdown-content\">");
            body.AppendLine(RenderMarkdown(p["bodyMarkdown"]));
            body.AppendLine("    </section>");
            body.Append(Nav());
            body.AppendLine("</main>");
            return Shell(c, E(p["seoTitle"]), E(p["seoDescription"]), body.ToString(), false, includePublicBaseHref, pageFileName: PageFileName(page), jsonLd: page == "pricing" ? RenderSoftwareApplicationJsonLd(null) : null);
        }
        body.AppendLine($"        <p class=\"description\">{E(p.GetValueOrDefault("introText", p.GetValueOrDefault("intro", string.Empty)))}</p>");
        if (button is not null)
        {
            body.AppendLine($"        <a class=\"download-button\" href=\"#\" aria-disabled=\"true\">{E(p.GetValueOrDefault("downloadButtonText", button))}</a>");
        }
        body.AppendLine("    </section>");
        foreach (var section in sections)
        {
            body.AppendLine($"""
    <section class="details-card legal-section">
        <h2>{E(section.title)}</h2>
        <p>{E(p[section.key])}</p>
    </section>
""");
        }
        body.Append(Nav());
        body.AppendLine("</main>");
        return Shell(c, E(p["seoTitle"]), E(p["seoDescription"]), body.ToString(), false, includePublicBaseHref, pageFileName: PageFileName(page), jsonLd: page == "pricing" ? RenderSoftwareApplicationJsonLd(null) : null);
    }

    private static string Shell(WebsiteContentSet c, string title, string description, string main, bool landing, bool includePublicBaseHref, string? extraBodyHtml = null, string pageFileName = "index.html", string? jsonLd = null)
    {
        var h = c.Pages["home"];
        var d = c.Design;
        var cardFontStyle = d.CardTextStyle.Contains("italic", StringComparison.OrdinalIgnoreCase) ? "italic" : "normal";
        var bodyClass = landing ? "landing-page" : string.Empty;
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"utf-8\">");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        if (includePublicBaseHref) { html.AppendLine($"    <base href=\"{PreviewPublicBaseHref}\">"); }
        var canonicalUrl = CanonicalUrl(pageFileName);
        html.AppendLine($"    <title>{title}</title>");
        html.AppendLine($"    <meta name=\"description\" content=\"{description}\">");
        html.AppendLine("    <meta name=\"robots\" content=\"index,follow\">");
        html.AppendLine($"    <link rel=\"canonical\" href=\"{canonicalUrl}\">");
        html.AppendLine("    <link rel=\"icon\" href=\"assets/brand/lvt-logo.png\">");
        html.AppendLine($"    <meta property=\"og:title\" content=\"{title}\">");
        html.AppendLine($"    <meta property=\"og:description\" content=\"{description}\">");
        html.AppendLine($"    <meta property=\"og:url\" content=\"{canonicalUrl}\">");
        html.AppendLine("    <meta property=\"og:type\" content=\"website\">");
        html.AppendLine("    <meta name=\"twitter:card\" content=\"summary\">");
        html.AppendLine($"    <meta name=\"twitter:title\" content=\"{title}\">");
        html.AppendLine($"    <meta name=\"twitter:description\" content=\"{description}\">");
        AppendSearchConsoleVerification(html, c.Marketing);
        if (!string.IsNullOrWhiteSpace(jsonLd)) { html.AppendLine(jsonLd); }
        html.AppendLine("    <link rel=\"stylesheet\" href=\"styles.css\">");
        html.AppendLine("    <style>");
        html.Append(RenderPreviewBaseStyles());
        html.AppendLine($"        :root {{ --footer-background: {d.FooterBackgroundColor}; --footer-text: {d.HeaderTextColor}; --text: {d.MainTextColor}; font-size: {d.BaseFontSizePx}px; }}");
        html.AppendLine($"        body {{ font-family: {d.MainFontFamily}; }}");
        html.AppendLine($"        .download-button, .app-panel__cue {{ border-radius: {d.ButtonBorderRadiusPx}px; }}");
        html.AppendLine($"        .site-header {{ background: {d.HeaderBackgroundColor}; color: {d.HeaderTextColor}; font-weight: {d.HeaderFontWeight}; }}");
        html.AppendLine($"        .landing-page .app-panel__content {{ font-style: {cardFontStyle}; }}");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine($"<body class=\"{bodyClass}\">");
        html.AppendLine("    <header class=\"site-header\" aria-label=\"Language Voice Tutor site header\">");
        html.AppendLine("        <div class=\"site-header__inner\">");
        html.AppendLine($"            <a class=\"site-header__brand\" href=\"index.html\" aria-label=\"Language Voice Tutor home\">{Logo(h)}</a>");
        html.AppendLine("            <div class=\"site-header__conversation-line\" aria-label=\"Supported study languages\">");
        html.AppendLine($"                <span class=\"site-header__headline\">{E(h["topHeaderText"])}</span>");
        html.AppendLine("                " + RenderLanguageList(h["supportedLanguageLine"]));
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");
        html.AppendLine("    </header>");
        html.AppendLine(main);
        html.AppendLine("    <footer class=\"site-footer\">");
        html.AppendLine($"        <p>{E(h["footerCopyrightText"])}</p>");
        html.AppendLine($"        {NavLinks(h)}");
        html.AppendLine("    </footer>");
        if (IsEnabled(c.Marketing, "enableConsentBanner")) { html.AppendLine(RenderConsentBanner(c.Marketing)); }
        if (!string.IsNullOrWhiteSpace(extraBodyHtml)) { html.AppendLine(extraBodyHtml); }
        html.AppendLine(RenderMarketingRuntime(c.Marketing));
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }


    private static string MarketingValue(Dictionary<string, string>? m, string key) => m is not null && m.TryGetValue(key, out var value) ? value : string.Empty;
    private static string PageFileName(string pageKey) => PageFiles().FirstOrDefault(p => p.PageKey == pageKey).FileName ?? "index.html";
    private static string CanonicalUrl(string fileName) => fileName == "index.html" ? PublicSiteBaseUrl + "/" : PublicSiteBaseUrl + "/" + fileName;

    private static void AppendSearchConsoleVerification(StringBuilder html, Dictionary<string, string>? m)
    {
        var token = SafeSearchConsoleToken(MarketingValue(m, "googleSearchConsoleVerificationToken"));
        if (!string.IsNullOrEmpty(token)) { html.AppendLine($"    <meta name=\"google-site-verification\" content=\"{E(token)}\">"); }
    }

    private static string RenderMarketingRuntime(Dictionary<string, string>? m)
    {
        var ga = IsEnabled(m, "enableAnalytics") ? SafeGaId(MarketingValue(m, "googleAnalyticsMeasurementId")) : string.Empty;
        var ads = IsEnabled(m, "enableAdsTracking") ? SafeAdsId(MarketingValue(m, "googleAdsId")) : string.Empty;
        var downloadLabel = SafeConversionLabel(MarketingValue(m, "googleAdsDownloadConversionLabel"));
        return $$"""
    <script>
      window.lvtMarketing = { gaMeasurementId: '{{E(ga)}}', googleAdsId: '{{E(ads)}}', downloadConversionLabel: '{{E(downloadLabel)}}' };
    </script>
    <script src="marketing-consent.js?v=marketing-seo" defer></script>
""";
    }

    private static string RenderConsentBanner(Dictionary<string, string>? m) => """
    <section class="consent-banner" id="consent-banner" role="dialog" aria-modal="false" aria-labelledby="consent-title" aria-describedby="consent-description" hidden>
      <div class="consent-banner__content">
        <div class="consent-banner__copy">
          <p class="consent-banner__eyebrow" id="consent-title">Optional cookies</p>
          <p id="consent-description">We may use analytics and advertising cookies, if enabled, to understand traffic, improve Language Voice Tutor, measure marketing performance, and count download clicks. You can accept all, reject non-essential cookies, or manage choices. <a class="consent-banner__link" href="privacy.html">Privacy Policy</a></p>
        </div>
        <div class="consent-choices" id="consent-manage" hidden>
          <p class="consent-choices__title">Manage optional categories</p>
          <label class="consent-choice"><input type="checkbox" id="consent-analytics"> <span><strong>Analytics cookies</strong><small>Help us measure site traffic and product improvement signals.</small></span></label>
          <label class="consent-choice"><input type="checkbox" id="consent-advertising"> <span><strong>Advertising cookies</strong><small>Help us measure marketing performance and download conversions if Google Ads is enabled.</small></span></label>
        </div>
        <div class="consent-banner__actions" aria-label="Cookie consent actions">
          <button class="consent-button consent-button--primary" type="button" data-consent-action="accept">Accept all</button>
          <button class="consent-button consent-button--secondary" type="button" data-consent-action="reject">Reject non-essential</button>
          <button class="consent-button consent-button--link" type="button" data-consent-action="manage">Manage choices</button>
          <button class="consent-button consent-button--primary" type="button" data-consent-action="save" id="consent-save" hidden>Save choices</button>
        </div>
      </div>
    </section>
""";

    private static string RenderSoftwareApplicationJsonLd(StaticReleaseManifest? r) => "    <script type=\"application/ld+json\">" + JsonSerializer.Serialize(new Dictionary<string, object?> { ["@context"]="https://schema.org", ["@type"]="SoftwareApplication", ["name"]="Language Voice Tutor", ["operatingSystem"]="Windows", ["applicationCategory"]="EducationalApplication", ["url"]=PublicSiteBaseUrl + "/download.html", ["downloadUrl"]=PublicSiteBaseUrl + "/download.html", ["softwareVersion"]=r?.Version ?? string.Empty }) + "</script>";

    private static string RenderRobotsTxt() => """
User-agent: *
Allow: /
Disallow: /admin/
Disallow: /api/
Disallow: /releases/windows/direct/*.exe

Sitemap: https://languagevoicetutor.com/sitemap.xml
""";
    private static string RenderSitemapXml(DateTimeOffset generatedAt) { var lastmod = generatedAt.ToString("yyyy-MM-dd"); var urls = new[] { "/", "/index.html", "/download.html", "/mobile.html", "/pricing.html", "/support.html", "/terms.html", "/privacy.html", "/refunds.html", "/cancellation.html", "/seller.html", "/ai-data.html", "/status.html" }; return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n" + string.Join("", urls.Select(u => $"  <url><loc>{PublicSiteBaseUrl}{u}</loc><lastmod>{lastmod}</lastmod></url>\n")) + "</urlset>\n"; }
    private static string RenderLlmsTxt() => """
# Language Voice Tutor

Language Voice Tutor is a Windows desktop application for practicing real-life spoken language lessons with AI-assisted conversation scenarios.

Windows desktop tester/direct release is available. Android and iOS apps are planned but not currently available. Live paid subscriptions are not enabled until Paddle live setup is completed.

## Important links
- Home: https://languagevoicetutor.com/
- Download: https://languagevoicetutor.com/download.html
- Pricing: https://languagevoicetutor.com/pricing.html
- Terms: https://languagevoicetutor.com/terms.html
- Privacy: https://languagevoicetutor.com/privacy.html
- Refund Policy: https://languagevoicetutor.com/refunds.html
- Cancellation Policy: https://languagevoicetutor.com/cancellation.html
- Support: https://languagevoicetutor.com/support.html
- Seller / Company Details: https://languagevoicetutor.com/seller.html
- AI & Data Disclosure: https://languagevoicetutor.com/ai-data.html
- Service Status: https://languagevoicetutor.com/status.html
""";

    private static string RenderMarketingConsentJs() => """
const consentKey = "lvt_marketing_consent_v1";
const deniedConsent = { analytics_storage: "denied", ad_storage: "denied", ad_user_data: "denied", ad_personalization: "denied" };
function hasGtag() { return typeof window.gtag === "function"; }
function readConsent() { try { return JSON.parse(localStorage.getItem(consentKey) || "null"); } catch { return null; } }
function writeConsent(choice) { localStorage.setItem(consentKey, JSON.stringify({ ...choice, savedAt: new Date().toISOString() })); }
function ensureDataLayer() { window.dataLayer = window.dataLayer || []; if (!hasGtag()) { window.gtag = function(){ window.dataLayer.push(arguments); }; } }
function consentUpdate(choice) {
    const update = {
        analytics_storage: choice?.analytics ? "granted" : "denied",
        ad_storage: choice?.advertising ? "granted" : "denied",
        ad_user_data: choice?.advertising ? "granted" : "denied",
        ad_personalization: choice?.advertising ? "granted" : "denied"
    };
    ensureDataLayer(); window.gtag("consent", "update", update);
}
function loadGoogleTags(choice) {
    const config = window.lvtMarketing || {};
    const tagId = choice?.analytics && config.gaMeasurementId ? config.gaMeasurementId : choice?.advertising && config.googleAdsId ? config.googleAdsId : "";
    if (!tagId || document.querySelector("script[data-lvt-google-tag]")) return;
    ensureDataLayer(); window.gtag('consent', 'default', deniedConsent); window.gtag("js", new Date());
    if (choice?.analytics && config.gaMeasurementId) { window.gtag("config", config.gaMeasurementId); }
    if (choice?.advertising && config.googleAdsId) { window.gtag("config", config.googleAdsId); }
    const script = document.createElement("script"); script.async = true; script.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(tagId)}`; script.dataset.lvtGoogleTag = "true"; document.head.appendChild(script);
}
function applyConsent(choice) { consentUpdate(choice); loadGoogleTags(choice); }
function trackDownloadClick() {
    const config = window.lvtMarketing || {};
    const choice = readConsent();
    if (hasGtag() && config.gaMeasurementId && choice?.analytics) {
        window.gtag("event", "download_windows_click", { platform: "windows", transport_type: "beacon" });
    }
    if (hasGtag() && config.googleAdsId && config.downloadConversionLabel && choice?.advertising) {
        window.gtag("event", "conversion", { send_to: `${config.googleAdsId}/${config.downloadConversionLabel}`, transport_type: "beacon" });
    }
}
ensureDataLayer(); window.gtag('consent', 'default', deniedConsent);
window.addEventListener("DOMContentLoaded", () => {
    const existing = readConsent();
    if (existing) { applyConsent(existing); }
    document.getElementById("download-button")?.addEventListener("click", trackDownloadClick);
    const banner = document.getElementById("consent-banner");
    if (!banner || existing) return;
    const manage = document.getElementById("consent-manage");
    const save = document.getElementById("consent-save");
    const analytics = document.getElementById("consent-analytics");
    const advertising = document.getElementById("consent-advertising");
    banner.hidden = false;
    banner.addEventListener("click", event => {
        const action = event.target?.closest("button")?.dataset?.consentAction;
        if (!action) return;
        if (action === "manage") { manage.hidden = false; save.hidden = false; return; }
        const choice = action === "accept" ? { analytics: true, advertising: true } : action === "save" ? { analytics: !!analytics.checked, advertising: !!advertising.checked } : { analytics: false, advertising: false };
        writeConsent(choice); applyConsent(choice); banner.hidden = true;
    });
});
""";

    private static string Logo(Dictionary<string, string> h)
    {
        var logoPath = NormalizeLogoPath(h.GetValueOrDefault("logoPath"), DefaultLogoPath);
        return $"<img class=\"site-header__logo-image\" src=\"{HtmlEncoder.Default.Encode(logoPath)}\" alt=\"{E(h.GetValueOrDefault("logoAltText", "Language Voice Tutor logo"))}\"><span class=\"site-header__logo-fallback\" hidden>{E(h.GetValueOrDefault("fallbackLogoText", "Language Voice Tutor"))}</span>";
    }

    private static string RenderLanguageList(string languageLine)
    {
        var languages = languageLine.Split('·', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return string.Join("<span class=\"site-header__separator\">·</span>", languages.Select(RenderLanguage));
    }

    private static string RenderLanguage(string language)
    {
        var label = LanguageLabelRegex().Replace(language, string.Empty).Trim();
        if (DefaultLanguageFlagPaths.TryGetValue(label, out var flagPath))
        {
            return $"<span class=\"site-header__language\"><img class=\"site-header__flag\" src=\"{HtmlEncoder.Default.Encode(flagPath)}\" alt=\"{E(label)} flag\">{E(label)}</span>";
        }

        return $"<span class=\"site-header__language\">{E(language)}</span>";
    }

    private static string RenderPreviewBaseStyles() => """
        :root { color-scheme: light; --background: #f6f2ea; --card: #fffaf2; --text: #24201b; --muted: #665f55; --accent: #2f6f5e; --accent-dark: #235648; --border: #ded2bf; --warning-background: #fff2cd; --warning-border: #e2b84f; --footer-background: #0d2b4c; --footer-text: #dce9f7; --footer-link: #ffffff; }
        * { box-sizing: border-box; }
        body { margin: 0; min-height: 100vh; background: var(--background); color: var(--text); line-height: 1.5; }
        body.landing-page { display: flex; min-width: 0; flex-direction: column; overflow-x: hidden; }
        .site-header { display: flex; min-height: 88px; align-items: center; overflow: hidden; width: 100%; }
        .site-header__inner { display: flex; width: 100%; min-height: 88px; align-items: center; justify-content: flex-start; gap: 22px; padding: 12px clamp(20px, 5vw, 72px); }
        .site-header__brand { display: inline-flex; flex: 0 0 auto; max-width: 220px; align-items: center; color: inherit; font-size: 1.2rem; font-weight: 850; text-decoration: none; }
        .site-header__logo-image { display: block; width: auto; max-width: 180px; max-height: 72px; height: auto; object-fit: contain; }
        .site-header__logo-fallback { display: inline-flex; align-items: center; min-height: 48px; }
        .site-header__conversation-line { display: flex; min-width: 0; align-items: center; flex-wrap: wrap; gap: 8px; }
        .site-header__headline { margin: 0; color: inherit; font-weight: 750; }
        .site-header__language { display: inline-flex; align-items: center; gap: 6px; white-space: nowrap; }
        .site-header__flag { display: inline-block; width: 22px; height: 15px; border-radius: 2px; object-fit: cover; box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.35); }
        .site-header__separator { opacity: 0.72; }
        .landing-page .landing-shell { display: grid; width: 100%; max-width: 100vw; flex: 1 1 auto; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); min-height: clamp(420px, calc(100svh - 176px), 760px); background: #07192c; overflow: hidden; }
        @supports (height: 100dvh) { .landing-page .landing-shell { min-height: clamp(420px, calc(100dvh - 176px), 760px); } }
        .landing-page .app-panel { position: relative; display: flex; min-width: 0; min-height: 100%; isolation: isolate; overflow: hidden; color: #ffffff; text-decoration: none; }
        .landing-page .app-panel__image, .landing-page .app-panel__shade { position: absolute; inset: 0; }
        .landing-page .app-panel__image { display: block; width: 100%; height: 100%; object-fit: cover; transform: scale(1.01); z-index: 0; }
        .landing-page .app-panel__shade { background: linear-gradient(180deg, rgba(4, 18, 32, 0.28) 0%, rgba(4, 18, 32, 0.08) 45%, rgba(4, 18, 32, 0.3) 100%); z-index: 1; }
        .landing-page .app-panel__content { position: relative; z-index: 2; display: flex; width: min(560px, calc(100% - 40px)); min-height: clamp(280px, 30svh, 390px); max-height: calc(100% - clamp(32px, 8svh, 96px)); flex-direction: column; align-items: flex-start; margin: clamp(16px, 4svh, 48px) auto; padding: clamp(20px, 3vw, 34px); border: 1px solid rgba(255, 255, 255, 0.24); border-radius: 28px; background: rgba(5, 22, 38, 0.3); box-shadow: 0 22px 70px rgba(0, 0, 0, 0.22); backdrop-filter: blur(4px); overflow: auto; }
        .landing-page .app-panel__eyebrow, .landing-page .app-panel__badge { display: inline-flex; width: fit-content; margin: 0 0 16px; border-radius: 999px; font-size: 0.82rem; font-weight: 800; letter-spacing: 0.08em; text-transform: uppercase; }
        .landing-page .app-panel__badge { padding: 8px 12px; background: rgba(255, 255, 255, 0.2); }
        .landing-page .app-panel h1, .landing-page .app-panel h2 { margin: 0 0 18px; font-size: clamp(2.1rem, 5vw, 4.7rem); line-height: 0.98; text-wrap: balance; }
        .landing-page .app-panel p { max-width: 32rem; margin: 0; color: rgba(255, 255, 255, 0.92); font-size: clamp(1rem, 1.6vw, 1.28rem); }
        .landing-page .app-panel__cue { display: inline-flex; align-items: center; margin-top: auto; padding: 13px 18px; background: #ffffff; color: #0d2b4c; font-weight: 850; box-shadow: 0 14px 34px rgba(0, 0, 0, 0.2); }
        .landing-page .app-panel__cue--disabled { cursor: not-allowed; background: rgba(255, 255, 255, 0.22); color: rgba(255, 255, 255, 0.76); box-shadow: none; }
        .site-footer { display: flex; min-height: 88px; align-items: center; justify-content: space-between; gap: 20px; padding: 22px clamp(20px, 5vw, 64px); background: var(--footer-background); color: var(--footer-text); }
        .site-footer p { margin: 0; }
        .site-footer__links, .legal-nav { display: flex; flex-wrap: wrap; gap: 10px 18px; }
        .site-footer__links { display: flex; flex-direction: column; align-items: flex-end; gap: 8px; }
        .site-footer__link-row { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 10px 18px; }
        .site-footer a { color: var(--footer-link); font-weight: 700; text-decoration-color: rgba(255, 255, 255, 0.45); text-underline-offset: 4px; }
        .page-shell { width: min(920px, calc(100% - 32px)); margin: 0 auto; padding: 48px 0; }
        .hero-card, .details-card, .support-card { background: var(--card); border: 1px solid var(--border); border-radius: 18px; box-shadow: 0 16px 45px rgba(64, 49, 30, 0.08); }
        .hero-card { padding: clamp(28px, 5vw, 56px); }
        h1, h2, p { margin-top: 0; }
        .page-shell h1 { margin-bottom: 16px; font-size: clamp(2.3rem, 7vw, 4.5rem); line-height: 1; }
        .page-shell h2 { margin-bottom: 18px; font-size: 1.35rem; }
        .description { max-width: 680px; margin-bottom: 22px; color: var(--muted); font-size: 1.18rem; }
        .download-button { display: inline-flex; width: fit-content; align-items: center; justify-content: center; min-height: 52px; padding: 0 24px; background: var(--accent); color: #ffffff; font-weight: 800; text-decoration: none; }
        .download-button[aria-disabled="true"] { cursor: not-allowed; background: #8b958f; color: #f4f4f4; pointer-events: none; }
        .details-card, .support-card { margin-top: 20px; padding: 24px; }
        .legal-page .hero-card, .legal-page .details-card, .legal-page .support-card { margin-bottom: 20px; }
        .legal-section p:last-child { margin-bottom: 0; }
        .markdown-content blockquote { border-left: 4px solid var(--accent); color: var(--muted); margin: 1rem 0; padding-left: 1rem; }
        .markdown-content hr { border: 0; border-top: 1px solid var(--border); margin: 1.5rem 0; }
        .markdown-content li { margin-bottom: 0.45rem; }
        .page-shell a { color: var(--accent-dark); }
        .consent-banner { position: fixed; z-index: 50; inset: auto clamp(12px, 4vw, 32px) clamp(12px, 4vw, 28px); max-width: 960px; margin: 0 auto; border: 1px solid rgba(220, 233, 247, 0.38); border-radius: 24px; background: linear-gradient(135deg, #0d2b4c 0%, #123a66 100%); color: #ffffff; box-shadow: 0 24px 70px rgba(7, 25, 44, 0.32); }
        .consent-banner[hidden] { display: none; }
        .consent-banner__content { display: grid; gap: 16px; padding: clamp(18px, 3vw, 26px); }
        .consent-banner__copy p { margin: 0; }
        .consent-banner__eyebrow { margin-bottom: 6px !important; font-size: 0.82rem; font-weight: 850; letter-spacing: 0.08em; text-transform: uppercase; color: #dce9f7; }
        .consent-banner__link { color: #ffffff; font-weight: 800; text-decoration-color: rgba(255, 255, 255, 0.65); text-underline-offset: 4px; }
        .consent-banner__actions { display: flex; flex-wrap: wrap; gap: 10px; }
        .consent-button { min-height: 42px; border: 1px solid transparent; border-radius: 999px; padding: 0 16px; cursor: pointer; font: inherit; font-weight: 800; }
        .consent-button:focus-visible, .consent-choice input:focus-visible { outline: 3px solid #dce9f7; outline-offset: 3px; }
        .consent-button--primary { background: #ffffff; color: #0d2b4c; }
        .consent-button--secondary { background: rgba(255, 255, 255, 0.12); color: #ffffff; border-color: rgba(255, 255, 255, 0.42); }
        .consent-button--link { background: transparent; color: #ffffff; text-decoration: underline; text-underline-offset: 4px; }
        .consent-choices { display: grid; gap: 10px; padding: 14px; border: 1px solid rgba(255, 255, 255, 0.22); border-radius: 18px; background: rgba(255, 255, 255, 0.08); }
        .consent-choices__title { margin: 0; font-weight: 850; }
        .consent-choice { display: flex; gap: 10px; align-items: flex-start; }
        .consent-choice input { margin-top: 4px; }
        .consent-choice small { display: block; color: #dce9f7; }
        @media (max-width: 760px) { .site-header__inner, .site-footer { align-items: flex-start; flex-direction: column; } .site-footer__links { align-items: flex-start; } .site-footer__link-row { justify-content: flex-start; } .landing-page .landing-shell { grid-template-columns: 1fr; min-height: auto; } .landing-page .app-panel { min-height: 68svh; } .landing-page .app-panel__content { max-height: none; overflow: visible; } }
        @media (max-width: 640px) { .page-shell { width: min(100% - 20px, 920px); padding: 20px 0; } }

""";


    private static string RenderMarkdown(string markdown)
    {
        var html = new StringBuilder();
        var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        string? listTag = null;
        var paragraph = new List<string>();
        void FlushParagraph()
        {
            if (paragraph.Count == 0) { return; }
            html.Append("<p>").Append(InlineMarkdown(string.Join(" ", paragraph))).AppendLine("</p>");
            paragraph.Clear();
        }
        void CloseList()
        {
            if (listTag is null) { return; }
            html.Append("</").Append(listTag).AppendLine(">");
            listTag = null;
        }
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0) { FlushParagraph(); CloseList(); continue; }
            if (line == "---" || line == "***") { FlushParagraph(); CloseList(); html.AppendLine("<hr>"); continue; }
            if (line.StartsWith("### ", StringComparison.Ordinal)) { FlushParagraph(); CloseList(); html.Append("<h3>").Append(InlineMarkdown(line[4..].Trim())).AppendLine("</h3>"); continue; }
            if (line.StartsWith("## ", StringComparison.Ordinal)) { FlushParagraph(); CloseList(); html.Append("<h2>").Append(InlineMarkdown(line[3..].Trim())).AppendLine("</h2>"); continue; }
            if (line.StartsWith("# ", StringComparison.Ordinal)) { FlushParagraph(); CloseList(); html.Append("<h1>").Append(InlineMarkdown(line[2..].Trim())).AppendLine("</h1>"); continue; }
            if (line.StartsWith("> ", StringComparison.Ordinal)) { FlushParagraph(); CloseList(); html.Append("<blockquote>").Append(InlineMarkdown(line[2..].Trim())).AppendLine("</blockquote>"); continue; }
            var bullet = line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal);
            var numbered = NumberedListRegex().IsMatch(line);
            if (bullet || numbered)
            {
                FlushParagraph();
                var desired = bullet ? "ul" : "ol";
                if (listTag != desired) { CloseList(); html.Append('<').Append(desired).AppendLine(">"); listTag = desired; }
                var item = bullet ? line[2..].Trim() : NumberedListRegex().Replace(line, string.Empty, 1).Trim();
                html.Append("<li>").Append(InlineMarkdown(item)).AppendLine("</li>");
                continue;
            }
            CloseList();
            paragraph.Add(line);
        }
        FlushParagraph();
        CloseList();
        return html.ToString();
    }

    private static string InlineMarkdown(string text)
    {
        var html = new StringBuilder();
        var index = 0;
        foreach (Match match in LinkRegex().Matches(text))
        {
            html.Append(InlineAutoLinks(text[index..match.Index]));
            var label = InlineFormatting(E(match.Groups[1].Value));
            var href = SafeHref(match.Groups[2].Value);
            html.Append(href is null ? label : Anchor(href, label));
            index = match.Index + match.Length;
        }
        html.Append(InlineAutoLinks(text[index..]));
        return html.ToString();
    }

    private static string InlineAutoLinks(string text)
    {
        var html = new StringBuilder();
        var index = 0;
        foreach (Match match in AutoLinkRegex().Matches(text))
        {
            html.Append(InlineFormatting(E(text[index..match.Index])));
            var token = match.Value;
            var trailing = TrimTrailingLinkPunctuation(token, out var linkText);
            var href = AutoLinkHref(linkText);
            html.Append(Anchor(SafeHref(href)!, E(linkText))).Append(E(trailing));
            index = match.Index + match.Length;
        }
        html.Append(InlineFormatting(E(text[index..])));
        return html.ToString();
    }

    private static string InlineFormatting(string encoded)
    {
        encoded = BoldRegex().Replace(encoded, "<strong>$1</strong>");
        encoded = ItalicRegex().Replace(encoded, "<em>$1</em>");
        return encoded;
    }

    private static string TrimTrailingLinkPunctuation(string token, out string linkText)
    {
        var end = token.Length;
        while (end > 0 && ".,;:)!?".IndexOf(token[end - 1]) >= 0)
        {
            end--;
        }
        linkText = token[..end];
        return token[end..];
    }

    private static string AutoLinkHref(string linkText)
    {
        if (linkText.Contains('@', StringComparison.Ordinal) && !linkText.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return $"mailto:{linkText}";
        }

        return linkText.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || linkText.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? linkText
            : $"https://{linkText}";
    }

    private static string? SafeHref(string href)
    {
        if (Uri.TryCreate(href.Trim(), UriKind.Absolute, out var absolute) && (absolute.Scheme == Uri.UriSchemeHttps || absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeMailto))
        {
            return E(absolute.ToString());
        }
        return null;
    }

    private static string Anchor(string href, string label)
    {
        var rel = href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? " rel=\"noopener noreferrer\""
            : string.Empty;
        return $"<a href=\"{href}\"{rel}>{label}</a>";
    }

    private static string Nav() => "<section class=\"support-card legal-nav\"><a href=\"index.html\">Home</a><a href=\"download.html\">Download</a><a href=\"mobile.html\">Mobile</a><a href=\"pricing.html\">Pricing</a><a href=\"terms.html\">Terms</a><a href=\"privacy.html\">Privacy</a><a href=\"refunds.html\">Refunds</a><a href=\"cancellation.html\">Cancellation</a><a href=\"support.html\">Support</a></section>";

    private static string NavLinks(Dictionary<string, string> h) => $"<nav class=\"site-footer__links\" aria-label=\"Legal and company links\"><div class=\"site-footer__link-row site-footer__link-row--primary\"><a href=\"privacy.html\">{E(h["footerPrivacyLabel"])}</a><a href=\"terms.html\">{E(h["footerTermsLabel"])}</a><a href=\"refunds.html\">{E(h["footerRefundsLabel"])}</a><a href=\"cancellation.html\">{E(h["footerCancellationLabel"])}</a><a href=\"support.html\">{E(h["footerSupportLabel"])}</a><a href=\"pricing.html\">{E(h["footerPricingLabel"])}</a></div><div class=\"site-footer__link-row site-footer__link-row--secondary\"><a href=\"seller.html\">Seller / Company Details</a><a href=\"ai-data.html\">AI &amp; Data Disclosure</a><a href=\"status.html\">Service Status</a></div></nav>";

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

    private static StaticReleaseManifest? ReadStaticReleaseManifest(string publicRoot)
    {
        var path = Path.Combine(publicRoot, "releases", "windows", "direct", "latest.json");
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var version = ReadRequiredString(root, "version");
            var installerRelativeUrl = ReadRequiredString(root, "installerRelativeUrl");
            var installerFileName = ReadOptionalString(root, "installerFileName") ?? installerRelativeUrl;
            if (version is null
                || installerRelativeUrl is null
                || !SafeInstallerFileRegex().IsMatch(installerRelativeUrl)
                || installerFileName != installerRelativeUrl)
            {
                return null;
            }

            return new StaticReleaseManifest(
                version,
                installerRelativeUrl,
                installerFileName,
                FormatInstallerSize(root));
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? ReadRequiredString(JsonElement root, string propertyName) => ReadOptionalString(root, propertyName);

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string FormatInstallerSize(JsonElement root)
    {
        if (!root.TryGetProperty("installerSizeBytes", out var property) || !property.TryGetInt64(out var bytes) || bytes <= 0)
        {
            return "Release details load from the public manifest.";
        }

        string[] units = ["bytes", "KB", "MB", "GB"];
        var value = (double)bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0 ? $"{bytes} bytes" : $"{value:0.0} {units[unitIndex]}";
    }

    private sealed record StaticReleaseManifest(
        string Version,
        string InstallerRelativeUrl,
        string InstallerFileName,
        string InstallerSize);

    private static WebsiteContentSet DefaultSet() => new(new()
    {
        ["home"] = new(){{"logoPath",""},{"logoAltText","Language Voice Tutor logo"},{"fallbackLogoText","Language Voice Tutor"},{"topHeaderText","Practice real conversations in:"},{"supportedLanguageLine",RequiredLanguageLine},{"windowsCardBadge","Available for testers"},{"windowsCardTitle","Application for Windows"},{"windowsCardDescription","Practice real-life language lessons by text or voice on your desktop."},{"windowsDownloadButtonText","Download desktop version"},{"mobileCardBadge","In development"},{"mobileCardTitle","Application for mobile devices"},{"mobileCardDescription","Android and iOS apps are planned but are not currently available."},{"mobileComingSoonButtonText","Not currently available"},{"footerCopyrightText","© Language Voice Tutor. All rights reserved."},{"footerPrivacyLabel","Privacy Policy"},{"footerTermsLabel","Terms of Use"},{"footerRefundsLabel","Refund Policy"},{"footerCancellationLabel","Cancellation"},{"footerSupportLabel","Support"},{"footerPricingLabel","Pricing"},{"seoTitle","Language Voice Tutor"},{"seoDescription","Language Voice Tutor helps you practice real-life language lessons by text or voice on desktop, with mobile apps planned."}},
        ["download"] = Page(ReleaseReadyDownloadPageTitle, ReleaseReadyDownloadSeoDescription, DownloadDefaults()),
        ["mobile"] = Page("Mobile app coming soon","Android and iOS versions are planned and not currently available.", new(){{"androidComingSoonText","Android app coming soon."},{"iosComingSoonText","iOS app coming soon."},{"emailSupportCtaText","Email support@languagevoicetutor.com for availability questions."}}),
        ["pricing"] = Page("Pricing","Language Voice Tutor is currently offered for Windows desktop tester access.", new(){{"freePlanText","Invited testers may be able to use free Windows desktop access during evaluation."},{"premiumPlanText","Premium subscription details are draft placeholders until paid billing is enabled by the owner."},{"trialText","Trial terms are not final and require owner/legal review."},{"paddleLiveCheckoutDisclaimerText","No live checkout button is provided and production Paddle billing is not enabled from this page."}}),
        ["support"] = Page("Contact support","For Language Voice Tutor help, contact support@languagevoicetutor.com.", new(){{"supportEmailText","support@languagevoicetutor.com"},{"responseTimeText","Response times may vary during tester access."},{"accountDeletionSupportText","Contact support for account or deletion requests."},{"billingSupportText","Billing support applies only if paid billing is enabled."}}),
        ["terms"] = Legal("Terms of Use", "These draft terms describe use of Language Voice Tutor and require owner/legal review before final publication.", new(){{"accountUseTerms","Use the service lawfully and keep account credentials secure."},{"aiLearningDisclaimer","The AI tutor may be inaccurate and is for educational practice, not professional advice."},{"billingSubscriptionTermsPlaceholder","Premium subscription terms are placeholders until owner/legal approval."},{"contactSupportText","Contact support@languagevoicetutor.com for help."}}),
        ["privacy"] = Legal("Privacy Policy", "This draft explains high-level data handling and requires owner/legal review.", new(){{"dataCollected","We may process account, settings, support, product usage, lesson, prompt, answer, and conversation data."},{"audioTranscriptionText","Voice features may capture audio for transcription and tutor responses."},{"aiProcessingText","Lesson content and context may be sent to AI providers to generate tutor feedback."},{"accountPaymentDataText","If paid billing is enabled, payment data may be processed by Paddle."},{"dataRetentionDeletionText","Retention and deletion periods require owner/legal approval."},{"contactText","Contact support@languagevoicetutor.com for privacy help."},{"bodyMarkdown", """
## Optional analytics, advertising, and cookie choices

Language Voice Tutor may use optional analytics cookies and optional advertising cookies on the public website if those features are enabled in the Website CMS and valid Google IDs are configured. If enabled, Google Analytics and Google Ads may help us understand site traffic, improve the product, measure marketing performance, and measure download button clicks.

Where consent is required, non-essential analytics and advertising storage is denied by default until you choose otherwise. The cookie banner may offer **Accept all**, **Reject non-essential**, and **Manage choices** controls for analytics cookies and advertising cookies. The site remains usable if you reject non-essential cookies.

You can change or withdraw your choices by using the choices interface when available, or by clearing this site's browser storage and cookies. This privacy policy is a product review draft and is not legal advice.
"""}}),
        ["refunds"] = Legal("Refund Policy", "This draft refund page is provided for review readiness.", new(){{"refundEligibilityText","Refund eligibility is a placeholder pending owner/legal approval."},{"howToRequestRefundText","Contact support@languagevoicetutor.com with your account email and a short explanation."},{"paddlePaymentProviderNote","When paid billing is enabled, refund handling may require Paddle coordination."},{"contactText","Contact support@languagevoicetutor.com."}}),
        ["cancellation"] = Legal("Cancellation Policy", "This draft explains cancellation support paths for a future or enabled Premium subscription.", new(){{"howToCancelText","If paid billing is enabled, cancel through the billing flow or contact support."},{"accessUntilPeriodEndText","Access may continue until the end of the current billing period unless final policy says otherwise."},{"supportText","Contact support@languagevoicetutor.com for cancellation help."}}),
        ["seller"] = Page("Seller / Company details","Seller and company details placeholders must be completed before Paddle live review.", new(){{"sellerNameLegalEntityPlaceholder","<LEGAL_SELLER_NAME>"},{"addressPlaceholder","<SELLER_ADDRESS>"},{"contactEmail","support@languagevoicetutor.com"},{"taxVatCompanyRegistrationPlaceholder","<TAX_VAT_COMPANY_REGISTRATION>"},{"paddleLiveReviewNote","Complete these placeholders before Paddle live review."}}),
        ["aiData"] = Page("AI / Data disclosure","Language Voice Tutor uses AI tutor features for language practice.", new(){{"aiTutorDisclosureText","The AI tutor generates practice responses and may be inaccurate."},{"voiceTranscriptionDisclosureText","Voice input may be transcribed for tutor interactions."},{"dataProcessingText","Practice data may be processed to provide lessons and feedback."},{"userControlDeletionText","Contact support for account and deletion requests."}}),
        ["status"] = Page("Platform availability / service status","Current platform availability and service status information.", new(){{"desktopAvailabilityText","Windows desktop is the current supported tester platform."},{"mobileComingSoonText","Android and iOS are coming soon."},{"serviceAvailabilityDisclaimer","Service availability may vary during testing and maintenance."},{"supportContactText","Contact support@languagevoicetutor.com for help."}})
    }, new WebsiteDesignContent("#0d2b4c", "#0d2b4c", "#24201b", "#dce9f7", "system-ui, -apple-system, BlinkMacSystemFont, \"Segoe UI\", sans-serif", 16, 700, 999, "Normal"), DefaultMarketing());
    private static Dictionary<string,string> Page(string title,string intro,Dictionary<string,string> extra){ extra["pageTitle"]=title; extra["introText"]=intro; if (!extra.ContainsKey("seoTitle")) extra["seoTitle"]=$"{title} | Language Voice Tutor"; extra["seoDescription"]=intro; return extra; }
    private static Dictionary<string,string> Legal(string title,string intro,Dictionary<string,string> extra){ var p=Page(title,intro,extra); p["effectiveDate"]="Effective date placeholder"; p["intro"]=intro; return p; }
    private static Dictionary<string, string> DownloadDefaults()
    {
        var values = new Dictionary<string, string>
        {
            { "downloadButtonText", "Download for Windows" },
            { "currentVersionLabel", "Current version and installer size are loaded from the release manifest." },
            { "safetySupportNote", "Windows may show a SmartScreen warning because code signing is deferred." },
            { "seoTitle", ReleaseReadyDownloadSeoTitle },
            { "bodyMarkdown", ReleaseReadyDownloadBodyMarkdown }
        };
        for (var i = 0; i < DefaultDownloadFeatureCards.Length; i++)
        {
            var card = DefaultDownloadFeatureCards[i];
            var prefix = $"featureCard{i + 1}";
            values[$"{prefix}Label"] = card.Label;
            values[$"{prefix}Title"] = card.Title;
            values[$"{prefix}Description"] = card.Description;
            values[$"{prefix}ImagePath"] = card.ImagePath;
        }
        return values;
    }

    private static Dictionary<string, string> DefaultMarketing() => new(StringComparer.OrdinalIgnoreCase) { ["enableAnalytics"] = "false", ["googleAnalyticsMeasurementId"] = "", ["enableAdsTracking"] = "false", ["googleAdsId"] = "", ["googleAdsDownloadConversionLabel"] = "", ["googleSearchConsoleVerificationToken"] = "", ["enableConsentBanner"] = "true", ["enableLlmsTxt"] = "true", ["defaultSocialImageUrl"] = "" };
    private static Dictionary<string, string> MergeMarketing(Dictionary<string, string>? existing, Dictionary<string, string>? incoming) { var merged = NormalizeMarketing(existing, DefaultMarketing()); if (incoming is not null) foreach (var kv in incoming) merged[kv.Key] = kv.Value; return NormalizeMarketing(merged, DefaultMarketing()); }
    private static Dictionary<string, string> NormalizeMarketing(Dictionary<string, string>? value, Dictionary<string, string>? fallback) { var result = new Dictionary<string, string>(fallback ?? DefaultMarketing(), StringComparer.OrdinalIgnoreCase); if (value is null) return result; foreach (var (key, raw) in value) result[key] = key switch { "googleAnalyticsMeasurementId" => SafeGaId(raw), "googleAdsId" => SafeAdsId(raw), "googleAdsDownloadConversionLabel" => SafeConversionLabel(raw), "googleSearchConsoleVerificationToken" => SafeSearchConsoleToken(raw), "enableAnalytics" or "enableAdsTracking" or "enableConsentBanner" or "enableLlmsTxt" => IsTruthy(raw) ? "true" : "false", "defaultSocialImageUrl" => NormalizeLogoPath(raw), _ => LimitText(raw, 120, string.Empty) }; return result; }
    private static bool IsEnabled(Dictionary<string, string>? m, string key) => m is not null && m.TryGetValue(key, out var value) && IsTruthy(value);
    private static bool IsTruthy(string? value) => string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    private static string SafeGaId(string? value) => value is not null && GaIdRegex().IsMatch(value.Trim()) ? value.Trim() : string.Empty;
    private static string SafeAdsId(string? value) => value is not null && AdsIdRegex().IsMatch(value.Trim()) ? value.Trim() : string.Empty;
    private static string SafeConversionLabel(string? value) => value is not null && ConversionLabelRegex().IsMatch(value.Trim()) ? value.Trim() : string.Empty;
    private static string SafeSearchConsoleToken(string? value) => value is not null && SearchConsoleTokenRegex().IsMatch(value.Trim()) ? value.Trim() : string.Empty;
    private static int TextLimitFor(string key) => key == "bodyMarkdown" ? 12000 : key.Contains("seo", StringComparison.OrdinalIgnoreCase) ? 180 : 900;
    private static string LimitText(string? value, int max, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string NormalizeHex(string? value, string fallback) => value is not null && HexColorRegex().IsMatch(value.Trim()) ? value.Trim() : fallback;
    private static string NormalizeFontFamily(string? value, string fallback) => value is not null && SafeFontRegex().IsMatch(value.Trim()) ? LimitText(value, 120, fallback) : fallback;
    private static WebsiteDesignContent NormalizeDesign(WebsiteDesignContent? value, WebsiteDesignContent fallback) => value is null ? fallback : new WebsiteDesignContent(NormalizeHex(value.HeaderBackgroundColor, fallback.HeaderBackgroundColor), NormalizeHex(value.FooterBackgroundColor, fallback.FooterBackgroundColor), NormalizeHex(value.MainTextColor, fallback.MainTextColor), NormalizeHex(value.HeaderTextColor, fallback.HeaderTextColor), NormalizeFontFamily(value.MainFontFamily, fallback.MainFontFamily), Math.Clamp(value.BaseFontSizePx, 14, 22), AllowedFontWeights().Contains(value.HeaderFontWeight) ? value.HeaderFontWeight : fallback.HeaderFontWeight, Math.Clamp(value.ButtonBorderRadiusPx, 0, 32), LimitText(value.CardTextStyle, 80, fallback.CardTextStyle));
    private static string NormalizeLogoPath(string? value, string fallback = "") { var t = value?.Trim() ?? ""; if (t.Length == 0) return fallback; if (Uri.TryCreate(t, UriKind.Absolute, out var uri)) return uri.Scheme == Uri.UriSchemeHttps ? t : fallback; return SafeRelativePathRegex().IsMatch(t) && !t.Contains("..", StringComparison.Ordinal) ? t : fallback; }
    private static HashSet<int> AllowedFontWeights() => [400, 500, 600, 700, 800];
    [GeneratedRegex("^#[0-9a-fA-F]{6}$")] private static partial Regex HexColorRegex();
    [GeneratedRegex("^G-[A-Z0-9]{6,16}$")] private static partial Regex GaIdRegex();
    [GeneratedRegex("^AW-[0-9]{6,16}$")] private static partial Regex AdsIdRegex();
    [GeneratedRegex("^[A-Za-z0-9_-]{4,80}$")] private static partial Regex ConversionLabelRegex();
    [GeneratedRegex("^[A-Za-z0-9_-]{8,120}$")] private static partial Regex SearchConsoleTokenRegex();
    [GeneratedRegex("^[a-zA-Z0-9 ,\"-]+$")] private static partial Regex SafeFontRegex();
    [GeneratedRegex("^[a-zA-Z0-9_./%#?=&:+-]+$")] private static partial Regex SafeRelativePathRegex();
    [GeneratedRegex("^LanguageVoiceTutorSetup-[A-Za-z0-9._-]+\\.exe$")] private static partial Regex SafeInstallerFileRegex();
    [GeneratedRegex("^\\d+\\.\\s+")] private static partial Regex NumberedListRegex();
    [GeneratedRegex("^[^\\p{L}]*")] private static partial Regex LanguageLabelRegex();
    [GeneratedRegex("\\[([^\\]]+)\\]\\(([^)]+)\\)")] private static partial Regex LinkRegex();
    [GeneratedRegex(@"(?<![\w@:/])(?:https?://[^\s<>()]+|[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}|(?:[A-Za-z0-9-]+\.)+[A-Za-z]{2,})(?![\w@-])", RegexOptions.IgnoreCase)] private static partial Regex AutoLinkRegex();
    [GeneratedRegex("\\*\\*([^*]+)\\*\\*")] private static partial Regex BoldRegex();
    [GeneratedRegex("(?<!_)_([^_]+)_(?!_)")] private static partial Regex ItalicRegex();
}
