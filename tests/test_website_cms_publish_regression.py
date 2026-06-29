from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Website/WebsiteContentService.cs"


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
    manifest = (ROOT / "site/public/releases/windows/direct/latest.json").read_text(encoding="utf-8")

    assert "ReadStaticReleaseManifest(root)" in source
    assert "Current Windows tester release is available through the Download for Windows button." in source
    assert "If release details do not load automatically, please contact" in download_html
    assert "Version</dt>\n                    <dd id=\"detail-version\">Unavailable</dd>" not in download_html
    assert "0.1.36-tester.31" in manifest
    assert "0.1.36-tester.31" in download_html
    assert "LanguageVoiceTutorSetup-0.1.36-tester.31.exe" in download_html
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
