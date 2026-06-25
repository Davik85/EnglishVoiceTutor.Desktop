#!/usr/bin/env python3
"""Policy checks for the read-only top-level Admin Website skeleton.

Uses only the Python standard library. The checks intentionally inspect static
Admin files and the current git diff; they do not contact services, enable
billing, or read secrets.
"""

from __future__ import annotations

import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ADMIN_HTML = ROOT / "backend" / "EnglishVoiceTutor.Api" / "wwwroot" / "admin" / "index.html"
ADMIN_STATIC_FILES = (
    ADMIN_HTML,
    ROOT / "backend" / "EnglishVoiceTutor.Api" / "wwwroot" / "admin" / "admin.js",
    ROOT / "backend" / "EnglishVoiceTutor.Api" / "wwwroot" / "admin" / "admin.css",
)

FORBIDDEN_ADMIN_PATTERNS = (
    (re.compile(r"https://checkout\.", re.IGNORECASE), "checkout URL"),
    (re.compile(r"data-paddle", re.IGNORECASE), "Paddle checkout data attribute"),
    (re.compile(r"\bPADDLE_[A-Z0-9_]*\s*=", re.IGNORECASE), "Paddle environment assignment"),
    (re.compile(r"\bsk-(?:live|test|proj|[A-Za-z0-9])[A-Za-z0-9_-]{12,}\b"), "OpenAI-style secret key"),
    (re.compile(r"\bprice_[A-Za-z0-9]{8,}\b"), "Paddle price identifier"),
    (re.compile(r"\bctm_[A-Za-z0-9]{8,}\b"), "Paddle customer identifier"),
    (re.compile(r"\btxn_[A-Za-z0-9]{8,}\b"), "Paddle transaction identifier"),
    (re.compile(r"\bsub_[A-Za-z0-9]{8,}\b"), "Paddle subscription identifier"),
    (re.compile(r"\bJWT_(?:KEY|SECRET|SIGNING_KEY)\b\s*=", re.IGNORECASE), "JWT key assignment"),
    (re.compile(r"\b(?:Server|Host|Data Source|Initial Catalog|User Id|Password)\s*=\s*[^;\n]+;", re.IGNORECASE), "connection-string fragment"),
)


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def changed_files() -> list[str]:
    result = subprocess.run(
        ["git", "diff", "--name-only", "HEAD", "--"],
        cwd=ROOT,
        check=True,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


def test_admin_shell_contains_website_tab_and_status_copy() -> None:
    html = ADMIN_HTML.read_text(encoding="utf-8")
    require(html, 'data-tab-id="website"', "top-level Website Admin tab")
    require(html, 'id="tab-panel-website"', "top-level Website Admin panel")
    if 'data-cms-sub-tab-id="website"' in html or 'id="cms-sub-panel-website"' in html:
        raise AssertionError("Website must not appear as a CMS Content sub-tab or CMS sub-panel.")
    require(html, "Website CMS planning status", "read-only Website status panel")
    require(html, "Current public website source is still static files under", "static site source status")
    require(html, "temporary Paddle review-readiness shells", "temporary Paddle review-readiness status")
    require(html, "Public site rendering is not connected to CMS yet", "no public rendering connection status")
    require(html, "No live Paddle billing is enabled from this tab", "no live Paddle billing status")


def test_admin_shell_contains_required_guardrails() -> None:
    html = ADMIN_HTML.read_text(encoding="utf-8")
    for guardrail in (
        "Do not paste Paddle secrets.",
        "Do not paste webhook secrets.",
        "Do not paste API keys.",
        "Do not paste JWT keys.",
        "Do not paste raw provider payloads.",
        "Do not paste customer IDs, transaction IDs, or subscription IDs into public copy.",
    ):
        require(html, guardrail, f"guardrail wording {guardrail}")


def test_no_site_public_files_changed_in_current_diff() -> None:
    changed_site_public = [name for name in changed_files() if name.startswith("site/public/")]
    if changed_site_public:
        raise AssertionError(f"site/public files must not be changed by this task: {changed_site_public}")


def test_no_checkout_or_secret_like_strings_in_admin_static_files() -> None:
    for path in ADMIN_STATIC_FILES:
        text = path.read_text(encoding="utf-8")
        for pattern, description in FORBIDDEN_ADMIN_PATTERNS:
            match = pattern.search(text)
            if match:
                raise AssertionError(
                    f"{path.relative_to(ROOT)} contains forbidden {description}: {match.group(0)!r}"
                )


if __name__ == "__main__":
    test_admin_shell_contains_website_tab_and_status_copy()
    test_admin_shell_contains_required_guardrails()
    test_no_site_public_files_changed_in_current_diff()
    test_no_checkout_or_secret_like_strings_in_admin_static_files()
    print("Admin Website CMS skeleton policy checks passed.")
