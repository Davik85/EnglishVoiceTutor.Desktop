from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PUBLIC = ROOT / "site/public"
SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Website/WebsiteContentService.cs"

PUBLIC_URLS = [
    "https://languagevoicetutor.com/",
    "https://languagevoicetutor.com/index.html",
    "https://languagevoicetutor.com/download.html",
    "https://languagevoicetutor.com/mobile.html",
    "https://languagevoicetutor.com/pricing.html",
    "https://languagevoicetutor.com/support.html",
    "https://languagevoicetutor.com/terms.html",
    "https://languagevoicetutor.com/privacy.html",
    "https://languagevoicetutor.com/refunds.html",
    "https://languagevoicetutor.com/cancellation.html",
    "https://languagevoicetutor.com/seller.html",
    "https://languagevoicetutor.com/ai-data.html",
    "https://languagevoicetutor.com/status.html",
]


def test_google_tags_are_optional_sanitized_and_consent_denied_by_default():
    source = SERVICE.read_text(encoding="utf-8")
    index = (PUBLIC / "index.html").read_text(encoding="utf-8")
    consent_js = (PUBLIC / "marketing-consent.js").read_text(encoding="utf-8")

    assert "SafeGaId" in source and "GaIdRegex" in source
    assert "SafeAdsId" in source and "AdsIdRegex" in source
    assert "googletagmanager.com/gtag/js?id=" in source
    assert "googletagmanager.com/gtag/js?id=" not in index
    assert "gtag('consent', 'default'" in source
    assert 'analytics_storage: "denied"' in consent_js
    assert 'ad_storage: "denied"' in consent_js
    assert 'ad_user_data: "denied"' in consent_js
    assert 'ad_personalization: "denied"' in consent_js
    assert 'id="consent-banner"' in index
    assert "Optional cookies" in index


def test_tracking_hooks_are_config_and_consent_gated_without_breaking_downloads():
    download = (PUBLIC / "download.html").read_text(encoding="utf-8")
    consent_js = (PUBLIC / "marketing-consent.js").read_text(encoding="utf-8")
    download_js = (PUBLIC / "download.js").read_text(encoding="utf-8")

    assert "download_windows_click" in consent_js
    assert "downloadConversionLabel" in consent_js
    assert "choice?.analytics" in consent_js
    assert "choice?.advertising" in consent_js
    assert 'window.lvtMarketing = { gaMeasurementId: \'\', googleAdsId: \'\', downloadConversionLabel: \'\' }' in download
    assert "loadManifest();" in download_js
    assert "normalizeInstallerRelativeUrl" in download_js



def test_public_checkout_pages_include_shared_marketing_consent_runtime():
    expected_config = "window.lvtMarketing = { gaMeasurementId: '', googleAdsId: '', downloadConversionLabel: '' }"
    expected_runtime = 'src="marketing-consent.js?v=marketing-seo" defer'

    for name in ["index.html", "download.html", "pricing.html", "pay.html"]:
        html = (PUBLIC / name).read_text(encoding="utf-8")
        assert expected_config in html
        assert expected_runtime in html
        assert 'id="consent-banner"' in html
        assert 'id="consent-analytics"' in html
        assert 'id="consent-advertising"' in html


def test_robots_sitemap_llms_and_seo_metadata_are_present_and_public_only():
    robots = (PUBLIC / "robots.txt").read_text(encoding="utf-8")
    sitemap = (PUBLIC / "sitemap.xml").read_text(encoding="utf-8")
    llms = (PUBLIC / "llms.txt").read_text(encoding="utf-8")

    assert "Allow: /" in robots
    assert "Disallow: /admin/" in robots
    assert "Disallow: /api/" in robots
    assert "Disallow: /releases/windows/direct/*.exe" in robots
    assert "Sitemap: https://languagevoicetutor.com/sitemap.xml" in robots

    for url in PUBLIC_URLS:
        assert f"<loc>{url}</loc>" in sitemap
    assert "/admin/" not in sitemap
    assert "/api/" not in sitemap
    assert ".exe" not in sitemap

    assert llms.startswith("# Language Voice Tutor")
    assert "Windows desktop application for practicing real-life spoken language lessons" in llms
    assert "Android and iOS apps are planned but not currently available" in llms
    assert "Live paid subscriptions are not enabled" in llms
    assert "Microsoft Store" not in llms

    for name in ["index.html", "download.html", "pricing.html"]:
        html = (PUBLIC / name).read_text(encoding="utf-8")
        assert '<meta name="description"' in html
        assert '<link rel="canonical" href="https://languagevoicetutor.com/' in html
        assert 'property="og:title"' in html
        assert 'name="twitter:card"' in html

    download = (PUBLIC / "download.html").read_text(encoding="utf-8")
    assert '"@type":"SoftwareApplication"' in download
    assert '"operatingSystem":"Windows"' in download
    assert "Android" not in download[download.find('"@type":"SoftwareApplication"'):download.find('</script>', download.find('"@type":"SoftwareApplication"'))]
