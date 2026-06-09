#!/usr/bin/env python3
"""Policy checks for the static tester download page foundation."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
SITE_PUBLIC = ROOT / "site" / "public"
INDEX = SITE_PUBLIC / "index.html"
DOWNLOAD_JS = SITE_PUBLIC / "download.js"
STYLES = SITE_PUBLIC / "styles.css"
UPLOAD_SCRIPT = ROOT / "scripts" / "upload-static-site.ps1"

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


def main() -> int:
    for path in [INDEX, DOWNLOAD_JS, STYLES, UPLOAD_SCRIPT]:
        if not path.exists():
            raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")

    index = read(INDEX)
    download_js = read(DOWNLOAD_JS)
    upload_script = read(UPLOAD_SCRIPT)

    assert_contains(index, 'href="styles.css"', "stylesheet reference")
    assert_contains(index, 'src="download.js"', "download script reference")
    assert_contains(download_js, '"/releases/windows/direct/latest.json"', "release manifest URL")
    assert_contains(download_js, "installerRelativeUrl", "installerRelativeUrl usage")
    assert_contains(upload_script, "[switch]$DryRun", "DryRun switch")
    assert_contains(upload_script, "site\\public", "static site source folder")
    assert_contains(upload_script, "Release files: not touched. Backend deployment: not touched.", "deployment scope guard")

    for path in [*SITE_PUBLIC.iterdir(), UPLOAD_SCRIPT]:
        if path.is_file():
            assert_no_sensitive_values(path)

    print("Static tester download page policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
