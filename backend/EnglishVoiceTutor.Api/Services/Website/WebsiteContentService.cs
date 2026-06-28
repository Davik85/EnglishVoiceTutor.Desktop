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
    private const string RequiredLanguageLine = "🇬🇧 English · 🇫🇷 French · 🇩🇪 German · 🇪🇸 Spanish · 🇮🇹 Italian · 🇵🇹 Portuguese";
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
        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<WebsiteContentDocument>(stream, JsonOptions, cancellationToken);
        if (document is null) { var d = DefaultSet(); return new WebsiteContentDocument(d, d); }
        return new WebsiteContentDocument(Normalize(document.Active), Normalize(document.Draft));
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
        return Normalize(new WebsiteContentSet(pages, design));
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
        var design = NormalizeDesign(input?.Design, defaults.Design);
        return new WebsiteContentSet(pages, design);
    }

    private async Task<IReadOnlyList<string>> RenderAllAsync(WebsiteContentSet c, string root, CancellationToken ct)
    {
        var files = new List<string>();
        async Task W(string file, string html) { var path = Path.Combine(root, file); await File.WriteAllTextAsync(path, html, ct); files.Add(path); }
        var release = ReadStaticReleaseManifest(root);
        foreach (var (pageKey, fileName) in PageFiles())
        {
            await W(fileName, RenderPage(c, pageKey, release: pageKey == "download" ? release : null));
        }
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
        return Shell(c, E(h["seoTitle"]), E(h["seoDescription"]), main, true, includePublicBaseHref);
    }


    private static string RenderDownload(WebsiteContentSet c, bool includePublicBaseHref, StaticReleaseManifest? release)
    {
        var p = c.Pages["download"];
        var currentVersion = "Current Windows tester release is available through the Download for Windows button.";
        var manifestStatus = "If release details do not load automatically, please contact support@languagevoicetutor.com.";
        var downloadAttributes = string.Empty;
        var downloadClass = "download-button is-disabled";
        var ariaDisabled = "true";
        if (release is not null)
        {
            currentVersion = release.Version;
            manifestStatus = "Release details are shown from the published local manifest and will refresh automatically when JavaScript runs.";
            downloadAttributes = $" href=\"{E(release.InstallerRelativeUrl)}\" download=\"{E(release.InstallerFileName)}\"";
            downloadClass = "download-button";
            ariaDisabled = "false";
        }
        var body = new StringBuilder();
        body.Append($"""
<main class="page-shell legal-page">
    <section class="hero-card" aria-labelledby="download-title">
        <h1 id="download-title">{E(p["pageTitle"])}</h1>
        <p class="description">{E(p.GetValueOrDefault("introText", p.GetValueOrDefault("intro", string.Empty)))}</p>
""");
        if (!string.IsNullOrWhiteSpace(p.GetValueOrDefault("bodyMarkdown")))
        {
            body.AppendLine("        <div class=\"markdown-content\">");
            body.AppendLine(RenderMarkdown(p["bodyMarkdown"]));
            body.AppendLine("        </div>");
        }
        body.Append($"""
        <div class="download-panel" aria-label="Windows download" data-manifest-url="/releases/windows/direct/latest.json">
            <p class="version-line">Current version: <strong id="current-version">{E(currentVersion)}</strong></p>
            <a id="download-button" class="{downloadClass}" aria-disabled="{ariaDisabled}"{downloadAttributes}>{E(p.GetValueOrDefault("downloadButtonText", "Download for Windows"))}</a>
            <p id="manifest-status" class="status-note" role="status">{E(manifestStatus)}</p>
            <p class="status-note">If release details do not load automatically, please contact <a href="mailto:support@languagevoicetutor.com">support@languagevoicetutor.com</a>.</p>
            <p class="warning-note">{E(p.GetValueOrDefault("safetySupportNote", string.Empty))}</p>
        </div>
    </section>

    <section class="details-card legal-section" aria-labelledby="release-details-title">
        <h2 id="release-details-title">Current release details</h2>
        <dl class="release-details">
            <div><dt>Version</dt><dd id="detail-version">{E(release?.Version ?? "Release details load from the public manifest.")}</dd></div>
            <div><dt>Installer filename</dt><dd id="detail-installer">{E(release?.InstallerFileName ?? "Use the Download for Windows button or contact support.")}</dd></div>
            <div><dt>Backend base URL</dt><dd id="detail-backend-base-url">{E(release?.BackendBaseUrl ?? "Release details load from the public manifest.")}</dd></div>
            <div><dt>Minimum supported version</dt><dd id="detail-minimum-supported-version">{E(release?.MinimumSupportedVersion ?? "Release details load from the public manifest.")}</dd></div>
            <div><dt>Update mode</dt><dd id="detail-update-mode">{E(release?.UpdateMode ?? "Release details load from the public manifest.")}</dd></div>
            <div><dt>Channel</dt><dd id="detail-channel">{E(release?.Channel ?? "Release details load from the public manifest.")}</dd></div>
            <div><dt>Installer size</dt><dd id="detail-size">{E(release?.InstallerSize ?? "Release details load from the public manifest.")}</dd></div>
            <div><dt>SHA-256</dt><dd id="detail-sha">{E(release?.InstallerSha256 ?? "Release details load from the public manifest.")}</dd></div>
        </dl>
    </section>
""");
        body.Append(Nav());
        body.AppendLine("</main>");
        return Shell(c, E(p["seoTitle"]), E(p["seoDescription"]), body.ToString(), false, includePublicBaseHref, "    <script src=\"download.js?v=manifest-download\" defer></script>");
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
            return Shell(c, E(p["seoTitle"]), E(p["seoDescription"]), body.ToString(), false, includePublicBaseHref);
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
        return Shell(c, E(p["seoTitle"]), E(p["seoDescription"]), body.ToString(), false, includePublicBaseHref);
    }

    private static string Shell(WebsiteContentSet c, string title, string description, string main, bool landing, bool includePublicBaseHref, string? extraBodyHtml = null)
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
        html.AppendLine($"    <title>{title}</title>");
        html.AppendLine($"    <meta name=\"description\" content=\"{description}\">");
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
        if (!string.IsNullOrWhiteSpace(extraBodyHtml)) { html.AppendLine(extraBodyHtml); }
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

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
                ReadOptionalString(root, "backendBaseUrl") ?? "Release details load from the public manifest.",
                ReadOptionalString(root, "minimumSupportedVersion") ?? "Release details load from the public manifest.",
                ReadOptionalString(root, "updateMode") ?? "Release details load from the public manifest.",
                ReadOptionalString(root, "channel") ?? "Release details load from the public manifest.",
                FormatInstallerSize(root),
                ReadOptionalString(root, "installerSha256") ?? "Release details load from the public manifest.");
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
        string BackendBaseUrl,
        string MinimumSupportedVersion,
        string UpdateMode,
        string Channel,
        string InstallerSize,
        string InstallerSha256);

    private static WebsiteContentSet DefaultSet() => new(new()
    {
        ["home"] = new(){{"logoPath",""},{"logoAltText","Language Voice Tutor logo"},{"fallbackLogoText","Language Voice Tutor"},{"topHeaderText","Practice real conversations in:"},{"supportedLanguageLine",RequiredLanguageLine},{"windowsCardBadge","Available for testers"},{"windowsCardTitle","Application for Windows"},{"windowsCardDescription","Practice real-life language lessons by text or voice on your desktop."},{"windowsDownloadButtonText","Download desktop version"},{"mobileCardBadge","In development"},{"mobileCardTitle","Application for mobile devices"},{"mobileCardDescription","Android and iOS apps are planned but are not currently available."},{"mobileComingSoonButtonText","Not currently available"},{"footerCopyrightText","© Language Voice Tutor. All rights reserved."},{"footerPrivacyLabel","Privacy Policy"},{"footerTermsLabel","Terms of Use"},{"footerRefundsLabel","Refund Policy"},{"footerCancellationLabel","Cancellation"},{"footerSupportLabel","Support"},{"footerPricingLabel","Pricing"},{"seoTitle","Language Voice Tutor"},{"seoDescription","Language Voice Tutor helps you practice real-life language lessons by text or voice on desktop, with mobile apps planned."}},
        ["download"] = Page("Language Voice Tutor tester download","A Windows desktop app for practicing spoken languages with an AI tutor.", new(){{"downloadButtonText","Download for Windows"},{"currentVersionLabel","Current version details are loaded from the release manifest."},{"safetySupportNote","Windows may show a SmartScreen warning because code signing is deferred."}}),
        ["mobile"] = Page("Mobile app coming soon","Android and iOS versions are planned and not currently available.", new(){{"androidComingSoonText","Android app coming soon."},{"iosComingSoonText","iOS app coming soon."},{"emailSupportCtaText","Email support@languagevoicetutor.com for availability questions."}}),
        ["pricing"] = Page("Pricing","Language Voice Tutor is currently offered for Windows desktop tester access.", new(){{"freePlanText","Invited testers may be able to use free Windows desktop access during evaluation."},{"premiumPlanText","Premium subscription details are draft placeholders until paid billing is enabled by the owner."},{"trialText","Trial terms are not final and require owner/legal review."},{"paddleLiveCheckoutDisclaimerText","No live checkout button is provided and production Paddle billing is not enabled from this page."}}),
        ["support"] = Page("Contact support","For Language Voice Tutor help, contact support@languagevoicetutor.com.", new(){{"supportEmailText","support@languagevoicetutor.com"},{"responseTimeText","Response times may vary during tester access."},{"accountDeletionSupportText","Contact support for account or deletion requests."},{"billingSupportText","Billing support applies only if paid billing is enabled."}}),
        ["terms"] = Legal("Terms of Use", "These draft terms describe use of Language Voice Tutor and require owner/legal review before final publication.", new(){{"accountUseTerms","Use the service lawfully and keep account credentials secure."},{"aiLearningDisclaimer","The AI tutor may be inaccurate and is for educational practice, not professional advice."},{"billingSubscriptionTermsPlaceholder","Premium subscription terms are placeholders until owner/legal approval."},{"contactSupportText","Contact support@languagevoicetutor.com for help."}}),
        ["privacy"] = Legal("Privacy Policy", "This draft explains high-level data handling and requires owner/legal review.", new(){{"dataCollected","We may process account, settings, support, product usage, lesson, prompt, answer, and conversation data."},{"audioTranscriptionText","Voice features may capture audio for transcription and tutor responses."},{"aiProcessingText","Lesson content and context may be sent to AI providers to generate tutor feedback."},{"accountPaymentDataText","If paid billing is enabled, payment data may be processed by Paddle."},{"dataRetentionDeletionText","Retention and deletion periods require owner/legal approval."},{"contactText","Contact support@languagevoicetutor.com for privacy help."}}),
        ["refunds"] = Legal("Refund Policy", "This draft refund page is provided for review readiness.", new(){{"refundEligibilityText","Refund eligibility is a placeholder pending owner/legal approval."},{"howToRequestRefundText","Contact support@languagevoicetutor.com with your account email and a short explanation."},{"paddlePaymentProviderNote","When paid billing is enabled, refund handling may require Paddle coordination."},{"contactText","Contact support@languagevoicetutor.com."}}),
        ["cancellation"] = Legal("Cancellation Policy", "This draft explains cancellation support paths for a future or enabled Premium subscription.", new(){{"howToCancelText","If paid billing is enabled, cancel through the billing flow or contact support."},{"accessUntilPeriodEndText","Access may continue until the end of the current billing period unless final policy says otherwise."},{"supportText","Contact support@languagevoicetutor.com for cancellation help."}}),
        ["seller"] = Page("Seller / Company details","Seller and company details placeholders must be completed before Paddle live review.", new(){{"sellerNameLegalEntityPlaceholder","<LEGAL_SELLER_NAME>"},{"addressPlaceholder","<SELLER_ADDRESS>"},{"contactEmail","support@languagevoicetutor.com"},{"taxVatCompanyRegistrationPlaceholder","<TAX_VAT_COMPANY_REGISTRATION>"},{"paddleLiveReviewNote","Complete these placeholders before Paddle live review."}}),
        ["aiData"] = Page("AI / Data disclosure","Language Voice Tutor uses AI tutor features for language practice.", new(){{"aiTutorDisclosureText","The AI tutor generates practice responses and may be inaccurate."},{"voiceTranscriptionDisclosureText","Voice input may be transcribed for tutor interactions."},{"dataProcessingText","Practice data may be processed to provide lessons and feedback."},{"userControlDeletionText","Contact support for account and deletion requests."}}),
        ["status"] = Page("Platform availability / service status","Current platform availability and service status information.", new(){{"desktopAvailabilityText","Windows desktop is the current supported tester platform."},{"mobileComingSoonText","Android and iOS are coming soon."},{"serviceAvailabilityDisclaimer","Service availability may vary during testing and maintenance."},{"supportContactText","Contact support@languagevoicetutor.com for help."}})
    }, new WebsiteDesignContent("#0d2b4c", "#0d2b4c", "#24201b", "#dce9f7", "system-ui, -apple-system, BlinkMacSystemFont, \"Segoe UI\", sans-serif", 16, 700, 999, "Normal"));
    private static Dictionary<string,string> Page(string title,string intro,Dictionary<string,string> extra){ extra["pageTitle"]=title; extra["introText"]=intro; extra["seoTitle"]=$"{title} | Language Voice Tutor"; extra["seoDescription"]=intro; return extra; }
    private static Dictionary<string,string> Legal(string title,string intro,Dictionary<string,string> extra){ var p=Page(title,intro,extra); p["effectiveDate"]="Effective date placeholder"; p["intro"]=intro; return p; }
    private static int TextLimitFor(string key) => key == "bodyMarkdown" ? 12000 : key.Contains("seo", StringComparison.OrdinalIgnoreCase) ? 180 : 900;
    private static string LimitText(string? value, int max, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string NormalizeHex(string? value, string fallback) => value is not null && HexColorRegex().IsMatch(value.Trim()) ? value.Trim() : fallback;
    private static string NormalizeFontFamily(string? value, string fallback) => value is not null && SafeFontRegex().IsMatch(value.Trim()) ? LimitText(value, 120, fallback) : fallback;
    private static WebsiteDesignContent NormalizeDesign(WebsiteDesignContent? value, WebsiteDesignContent fallback) => value is null ? fallback : new WebsiteDesignContent(NormalizeHex(value.HeaderBackgroundColor, fallback.HeaderBackgroundColor), NormalizeHex(value.FooterBackgroundColor, fallback.FooterBackgroundColor), NormalizeHex(value.MainTextColor, fallback.MainTextColor), NormalizeHex(value.HeaderTextColor, fallback.HeaderTextColor), NormalizeFontFamily(value.MainFontFamily, fallback.MainFontFamily), Math.Clamp(value.BaseFontSizePx, 14, 22), AllowedFontWeights().Contains(value.HeaderFontWeight) ? value.HeaderFontWeight : fallback.HeaderFontWeight, Math.Clamp(value.ButtonBorderRadiusPx, 0, 32), LimitText(value.CardTextStyle, 80, fallback.CardTextStyle));
    private static string NormalizeLogoPath(string? value, string fallback = "") { var t = value?.Trim() ?? ""; if (t.Length == 0) return fallback; if (Uri.TryCreate(t, UriKind.Absolute, out var uri)) return uri.Scheme == Uri.UriSchemeHttps ? t : fallback; return SafeRelativePathRegex().IsMatch(t) && !t.Contains("..", StringComparison.Ordinal) ? t : fallback; }
    private static HashSet<int> AllowedFontWeights() => [400, 500, 600, 700, 800];
    [GeneratedRegex("^#[0-9a-fA-F]{6}$")] private static partial Regex HexColorRegex();
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
