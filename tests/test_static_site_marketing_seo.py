import json
import re
from html.parser import HTMLParser
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PUBLIC = ROOT / "site/public"
SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Website/WebsiteContentService.cs"

EXPECTED_SITEMAP_PATHS = [
    "/",
    "/download.html",
    "/pricing.html",
    "/support.html",
    "/terms.html",
    "/privacy.html",
    "/refunds.html",
    "/cancellation.html",
    "/seller.html",
    "/ai-data.html",
    "/status.html",
]


class _HomepageHeadParser(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.links = []
        self.metas = []
        self.titles = []
        self.json_ld = []
        self._title_parts = None
        self._json_parts = None

    def handle_starttag(self, tag, attrs):
        attributes = dict(attrs)
        if tag == "link":
            self.links.append(attributes)
        elif tag == "meta":
            self.metas.append(attributes)
        elif tag == "title":
            self._title_parts = []
        elif tag == "script" and attributes.get("type") == "application/ld+json":
            self._json_parts = []

    def handle_data(self, data):
        if self._title_parts is not None:
            self._title_parts.append(data)
        if self._json_parts is not None:
            self._json_parts.append(data)

    def handle_endtag(self, tag):
        if tag == "title" and self._title_parts is not None:
            self.titles.append("".join(self._title_parts).strip())
            self._title_parts = None
        elif tag == "script" and self._json_parts is not None:
            self.json_ld.append("".join(self._json_parts))
            self._json_parts = None


def _meta_values(parser, attribute, key):
    return [meta.get("content") for meta in parser.metas if meta.get(attribute) == key]


def _graph_node(nodes, node_type, node_id):
    matches = [node for node in nodes if node.get("@type") == node_type and node.get("@id") == node_id]
    assert len(matches) == 1
    return matches[0]


def _logo_url(logo):
    return logo if isinstance(logo, str) else logo.get("url") or logo.get("contentUrl")


def test_independent_homepage_has_root_social_and_application_seo_metadata():
    raw_html = (PUBLIC / "index.html").read_bytes()
    assert len(raw_html) < 1_000_000
    for metadata_token in [
        b"og:title",
        b"og:description",
        b"og:url",
        b"og:image",
        b"twitter:image",
    ]:
        offset = raw_html.lower().find(metadata_token)
        assert 0 <= offset < 32 * 1024
    body_offset = raw_html.lower().find(b"<body")
    assert body_offset >= 0
    head_html = raw_html[:body_offset].decode("utf-8")

    parser = _HomepageHeadParser()
    parser.feed(head_html)

    canonicals = [
        link.get("href")
        for link in parser.links
        if "canonical" in link.get("rel", "").lower().split()
    ]
    assert canonicals == ["https://languagevoicetutor.com/"]
    assert parser.titles == [
        "AI Language Tutor for Speaking Practice & Real Conversations | Language Voice Tutor"
    ]
    assert _meta_values(parser, "name", "description") == [
        "Practice English, French, German, Spanish, Italian, and Portuguese with an AI language tutor. Improve speaking through realistic voice and text conversations, CEFR levels A1–B2, guided lessons, and instant corrections."
    ]
    assert _meta_values(parser, "name", "robots") == [
        "index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1"
    ]
    assert _meta_values(parser, "property", "og:url") == ["https://languagevoicetutor.com/"]
    assert _meta_values(parser, "property", "og:image") == [
        "https://languagevoicetutor.com/assets/brand/lvt-logo.png"
    ]
    assert _meta_values(parser, "name", "twitter:image") == [
        "https://languagevoicetutor.com/assets/brand/lvt-logo.png"
    ]
    assert "/ai-language-tutor" not in head_html

    documents = [json.loads(block) for block in parser.json_ld]
    assert documents
    nodes = []
    for document in documents:
        if isinstance(document, dict) and isinstance(document.get("@graph"), list):
            nodes.extend(document["@graph"])
        elif isinstance(document, dict):
            nodes.append(document)
        else:
            nodes.extend(document)

    node_ids = [node["@id"] for node in nodes if "@id" in node]
    assert len(node_ids) == len(set(node_ids))

    website = _graph_node(
        nodes, "WebSite", "https://languagevoicetutor.com/#website"
    )
    assert website["url"] == "https://languagevoicetutor.com/"
    assert website["name"] == "Language Voice Tutor"

    webpage = _graph_node(
        nodes, "WebPage", "https://languagevoicetutor.com/#webpage"
    )
    assert webpage["url"] == "https://languagevoicetutor.com/"
    assert {item["@id"] for item in webpage["mainEntity"]} == {
        "https://languagevoicetutor.com/#windows-app",
        "https://languagevoicetutor.com/#android-app",
    }

    windows_app = _graph_node(
        nodes, "SoftwareApplication", "https://languagevoicetutor.com/#windows-app"
    )
    assert windows_app["downloadUrl"] == "https://languagevoicetutor.com/download.html"

    android_app = _graph_node(
        nodes, "SoftwareApplication", "https://languagevoicetutor.com/#android-app"
    )
    assert android_app["downloadUrl"] == (
        "https://play.google.com/store/apps/details?id=com.languagevoicetutor.mobile"
    )

    organizations = [node for node in nodes if node.get("@type") == "Organization"]
    for organization in organizations:
        assert _logo_url(organization["logo"]) == (
            "https://languagevoicetutor.com/assets/brand/lvt-logo.png"
        )


def test_google_tags_are_optional_sanitized_and_consent_denied_by_default():
    source = SERVICE.read_text(encoding="utf-8")
    index = (PUBLIC / "index.html").read_text(encoding="utf-8")
    consent_js = (PUBLIC / "marketing-consent.js").read_text(encoding="utf-8")

    assert "SafeGaId" in source and "GaIdRegex" in source
    assert "SafeAdsId" in source and "AdsIdRegex" in source
    assert "googletagmanager.com/gtag/js?id=" in source
    assert "googletagmanager.com/gtag/js?id=" not in index
    assert 'src="/marketing-consent.js?v=marketing-seo" defer' in index
    assert 'id="consent-banner"' in index
    assert not re.search(r"G-[A-Z0-9]{6,16}", index)
    assert "fallbackMarketing" in consent_js
    assert "if (!window.lvtMarketing)" in consent_js
    assert "gtag('consent', 'default'" in source
    assert 'analytics_storage: "denied"' in consent_js
    assert 'ad_storage: "denied"' in consent_js
    assert 'ad_user_data: "denied"' in consent_js
    assert 'ad_personalization: "denied"' in consent_js
    assert 'id="consent-banner"' in source
    assert "Optional cookies" in source


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

    for name in ["download.html", "pricing.html", "pay.html"]:
        html = (PUBLIC / name).read_text(encoding="utf-8")
        assert expected_config in html
        assert expected_runtime in html
        assert 'id="consent-banner"' in html
        assert 'id="consent-analytics"' in html
        assert 'id="consent-advertising"' in html


def test_robots_sitemap_llms_and_seo_metadata_are_present_and_public_only():
    source = SERVICE.read_text(encoding="utf-8")
    robots_start = source.index("private static string RenderRobotsTxt")
    sitemap_start = source.index("private static string RenderSitemapXml")
    llms_start = source.index("private static string RenderLlmsTxt")
    consent_js_start = source.index("private static string RenderMarketingConsentJs")

    robots_renderer = source[robots_start:sitemap_start]
    sitemap_renderer = source[sitemap_start:llms_start]
    llms_renderer = source[llms_start:consent_js_start]

    assert "Allow: /" in robots_renderer
    assert "Disallow: /admin/" in robots_renderer
    assert "Disallow: /api/" in robots_renderer
    assert "Disallow: /releases/windows/direct/*.exe" in robots_renderer
    assert "Sitemap: https://languagevoicetutor.com/sitemap.xml" in robots_renderer

    routes_match = re.search(r"var urls = new\[\] \{(?P<routes>[^}]*)\}", sitemap_renderer)
    assert routes_match is not None
    sitemap_paths = re.findall(r'"([^"]+)"', routes_match.group("routes"))
    assert sitemap_paths == EXPECTED_SITEMAP_PATHS
    assert sitemap_paths.count("/") == 1
    assert "/index.html" not in sitemap_paths
    assert "/mobile.html" not in sitemap_paths
    assert "/ai-language-tutor/" not in sitemap_paths
    assert "/admin/" not in sitemap_renderer
    assert "/api/" not in sitemap_renderer
    assert ".exe" not in sitemap_renderer

    assert "# Language Voice Tutor" in llms_renderer
    assert "Language Voice Tutor is available for Windows and Android" in llms_renderer
    assert "the Android app is publicly available on Google Play" in llms_renderer
    assert "https://play.google.com/store/apps/details?id=com.languagevoicetutor.mobile" in llms_renderer
    assert "Android and iOS apps are planned but not currently available" not in llms_renderer
    assert "Live paid subscriptions are not enabled" in llms_renderer
    assert "Microsoft Store" not in llms_renderer

    for name in ["download.html", "pricing.html"]:
        html = (PUBLIC / name).read_text(encoding="utf-8")
        assert '<meta name="description"' in html
        assert '<link rel="canonical" href="https://languagevoicetutor.com/' in html
        assert 'property="og:title"' in html
        assert 'name="twitter:card"' in html

    download = (PUBLIC / "download.html").read_text(encoding="utf-8")
    assert '"@type":"SoftwareApplication"' in download
    assert '"operatingSystem":"Windows"' in download
    assert "Android" not in download[download.find('"@type":"SoftwareApplication"'):download.find('</script>', download.find('"@type":"SoftwareApplication"'))]
