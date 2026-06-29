import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PUBLIC = ROOT / "site/public"
SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Website/WebsiteContentService.cs"


def test_privacy_policy_discloses_optional_analytics_ads_and_cookie_consent():
    source = SERVICE.read_text(encoding="utf-8")
    privacy = (PUBLIC / "privacy.html").read_text(encoding="utf-8")
    combined = source + privacy
    for phrase in ["Optional analytics, advertising, and cookie choices", "optional analytics cookies", "optional advertising cookies", "Google Analytics and Google Ads may help", "measure marketing performance", "measure download button clicks", "denied by default", "Accept all", "Reject non-essential", "Manage choices", "site remains usable", "clearing this site's browser storage and cookies"]:
        assert phrase in combined


def test_polished_consent_banner_markup_and_manage_choices_are_published():
    index = (PUBLIC / "index.html").read_text(encoding="utf-8")
    source = SERVICE.read_text(encoding="utf-8")
    combined = source + index
    assert 'class="consent-banner"' in index
    assert 'role="dialog"' in index
    assert 'aria-labelledby="consent-title"' in index
    assert 'class="consent-banner__content"' in index
    assert 'class="consent-button consent-button--primary"' in index
    assert 'class="consent-button consent-button--secondary"' in index
    assert 'class="consent-button consent-button--link"' in index
    assert 'class="consent-choices" id="consent-manage"' in index
    assert "Analytics cookies" in index
    assert "Advertising cookies" in index
    assert ".consent-banner { position: fixed;" in combined
    assert "border-radius: 24px" in combined


def test_empty_public_google_settings_emit_no_scripts_or_placeholder_ids():
    public_html = "\n".join(p.read_text(encoding="utf-8") for p in PUBLIC.glob("*.html"))
    assert not re.search(r"G-[A-Z0-9]{6,}", public_html)
    assert not re.search(r"AW-[0-9]{6,}", public_html)
    assert "googletagmanager.com/gtag/js" not in public_html
    assert "G-XXXXXXXXXX" not in public_html
    assert "AW-123456789" not in public_html


def test_consent_runtime_loads_google_only_after_valid_config_and_choice():
    consent_js = (PUBLIC / "marketing-consent.js").read_text(encoding="utf-8")
    source = SERVICE.read_text(encoding="utf-8")
    assert 'analytics_storage: "denied"' in consent_js
    assert 'ad_storage: "denied"' in consent_js
    assert 'ad_user_data: "denied"' in consent_js
    assert 'ad_personalization: "denied"' in consent_js
    assert "loadGoogleTags(choice)" in consent_js
    assert "choice?.analytics && config.gaMeasurementId" in consent_js
    assert "choice?.advertising && config.googleAdsId" in consent_js
    assert "transport_type: \"beacon\"" in consent_js
    assert "SafeGaId" in source
    assert "SafeAdsId" in source


def test_google_id_sanitizers_accept_safe_samples_only_when_enabled_and_reject_unsafe_values():
    source = SERVICE.read_text(encoding="utf-8")

    assert 'IsEnabled(m, "enableAnalytics") ? SafeGaId(MarketingValue(m, "googleAnalyticsMeasurementId")) : string.Empty' in source
    assert 'IsEnabled(m, "enableAdsTracking") ? SafeAdsId(MarketingValue(m, "googleAdsId")) : string.Empty' in source
    assert '[GeneratedRegex("^G-[A-Z0-9]{6,16}$")]' in source
    assert '[GeneratedRegex("^AW-[0-9]{6,16}$")]' in source
    assert '"googleAnalyticsMeasurementId" => SafeGaId(raw)' in source
    assert '"googleAdsId" => SafeAdsId(raw)' in source
    assert '"enableAnalytics" or "enableAdsTracking"' in source
