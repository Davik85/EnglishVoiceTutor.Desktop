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
    assert "Normalize(new WebsiteContentSet(pages, design))" in source


def test_home_stays_structured_and_simple_pages_keep_body_markdown_rendering():
    source = SERVICE.read_text(encoding="utf-8")

    assert '["home"] = new(){{"logoPath"' in source
    assert 'private static string RenderHome(WebsiteContentSet c)' in source
    assert 'private static string RenderSimple' in source
    assert 'bodyMarkdown' in source
    assert 'RenderMarkdown(p["bodyMarkdown"])' in source
