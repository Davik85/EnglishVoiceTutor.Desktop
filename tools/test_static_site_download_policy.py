#!/usr/bin/env python3
"""Policy checks for the static public direct download page foundation."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
SITE_PUBLIC = ROOT / "site" / "public"
DOWNLOAD_HTML = SITE_PUBLIC / "download.html"
DOWNLOAD_JS = SITE_PUBLIC / "download.js"
LATEST_JSON = SITE_PUBLIC / "releases" / "windows" / "direct" / "latest.json"
STYLES = SITE_PUBLIC / "styles.css"
UPLOAD_SCRIPT = ROOT / "scripts" / "upload-static-site.ps1"
OLD_INSTALLER_VERSION_PATTERN = re.compile(r"0\.1\.(?:13|16|17|18)")
INSTALLER_EXE_PATTERN = re.compile(r'LanguageVoiceTutorSetup-[^\s"\'<>]+\.exe')

SENSITIVE_ASSIGNMENT_PATTERNS = [
    re.compile(r"(?i)(api[_-]?key|secret|password|passwd|pwd|token)\s*[:=]\s*['\"][^'\"]+['\"]"),
    re.compile(r"(?i)(bearer|basic)\s+[a-z0-9._~+/=-]{16,}"),
    re.compile(r"(?i)-----BEGIN (?:RSA |OPENSSH |EC |DSA )?PRIVATE KEY-----"),
]


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_no_sensitive_values(path: pathlib.Path) -> None:
    text = read(path)
    for pattern in SENSITIVE_ASSIGNMENT_PATTERNS:
        match = pattern.search(text)
        if match:
            raise AssertionError(f"Potential sensitive value found in {path.relative_to(ROOT)}: {match.group(0)}")


def assert_no_hardcoded_installer_fallback(path: pathlib.Path) -> None:
    text = read(path)
    manifest_installer = read(LATEST_JSON) if LATEST_JSON.exists() else ""
    if OLD_INSTALLER_VERSION_PATTERN.search(text):
        raise AssertionError(f"Old installer version is hardcoded in {path.relative_to(ROOT)}")

    for match in INSTALLER_EXE_PATTERN.finditer(text):
        matched_text = match.group(0)
        before = text[max(0, match.start() - 80):match.start()]
        if "A-Za-z0-9._-" in matched_text or "installerFileName" in before or "installerRelativeUrl" in before:
            continue
        if matched_text in manifest_installer:
            continue
        raise AssertionError(f"Hardcoded installer executable fallback in {path.relative_to(ROOT)}: {matched_text}")


def main() -> int:
    for path in [DOWNLOAD_HTML, DOWNLOAD_JS, STYLES, UPLOAD_SCRIPT, LATEST_JSON]:
        if not path.exists():
            raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")

    download_html = read(DOWNLOAD_HTML)
    download_js = read(DOWNLOAD_JS)
    upload_script = read(UPLOAD_SCRIPT)
    latest_json = read(LATEST_JSON)

    assert_contains(download_html, 'href="styles.css"', "stylesheet reference")
    assert_contains(download_html, 'src="download.js?v=', "cache-busted download script reference")
    assert_contains(download_html, 'id="installer-size"', "user-facing installer size display")
    assert_contains(download_html, "assets/images/download/quick-start.webp", "quick start screenshot path")
    assert_contains(download_html, "assets/images/download/topics.webp", "topics screenshot path")
    assert_contains(download_html, "assets/images/download/guided-lesson.webp", "guided lesson screenshot path")
    assert_contains(download_html, "assets/images/download/conversation.webp", "conversation screenshot path")
    assert_contains(download_html, 'class="download-cta-support"', "support email inside download CTA card")
    assert_contains(download_html, 'href="mailto:support@languagevoicetutor.com"', "support mailto link")
    if 'class="download-content-shell"' in download_html or '<section class="support-card" aria-label="Support">' in download_html:
        raise AssertionError("download.html must not include a separate support section below the hero")
    support_index = download_html.index('class="download-cta-support"')
    panel_close_index = download_html.index('</section>', support_index)
    hero_close_index = download_html.index('</section>', panel_close_index + len('</section>'))
    footer_index = download_html.index('<footer class="site-footer">')
    if support_index > panel_close_index or hero_close_index > footer_index:
        raise AssertionError("support email must stay inside the download CTA card and the footer must follow the hero")
    if "Technical release details" in download_html:
        raise AssertionError("download.html must not show a public technical release details section")
    for removed_id in ["detail-installer", "detail-backend-base-url", "detail-minimum-supported-version", "detail-update-mode", "detail-sha"]:
        if removed_id in download_html:
            raise AssertionError(f"download.html must not include removed technical detail element: {removed_id}")
    assert_contains(download_html, "1.0", "manifest version rendered into static download page")
    assert_contains(download_html, "LanguageVoiceTutorSetup-1.0.exe", "manifest installer rendered into static download page")
    if "Version</dt>\n                    <dd id=\"detail-version\">Unavailable</dd>" in download_html:
        raise AssertionError("download.html must not default release details to Version: Unavailable")
    assert_contains(download_js, '"/releases/windows/direct/latest.json"', "release manifest URL")
    assert_contains(download_js, "installerRelativeUrl", "installerRelativeUrl usage")
    assert_contains(download_js, "installerFileName", "installerFileName usage")
    assert_contains(download_js, "installerSizeBytes", "installer size usage")
    assert_contains(download_js, "installerSize", "installer size element usage")
    assert_contains(download_js, "Date.now()", "latest.json cache busting")
    assert_contains(download_js, 'removeAttribute("href")', "disabled button removes href")
    assert_contains(download_js, "setDownloadEnabled(false)", "download disabled before manifest load")
    assert_contains(download_js, "`${releaseBaseUrl}${installerRelativeUrl}`", "download href built from manifest installerRelativeUrl")
    assert_contains(download_js, "Could not load the latest Windows download. Please try again later.", "friendly manifest load failure")
    assert_contains(download_js, "Release details are temporarily unavailable. Please try again later.", "safe invalid manifest failure")
    assert_contains(upload_script, "[switch]$DryRun", "DryRun switch")
    assert_contains(upload_script, "site\\public", "static site source folder")
    assert_contains(upload_script, "Release files: not touched. Backend deployment: not touched.", "deployment scope guard")
    assert_contains(latest_json, '"updateMode": "manual-confirmation"', "current manual confirmation update mode")

    for path in [DOWNLOAD_HTML, DOWNLOAD_JS]:
        assert_no_hardcoded_installer_fallback(path)

    for path in [*SITE_PUBLIC.iterdir(), UPLOAD_SCRIPT]:
        if path.is_file():
            assert_no_sensitive_values(path)

    print("Static public direct download page policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
