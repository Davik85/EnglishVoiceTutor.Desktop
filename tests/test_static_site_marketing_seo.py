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
    "/english-speaking-practice.html",
    "/french-speaking-practice.html",
    "/german-speaking-practice.html",
    "/spanish-speaking-practice.html",
    "/italian-speaking-practice.html",
    "/portuguese-speaking-practice.html",
]

EXPECTED_LANGUAGE_PRACTICE_PAGES = {
    "english-speaking-practice.html": {
        "title": "English Speaking Practice with AI | Orralen",
        "h1": "Practice English Speaking with an AI Tutor",
        "canonical": "https://languagevoicetutor.com/english-speaking-practice.html",
        "description": "Practise speaking English with an AI tutor by voice or text. Build confidence through everyday conversations, clear corrections and 100+ guided lessons.",
    },
    "french-speaking-practice.html": {
        "title": "French Speaking Practice with AI | Orralen",
        "h1": "Practice French Speaking with an AI Tutor",
        "canonical": "https://languagevoicetutor.com/french-speaking-practice.html",
        "description": "Practise speaking French with an AI tutor by voice or text. Build useful conversation skills with everyday situations, clear corrections and guided lessons.",
    },
    "german-speaking-practice.html": {
        "title": "German Speaking Practice with AI | Orralen",
        "h1": "Practice German Speaking with an AI Tutor",
        "canonical": "https://languagevoicetutor.com/german-speaking-practice.html",
        "description": "Practise speaking German with an AI tutor by voice or text. Work through everyday situations from A1 to B2 with clear corrections and guided lessons.",
    },
    "spanish-speaking-practice.html": {
        "title": "Spanish Speaking Practice with AI | Orralen",
        "h1": "Practice Spanish Speaking with an AI Tutor",
        "canonical": "https://languagevoicetutor.com/spanish-speaking-practice.html",
        "description": "Practise speaking Spanish with an AI tutor by voice or text. Build confidence with everyday conversations, clear corrections and 100+ guided lessons.",
    },
    "italian-speaking-practice.html": {
        "title": "Italian Speaking Practice with AI | Orralen",
        "h1": "Practice Italian Speaking with an AI Tutor",
        "canonical": "https://languagevoicetutor.com/italian-speaking-practice.html",
        "description": "Practise speaking Italian with an AI tutor by voice or text. Use everyday situations from A1 to B2, get clear corrections and explore guided lessons.",
    },
    "portuguese-speaking-practice.html": {
        "title": "Portuguese Speaking Practice with AI | Orralen",
        "h1": "Practice Portuguese Speaking with an AI Tutor",
        "canonical": "https://languagevoicetutor.com/portuguese-speaking-practice.html",
        "description": "Practise speaking Portuguese with an AI tutor by voice or text. Build confidence in everyday situations with clear corrections and 100+ guided lessons.",
    },
}

EXPECTED_LANGUAGE_LINK_TITLES = {
    name: f"{name.split('-', 1)[0].title()} speaking practice"
    for name in EXPECTED_LANGUAGE_PRACTICE_PAGES
}


