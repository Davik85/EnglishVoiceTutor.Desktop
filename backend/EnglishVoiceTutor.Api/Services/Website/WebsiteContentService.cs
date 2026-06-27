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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private const string RequiredLanguageLine = "🇬🇧 English · 🇫🇷 French · 🇩🇪 German · 🇪🇸 Spanish · 🇮🇹 Italian · 🇵🇹 Portuguese";

    public async Task<WebsiteContentResponse> GetAsync(CancellationToken cancellationToken)
    {
        var document = await ReadDocumentAsync(cancellationToken);
        return new WebsiteContentResponse(document.Active, document.Draft);
    }

    public async Task<WebsiteContentResponse> SaveDraftAsync(WebsiteContentSet draft, CancellationToken cancellationToken)
    {
        var document = await ReadDocumentAsync(cancellationToken);
        document = document with { Draft = Normalize(draft) };
        await WriteDocumentAsync(document, cancellationToken);
        return new WebsiteContentResponse(document.Active, document.Draft);
    }

    public async Task<WebsitePublishResponse> PublishAsync(WebsiteContentSet content, CancellationToken cancellationToken)
    {
        var active = Normalize(content);
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
                foreach (var key in target.Keys.ToList())
                {
                    if (!fields.TryGetValue(key, out var value)) { continue; }
                    target[key] = key == "logoPath" ? NormalizeLogoPath(value) : LimitText(value, key.Contains("seo", StringComparison.OrdinalIgnoreCase) ? 180 : 900, target[key]);
                }
            }
        }
        pages["home"]["supportedLanguageLine"] = RequiredLanguageLine;
        var d = input?.Design ?? defaults.Design;
        var design = new WebsiteDesignContent(NormalizeHex(d.HeaderBackgroundColor, defaults.Design.HeaderBackgroundColor), NormalizeHex(d.FooterBackgroundColor, defaults.Design.FooterBackgroundColor), NormalizeHex(d.MainTextColor, defaults.Design.MainTextColor), NormalizeHex(d.HeaderTextColor, defaults.Design.HeaderTextColor), NormalizeFontFamily(d.MainFontFamily, defaults.Design.MainFontFamily), Math.Clamp(d.BaseFontSizePx, 14, 22), AllowedFontWeights().Contains(d.HeaderFontWeight) ? d.HeaderFontWeight : defaults.Design.HeaderFontWeight, Math.Clamp(d.ButtonBorderRadiusPx, 0, 32), LimitText(d.CardTextStyle, 80, defaults.Design.CardTextStyle));
        return new WebsiteContentSet(pages, design);
    }

    private async Task<IReadOnlyList<string>> RenderAllAsync(WebsiteContentSet c, string root, CancellationToken ct)
    {
        var files = new List<string>();
        async Task W(string file, string html) { var path = Path.Combine(root, file); await File.WriteAllTextAsync(path, html, ct); files.Add(path); }
        await W("index.html", RenderHome(c));
        await W("download.html", RenderSimple(c, "download", "download-title", [("Current version", "currentVersionLabel"), ("Safety and support", "safetySupportNote")], "Download for Windows"));
        await W("mobile.html", RenderSimple(c, "mobile", "mobile-title", [("Android", "androidComingSoonText"), ("iOS", "iosComingSoonText"), ("Contact", "emailSupportCtaText")], null));
        await W("pricing.html", RenderSimple(c, "pricing", "pricing-title", [("Free plan", "freePlanText"), ("Premium plan", "premiumPlanText"), ("Trial", "trialText"), ("Checkout status", "paddleLiveCheckoutDisclaimerText")], null));
        await W("support.html", RenderSimple(c, "support", "support-title", [("Support email", "supportEmailText"), ("Response time", "responseTimeText"), ("Accounts and deletion", "accountDeletionSupportText"), ("Billing", "billingSupportText")], null));
        await W("terms.html", RenderSimple(c, "terms", "terms-title", [("Effective date", "effectiveDate"), ("Accounts and use", "accountUseTerms"), ("AI and learning disclaimer", "aiLearningDisclaimer"), ("Billing and subscriptions", "billingSubscriptionTermsPlaceholder"), ("Contact", "contactSupportText")], null));
        await W("privacy.html", RenderSimple(c, "privacy", "privacy-title", [("Effective date", "effectiveDate"), ("Data collected", "dataCollected"), ("Audio and transcription", "audioTranscriptionText"), ("AI processing", "aiProcessingText"), ("Account and payment data", "accountPaymentDataText"), ("Retention and deletion", "dataRetentionDeletionText"), ("Contact", "contactText")], null));
        await W("refunds.html", RenderSimple(c, "refunds", "refunds-title", [("Effective date", "effectiveDate"), ("Refund eligibility", "refundEligibilityText"), ("How to request a refund", "howToRequestRefundText"), ("Payment provider note", "paddlePaymentProviderNote"), ("Contact", "contactText")], null));
        await W("cancellation.html", RenderSimple(c, "cancellation", "cancellation-title", [("Effective date", "effectiveDate"), ("How to cancel", "howToCancelText"), ("Access until period end", "accessUntilPeriodEndText"), ("Support", "supportText")], null));
        await W("seller.html", RenderSimple(c, "seller", "seller-title", [("Seller name / legal entity", "sellerNameLegalEntityPlaceholder"), ("Address", "addressPlaceholder"), ("Contact email", "contactEmail"), ("Tax, VAT, company registration", "taxVatCompanyRegistrationPlaceholder"), ("Paddle live review note", "paddleLiveReviewNote")], null));
        await W("ai-data.html", RenderSimple(c, "aiData", "ai-data-title", [("AI tutor disclosure", "aiTutorDisclosureText"), ("Voice and transcription", "voiceTranscriptionDisclosureText"), ("Data processing", "dataProcessingText"), ("User control and deletion", "userControlDeletionText")], null));
        await W("status.html", RenderSimple(c, "status", "status-title", [("Desktop availability", "desktopAvailabilityText"), ("Mobile", "mobileComingSoonText"), ("Service availability", "serviceAvailabilityDisclaimer"), ("Support", "supportContactText")], null));
        return files;
    }

    private static string RenderHome(WebsiteContentSet c) { var h = c.Pages["home"]; return Shell(c, E(h["seoTitle"]), E(h["seoDescription"]), $"<main class=\"landing-shell\" aria-label=\"Language Voice Tutor applications\"><a class=\"app-panel app-panel--windows\" href=\"download.html\"><img class=\"app-panel__image\" src=\"assets/images/landing/windows-desktop.webp\" alt=\"Preview image for the Language Voice Tutor desktop app\"><span class=\"app-panel__shade\"></span><section class=\"app-panel__content\"><p class=\"app-panel__eyebrow\">{E(h["windowsCardBadge"])}</p><h1>{E(h["windowsCardTitle"])}</h1><p>{E(h["windowsCardDescription"])}</p><span class=\"app-panel__cue\">{E(h["windowsDownloadButtonText"])}</span></section></a><section class=\"app-panel app-panel--mobile app-panel--inactive\"><img class=\"app-panel__image\" src=\"assets/images/landing/mobile.webp\" alt=\"Preview image for future Language Voice Tutor mobile apps\"><span class=\"app-panel__shade\"></span><div class=\"app-panel__content\"><span class=\"app-panel__badge\">{E(h["mobileCardBadge"])}</span><h2>{E(h["mobileCardTitle"])}</h2><p>{E(h["mobileCardDescription"])}</p><span class=\"app-panel__cue app-panel__cue--disabled\">{E(h["mobileComingSoonButtonText"])}</span></div></section></main>", true); }
    private static string RenderSimple(WebsiteContentSet c, string page, string titleId, (string title, string key)[] sections, string? button) { var p = c.Pages[page]; var body = $"<main class=\"page-shell legal-page\"><section class=\"hero-card\"><h1 id=\"{titleId}\">{E(p["pageTitle"])}</h1><p class=\"description\">{E(p["introText"] is var intro ? intro : p.GetValueOrDefault("intro", ""))}</p>{(button is null ? "" : $"<a class=\"download-button\" href=\"#\" aria-disabled=\"true\">{E(p.GetValueOrDefault("downloadButtonText", button))}</a>")}</section>" + string.Concat(sections.Select(s => $"<section class=\"details-card legal-section\"><h2>{E(s.title)}</h2><p>{E(p[s.key])}</p></section>")) + Nav() + "</main>"; return Shell(c, E(p["seoTitle"]), E(p["seoDescription"]), body, false); }
    private static string Shell(WebsiteContentSet c, string title, string description, string main, bool landing) { var h=c.Pages["home"]; var d=c.Design; return $"<!doctype html>\n<html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>{title}</title><meta name=\"description\" content=\"{description}\"><link rel=\"stylesheet\" href=\"styles.css\"><style>:root{{--footer-background:{d.FooterBackgroundColor};--footer-text:{d.HeaderTextColor};--text:{d.MainTextColor};font-size:{d.BaseFontSizePx}px}}body{{font-family:{d.MainFontFamily}}.download-button,.app-panel__cue{{border-radius:{d.ButtonBorderRadiusPx}px}}.landing-page .site-header{{background:{d.HeaderBackgroundColor};color:{d.HeaderTextColor};font-weight:{d.HeaderFontWeight}}.landing-page .app-panel__content{{font-style:{(d.CardTextStyle.Contains("italic",StringComparison.OrdinalIgnoreCase)?"italic":"normal")}}}</style></head><body class=\"{(landing?"landing-page":"")}\"><header class=\"site-header\"><div class=\"site-header__logo\">{Logo(h)}</div><div class=\"site-header__copy\"><p class=\"site-header__headline\">{E(h["topHeaderText"])}</p><p class=\"site-header__languages\">{E(h["supportedLanguageLine"])}</p></div></header>{main}<footer class=\"site-footer\"><p>{E(h["footerCopyrightText"])}</p>{NavLinks(h)}</footer></body></html>"; }
    private static string Logo(Dictionary<string,string> h) => string.IsNullOrWhiteSpace(h["logoPath"]) ? $"<span class=\"site-header__logo-text\">{E(h["fallbackLogoText"])}</span>" : $"<img class=\"site-header__logo-image\" src=\"{HtmlEncoder.Default.Encode(h["logoPath"])}\" alt=\"{E(h["logoAltText"])}\">";
    private static string Nav() => "<section class=\"support-card legal-nav\"><a href=\"index.html\">Home</a><a href=\"download.html\">Download</a><a href=\"mobile.html\">Mobile</a><a href=\"pricing.html\">Pricing</a><a href=\"terms.html\">Terms</a><a href=\"privacy.html\">Privacy</a><a href=\"refunds.html\">Refunds</a><a href=\"cancellation.html\">Cancellation</a><a href=\"support.html\">Support</a></section>";
    private static string NavLinks(Dictionary<string,string> h) => $"<nav class=\"site-footer__links\"><a href=\"privacy.html\">{E(h["footerPrivacyLabel"])}</a><a href=\"terms.html\">{E(h["footerTermsLabel"])}</a><a href=\"refunds.html\">{E(h["footerRefundsLabel"])}</a><a href=\"cancellation.html\">{E(h["footerCancellationLabel"])}</a><a href=\"support.html\">{E(h["footerSupportLabel"])}</a><a href=\"pricing.html\">{E(h["footerPricingLabel"])}</a></nav>";
    private static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

    private static WebsiteContentSet DefaultSet() => new(new()
    {
        ["home"] = new(){{"logoPath",""},{"logoAltText","Language Voice Tutor logo"},{"fallbackLogoText","Language Voice Tutor"},{"topHeaderText","Practice real conversations in:"},{"supportedLanguageLine",RequiredLanguageLine},{"windowsCardBadge","Available for testers"},{"windowsCardTitle","Application for Windows"},{"windowsCardDescription","Practice real-life language lessons by text or voice on your desktop."},{"windowsDownloadButtonText","Download desktop version"},{"mobileCardBadge","In development"},{"mobileCardTitle","Application for mobile devices"},{"mobileCardDescription","Android and iOS versions are planned."},{"mobileComingSoonButtonText","Mobile version coming soon"},{"footerCopyrightText","© Language Voice Tutor. All rights reserved."},{"footerPrivacyLabel","Privacy Policy"},{"footerTermsLabel","Terms of Use"},{"footerRefundsLabel","Refund Policy"},{"footerCancellationLabel","Cancellation"},{"footerSupportLabel","Support"},{"footerPricingLabel","Pricing"},{"seoTitle","Language Voice Tutor"},{"seoDescription","Language Voice Tutor helps you practice real-life language lessons by text or voice on desktop, with mobile apps planned."}},
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
    private static string LimitText(string? value, int max, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string NormalizeHex(string? value, string fallback) => value is not null && HexColorRegex().IsMatch(value.Trim()) ? value.Trim() : fallback;
    private static string NormalizeFontFamily(string? value, string fallback) => value is not null && SafeFontRegex().IsMatch(value.Trim()) ? LimitText(value, 120, fallback) : fallback;
    private static string NormalizeLogoPath(string? value) { var t = value?.Trim() ?? ""; if (t.Length == 0) return ""; if (Uri.TryCreate(t, UriKind.Absolute, out var uri)) return uri.Scheme == Uri.UriSchemeHttps ? t : ""; return SafeRelativePathRegex().IsMatch(t) && !t.Contains("..", StringComparison.Ordinal) ? t : ""; }
    private static HashSet<int> AllowedFontWeights() => [400, 500, 600, 700, 800];
    [GeneratedRegex("^#[0-9a-fA-F]{6}$")] private static partial Regex HexColorRegex();
    [GeneratedRegex("^[a-zA-Z0-9 ,\"-]+$")] private static partial Regex SafeFontRegex();
    [GeneratedRegex("^[a-zA-Z0-9_./%#?=&:+-]+$")] private static partial Regex SafeRelativePathRegex();
}
