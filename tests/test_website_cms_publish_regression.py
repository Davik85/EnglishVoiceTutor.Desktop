from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Website/WebsiteContentService.cs"
ADMIN_JS = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"


def test_homepage_asset_fallbacks_use_existing_public_assets():
    source = SERVICE.read_text(encoding="utf-8")

    expected_assets = [
        "assets/brand/lvt-logo.png",
        "assets/flags/gb.webp",
        "assets/flags/fr.webp",
        "assets/flags/de.webp",
        "assets/flags/es.webp",
        "assets/flags/it.webp",
        "assets/flags/pt.webp",
    ]

    for asset in expected_assets:
        assert (ROOT / "site/public" / asset).is_file(), asset
        assert asset in source

    assert '<img class=\\"site-header__logo-image\\"' in source
    assert '<img class=\\"site-header__flag\\"' in source
    assert 'string.IsNullOrWhiteSpace(h["logoPath"])' not in source


def test_save_draft_merges_into_existing_draft_instead_of_replacing_all_content():
    source = SERVICE.read_text(encoding="utf-8")

    assert "Draft = MergeDraft(document.Draft, draft)" in source
    assert "var pages = merged.Pages.ToDictionary" in source
    assert "incomingDraft.Pages" in source
    assert "incomingDraft.Design is null ? merged.Design" in source
    assert "Normalize(new WebsiteContentSet(pages, design, marketing))" in source


def test_home_stays_structured_and_simple_pages_keep_body_markdown_rendering():
    source = SERVICE.read_text(encoding="utf-8")

    assert '["home"] = new(){{"logoPath"' in source
    assert 'private static string RenderHome(WebsiteContentSet c, bool includePublicBaseHref)' in source
    assert 'private static string RenderSimple' in source
    assert 'bodyMarkdown' in source
    assert 'RenderMarkdown(p["bodyMarkdown"])' in source


def test_public_website_polish_regressions_are_locked():
    source = SERVICE.read_text(encoding="utf-8")
    index_html = (ROOT / "site/public/index.html").read_text(encoding="utf-8")
    download_html = (ROOT / "site/public/download.html").read_text(encoding="utf-8")
    styles = (ROOT / "site/public/styles.css").read_text(encoding="utf-8")
    manifest = (ROOT / "site/public/releases/windows/direct/latest.json").read_text(encoding="utf-8")

    assert "ReadStaticReleaseManifest(root)" in source
    assert "Language Voice Tutor for Windows" in source
    assert "Technical release details" in source
    assert "download-hero" in source
    assert "Language Voice Tutor for Windows" in download_html
    assert "Download for Windows" in download_html
    assert "Start quickly" in download_html
    assert "Choose practical topics" in download_html
    assert "Learn step by step" in download_html
    assert "Practice real conversation" in download_html
    assert "Technical release details" in download_html
    assert "assets/images/landing/windows-desktop.webp" in styles
    assert ".site-footer > p {\n    white-space: pre-line;\n}" in styles
    assert "color: #102A43" in styles
    assert "color: #8A7557" in styles
    assert "color: #FFFFFF" in styles
    assert "border: 1px solid rgba(23, 50, 77, 0.28)" in styles
    assert "box-shadow: 0 1px 2px rgba(23, 50, 77, 0.18)" in styles
    assert "#F2E8D5" not in styles
    assert "#1B2A3A" not in styles
    assert "#EDE7DC" not in styles
    assert "Version</dt>\n                    <dd id=\"detail-version\">Unavailable</dd>" not in download_html
    assert "1.0" in manifest
    assert "1.0" in download_html
    assert "LanguageVoiceTutorSetup-1.0.exe" in download_html
    assert 'src="download.js?v=' in download_html

    assert "Mobile version coming soon" not in source
    assert "Mobile version coming soon" not in index_html
    assert "Android and iOS apps are planned but are not currently available." in source
    assert "Android and iOS apps are planned but are not currently available." in index_html
    assert "Not currently available" in index_html

    assert 'site-footer__link-row site-footer__link-row--primary' in index_html
    assert 'site-footer__link-row site-footer__link-row--secondary' in index_html
    assert 'href="seller.html"' in index_html
    assert 'href="ai-data.html"' in index_html
    assert 'href="status.html"' in index_html


def test_homepage_header_logo_sizing_and_overflow_breakpoint_are_locked():
    styles = (ROOT / "site/public/styles.css").read_text(encoding="utf-8")

    assert "width: 132px !important;" in styles
    assert "max-height: 66px !important;" in styles
    assert "max-width: 124px !important;" in styles
    assert "@media (min-width: 901px) and (max-width: 1200px)" in styles
    assert "flex-wrap: wrap !important;" in styles
    assert "padding: 0 32px !important;" in styles
    assert "height: 88px !important;" in styles
    assert "object-fit: contain !important;" in styles


def test_website_design_editor_submits_independent_footer_text_color():
    source = SERVICE.read_text(encoding="utf-8")
    admin_js = ADMIN_JS.read_text(encoding="utf-8")

    assert "FooterTextColor" in source
    assert "--footer-text: {d.FooterTextColor ?? DefaultFooterTextColor}" in source
    assert '["footerTextColor", "Footer text color"]' in admin_js
    assert "data-website-design-key" in admin_js
    assert "websiteContentDraft.design ||= {}" in admin_js
    assert "body: JSON.stringify(websiteContentDraft)" in admin_js