class _HomepageHeadParser(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.links = []
        self.metas = []
        self.titles = []
        self.h1s = []
        self.json_ld = []
        self._title_parts = None
        self._h1_parts = None
        self._json_parts = None

    def handle_starttag(self, tag, attrs):
        attributes = dict(attrs)
        if tag == "link":
            self.links.append(attributes)
        elif tag == "meta":
            self.metas.append(attributes)
        elif tag == "title":
            self._title_parts = []
        elif tag == "h1":
            self._h1_parts = []
        elif tag == "script" and attributes.get("type") == "application/ld+json":
            self._json_parts = []

    def handle_data(self, data):
        if self._title_parts is not None:
            self._title_parts.append(data)
        if self._h1_parts is not None:
            self._h1_parts.append(data)
        if self._json_parts is not None:
            self._json_parts.append(data)

    def handle_endtag(self, tag):
        if tag == "title" and self._title_parts is not None:
            self.titles.append("".join(self._title_parts).strip())
            self._title_parts = None
        elif tag == "h1" and self._h1_parts is not None:
            self.h1s.append("".join(self._h1_parts).strip())
            self._h1_parts = None
        elif tag == "script" and self._json_parts is not None:
            self.json_ld.append("".join(self._json_parts))
            self._json_parts = None


class _HomepageLanguageLinkParser(HTMLParser):
    _VOID_ELEMENTS = {"area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"}

    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.links = {"language-strip": [], "language-box": []}
        self.link_attributes = {"language-strip": [], "language-box": []}
        self._open_elements = []

    def handle_starttag(self, tag, attrs):
        attributes = dict(attrs)
        classes = set(attributes.get("class", "").split())
        group = next((name for name in self.links if name in classes), None)

        if tag == "a" and attributes.get("href"):
            for _, parent_group in reversed(self._open_elements):
                if parent_group is not None:
                    self.links[parent_group].append(attributes["href"])
                    self.link_attributes[parent_group].append(attributes)
                    break

        if tag not in self._VOID_ELEMENTS:
            self._open_elements.append((tag, group))

    def handle_endtag(self, tag):
        for index in range(len(self._open_elements) - 1, -1, -1):
            if self._open_elements[index][0] == tag:
                del self._open_elements[index:]
                break


def _meta_values(parser, attribute, key):
    return [meta.get("content") for meta in parser.metas if meta.get(attribute) == key]


def _graph_node(nodes, node_type, node_id):
    matches = [node for node in nodes if node.get("@type") == node_type and node.get("@id") == node_id]
    assert len(matches) == 1
    return matches[0]


def _logo_url(logo):
    return logo if isinstance(logo, str) else logo.get("url") or logo.get("contentUrl")


def test_independent_homepage_references_external_stylesheet_once():
    stylesheet_href = "/assets/homepage/index.css?v=20260913-visual3"
    index = (PUBLIC / "index.html").read_text(encoding="utf-8")

    assert index.count(stylesheet_href) == 1
    assert (PUBLIC / stylesheet_href.split("?", 1)[0].removeprefix("/")).is_file()


def test_language_practice_pages_have_focused_static_seo_and_download_paths():
    stylesheet_href = "/assets/homepage/index.css?v=20260913-visual3"
    practice_stylesheet_href = "/assets/homepage/language-practice.css?v=20260913-visual3"
    consent_runtime = 'src="/marketing-consent.js?v=marketing-seo"'
    google_play_url = "https://play.google.com/store/apps/details?id=com.languagevoicetutor.mobile"
    windows_url = "https://languagevoicetutor.com/download.html"
    trial_sentence = "Register and get 7 days of Premium. The Free plan includes one free lesson every day."
    product_image_paths = [
        "/assets/homepage/devices/hero-screen.jpg",
        "/assets/homepage/devices/mobile-app.jpg",
        "/assets/homepage/devices/windows-laptop.png",
        "/assets/homepage/people/lana.jpg",
    ]
    page_image_paths = [
        "/assets/homepage/devices/hero-screen.jpg",
    ]
    expected_language_links = [f'/{name}' for name in EXPECTED_LANGUAGE_PRACTICE_PAGES]
    expected_language_titles = list(EXPECTED_LANGUAGE_LINK_TITLES.values())
    descriptions = []
    canonicals = []

    for name, expected in EXPECTED_LANGUAGE_PRACTICE_PAGES.items():
        path = PUBLIC / name
        assert path.is_file(), name
        html = path.read_text(encoding="utf-8")
        parser = _HomepageHeadParser()
        parser.feed(html)
        language_parser = _HomepageLanguageLinkParser()
        language_parser.feed(html)

        page_canonicals = [
            link.get("href")
            for link in parser.links
            if "canonical" in link.get("rel", "").lower().split()
        ]
        page_descriptions = _meta_values(parser, "name", "description")

        assert parser.titles == [expected["title"]], name
        assert parser.h1s == [expected["h1"]], name
        assert page_canonicals == [expected["canonical"]], name
        assert page_descriptions == [expected["description"]], name
        assert _meta_values(parser, "name", "robots") == ["index, follow"], name
        assert _meta_values(parser, "property", "og:title") == [expected["title"]], name
        assert _meta_values(parser, "property", "og:description") == [expected["description"]], name
        assert _meta_values(parser, "property", "og:url") == [expected["canonical"]], name
        assert _meta_values(parser, "name", "twitter:title") == [expected["title"]], name
        assert html.count(stylesheet_href) == 1, name
        assert html.count(consent_runtime) == 1, name
        assert html.count(practice_stylesheet_href) == 1, name
        assert google_play_url in html, name
        assert windows_url in html, name
        assert html.count(trial_sentence) == 1, name
        assert 'id="consent-banner"' in html, name
        assert 'class="practice-hero__product-frame"' in html, name
        assert 'fetchpriority="high"' in html, name
        assert all(image_path in html for image_path in page_image_paths), name
        assert html.count('class="topbar"') == 1, name
        assert html.count('class="language-strip"') == 1, name
        assert language_parser.links["language-strip"] == expected_language_links, name
        assert [
            attributes.get("title")
            for attributes in language_parser.link_attributes["language-strip"]
        ] == expected_language_titles, name
        assert [
            attributes.get("href")
            for attributes in language_parser.link_attributes["language-strip"]
            if attributes.get("aria-current") == "page"
        ] == [f"/{name}"], name
        assert 'class="practice-header"' not in html, name
        assert 'class="practice-language-nav"' not in html, name
        assert "Back to homepage" not in html, name
        assert 'class="practice-tutors"' not in html, name
        assert 'class="practice-tutor-row"' not in html, name
        assert "practice-download-visual" not in html, name
        assert "practice-download-visual__phone" not in html, name
        assert "practice-download-visual__laptop" not in html, name
        assert not re.search(r"G-[A-Z0-9]{6,16}", html), name
        assert not re.search(r"AW-\d+", html), name
        assert "google-site-verification" not in html.lower(), name

        descriptions.extend(page_descriptions)
        canonicals.extend(page_canonicals)

    assert len(descriptions) == len(set(descriptions)) == 6
    assert len(canonicals) == len(set(canonicals)) == 6
    assert all((PUBLIC / image_path.removeprefix("/")).is_file() for image_path in product_image_paths)

    practice_css = (PUBLIC / practice_stylesheet_href.split("?", 1)[0].removeprefix("/")).read_text(encoding="utf-8")
    assert "practice-dialogue::before" in practice_css
    assert 'url("/assets/homepage/people/lana.jpg")' in practice_css
    assert 'url("/assets/homepage/devices/mobile-app.jpg")' in practice_css
    assert 'url("/assets/homepage/devices/windows-laptop.png")' in practice_css
    assert ".practice-header" not in practice_css
    assert ".practice-language-nav" not in practice_css
    assert ".practice-tutors" not in practice_css
    assert ".practice-tutor-row" not in practice_css
    assert ".practice-section__intro::before" not in practice_css
    assert ".practice-card::before" not in practice_css
    assert ".practice-card:nth-child" not in practice_css
    assert ".practice-section__intro {\n  max-width: 780px;\n  margin: 0 auto 34px;\n  text-align: center;\n}" in practice_css

    homepage_css = (PUBLIC / stylesheet_href.split("?", 1)[0].removeprefix("/")).read_text(encoding="utf-8")
    assert ".language-strip .lang:hover .flag-icon" in homepage_css
    assert ".language-strip .lang:focus-visible .flag-icon" in homepage_css
    assert "transform:scale(1.12)" in homepage_css


def test_independent_homepage_has_root_social_and_application_seo_metadata():
    homepage_title = "AI Speaking Practice in 6 Languages | Orralen"
    homepage_description = (
        "Practice English, French, German, Spanish, Italian and Portuguese with an AI tutor. "
        "Speak or type, get corrections, and train at CEFR levels A1–B2."
    )
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
    assert parser.titles == [homepage_title]
    assert _meta_values(parser, "name", "description") == [homepage_description]
    assert _meta_values(parser, "property", "og:title") == [homepage_title]
    assert _meta_values(parser, "property", "og:description") == [homepage_description]
    assert _meta_values(parser, "name", "twitter:title") == [homepage_title]
    assert _meta_values(parser, "name", "twitter:description") == [homepage_description]
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
    assert webpage["name"] == homepage_title
    assert webpage["description"] == homepage_description
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


def test_independent_homepage_language_selectors_link_to_practice_pages():
    html = (PUBLIC / "index.html").read_text(encoding="utf-8")
    parser = _HomepageLanguageLinkParser()
    parser.feed(html)

    expected_links = [f"/{name}" for name in EXPECTED_LANGUAGE_PRACTICE_PAGES]
    assert html.count('class="topbar"') == 1
    assert html.count('class="language-strip"') == 1
    assert parser.links["language-strip"] == expected_links
    assert [
        attributes.get("title")
        for attributes in parser.link_attributes["language-strip"]
    ] == list(EXPECTED_LANGUAGE_LINK_TITLES.values())
    assert parser.links["language-box"] == expected_links


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
