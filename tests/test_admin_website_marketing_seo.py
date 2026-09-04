from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ADMIN_JS = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"
SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Website/WebsiteContentService.cs"

MARKETING_KEYS = [
    "enableConsentBanner",
    "enableAnalytics",
    "googleAnalyticsMeasurementId",
    "enableAdsTracking",
    "googleAdsId",
    "googleAdsDownloadConversionLabel",
    "googleSearchConsoleVerificationToken",
    "enableLlmsTxt",
]


def test_admin_website_cms_has_visible_marketing_seo_fields_and_labels():
    source = ADMIN_JS.read_text(encoding="utf-8")

    assert "Marketing / SEO" in source
    assert "Optional public marketing settings" in source
    for key in MARKETING_KEYS:
        assert key in source
    for label in [
        "Enable consent banner",
        "Enable analytics",
        "Google Analytics Measurement ID",
        "Enable ads tracking",
        "Google Ads ID",
        "Google Ads download conversion label",
        "Google Search Console verification token",
        "Enable llms.txt",
    ]:
        assert label in source


def test_admin_marketing_settings_round_trip_with_json_contract():
    admin_js = ADMIN_JS.read_text(encoding="utf-8")
    service = SERVICE.read_text(encoding="utf-8")

    assert "websiteContentDraft.marketing ||= {}" in admin_js
    assert "data-website-marketing-key" in admin_js
    assert 'marketing[key] = input.type === "checkbox" ? String(input.checked) : input.value' in admin_js
    assert "body: JSON.stringify(websiteContentDraft)" in admin_js
    assert "body: JSON.stringify({ content: websiteContentDraft" in admin_js
    assert "Dictionary<string, string>? Marketing = null" in (ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Website/WebsiteContentContracts.cs").read_text(encoding="utf-8")
    assert "var marketing = MergeMarketing(merged.Marketing, incomingDraft.Marketing)" in service
    assert "NormalizeMarketing(input?.Marketing" in service


def test_publish_writes_marketing_artifacts_and_release_manifest_is_source():
    service = SERVICE.read_text(encoding="utf-8")

    assert 'await W("robots.txt", RenderRobotsTxt())' in service
    assert 'await W("sitemap.xml", RenderSitemapXml' in service
    assert 'await W("llms.txt", RenderLlmsTxt())' in service
    assert 'await W("marketing-consent.js", RenderMarketingConsentJs(c.Marketing))' in service
    assert "ReadStaticReleaseManifest(root)" in service
    assert "LanguageVoiceTutorSetup-1.0.exe" not in service
