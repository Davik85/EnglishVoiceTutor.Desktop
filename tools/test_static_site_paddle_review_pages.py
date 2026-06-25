#!/usr/bin/env python3
"""Smoke-test Paddle review-readiness static website pages.

This script intentionally uses only the Python standard library. It inspects the
checked-in static site files and does not contact external services, enable
Paddle, or require secrets.
"""

from __future__ import annotations

import re
import sys
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import urlparse

REPO_ROOT = Path(__file__).resolve().parents[1]
SITE_PUBLIC = REPO_ROOT / "site" / "public"

REQUIRED_PAGES = (
    "pricing.html",
    "terms.html",
    "privacy.html",
    "refunds.html",
    "cancellation.html",
    "support.html",
)

INDEX_REQUIRED_LINKS = REQUIRED_PAGES
DOWNLOAD_REQUIRED_LINKS = (
    "privacy.html",
    "terms.html",
    "refunds.html",
    "cancellation.html",
    "support.html",
)

REQUIRED_PLACEHOLDERS = {
    "pricing.html": "<PREMIUM_PRICE_AND_BILLING_PERIOD>",
    "terms.html": "<LEGAL_SELLER_NAME>",
    "support.html": "<SUPPORT_PHONE_OR_OWNER_DECISION>",
}

# Patterns intentionally target obvious secrets/identifiers and production
# activation claims while allowing owner-review placeholders such as
# <LEGAL_SELLER_NAME> and wording that explicitly says production billing is not
# enabled.
FORBIDDEN_HTML_PATTERNS = (
    (re.compile(r"https://checkout\.", re.IGNORECASE), "live checkout URL"),
    (re.compile(r"data-paddle", re.IGNORECASE), "Paddle checkout data attribute"),
    (re.compile(r"\bPADDLE_[A-Z0-9_]*\s*=", re.IGNORECASE), "Paddle environment assignment"),
    (re.compile(r"\bsk-(?:live|test|proj|[A-Za-z0-9])[A-Za-z0-9_-]{12,}\b"), "OpenAI-style secret key"),
    (re.compile(r"\bprice_[A-Za-z0-9]{8,}\b"), "Paddle price identifier"),
    (re.compile(r"\bctm_[A-Za-z0-9]{8,}\b"), "Paddle customer identifier"),
    (re.compile(r"\btxn_[A-Za-z0-9]{8,}\b"), "Paddle transaction identifier"),
    (re.compile(r"\bpaddle-signature\b|\bwebhook[-_ ]?signature\b", re.IGNORECASE), "webhook signature"),
    (re.compile(r"\bJWT_(?:KEY|SECRET|SIGNING_KEY)\b\s*=", re.IGNORECASE), "JWT key assignment"),
    (re.compile(r"\b(?:Server|Host|Data Source|Initial Catalog|User Id|Password)\s*=\s*[^;\n]+;", re.IGNORECASE), "connection-string fragment"),
    (re.compile(r"\bproduction\s+(?:Paddle\s+)?billing\s+is\s+(?:enabled|live|active|ready)\b", re.IGNORECASE), "production Paddle billing enabled claim"),
    (re.compile(r"\blive\s+Paddle\s+(?:billing|payments?)\s+(?:is|are)\s+(?:enabled|active|ready|live)\b", re.IGNORECASE), "live Paddle billing enabled claim"),
    (re.compile(r"\bPaddle\s+live\s+(?:billing|payments?)\s+(?:is|are)\s+(?:enabled|active|ready|live)\b", re.IGNORECASE), "Paddle live billing enabled claim"),
    (re.compile(r"\b(?:Android|iOS)\s+(?:app|apps|version|versions)?\s*(?:is|are)?\s*(?:currently\s+)?available\b", re.IGNORECASE), "mobile app currently available claim"),
    (re.compile(r"\bavailable\s+(?:on|for)\s+(?:Android|iOS)\b", re.IGNORECASE), "mobile app available claim"),
)


class LinkParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__()
        self.hrefs: list[str] = []

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        for name, value in attrs:
            if name.lower() == "href" and value:
                self.hrefs.append(value.strip())


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def html_files() -> list[Path]:
    return sorted(SITE_PUBLIC.glob("*.html"))


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def extract_hrefs(path: Path) -> list[str]:
    parser = LinkParser()
    parser.feed(read(path))
    return parser.hrefs


def normalize_local_html_href(href: str) -> str | None:
    parsed = urlparse(href)
    if parsed.scheme or parsed.netloc:
        return None
    path = parsed.path
    if not path.endswith(".html"):
        return None
    if path.startswith("/") or ".." in Path(path).parts:
        fail(f"{href!r} is not a safe site/public-relative .html href")
    return path


def assert_contains_link(page: str, required_href: str) -> None:
    hrefs = extract_hrefs(SITE_PUBLIC / page)
    normalized = {normalize_local_html_href(href) for href in hrefs}
    if required_href not in normalized:
        fail(f"{page} does not link to {required_href}")


def main() -> None:
    if not SITE_PUBLIC.is_dir():
        fail(f"Missing static site directory: {SITE_PUBLIC}")

    for page in REQUIRED_PAGES:
        if not (SITE_PUBLIC / page).is_file():
            fail(f"Missing required Paddle review page: {page}")

    for href in INDEX_REQUIRED_LINKS:
        assert_contains_link("index.html", href)
    for href in DOWNLOAD_REQUIRED_LINKS:
        assert_contains_link("download.html", href)

    existing_html = {path.name for path in html_files()}
    for path in html_files():
        for href in extract_hrefs(path):
            local_html = normalize_local_html_href(href)
            if local_html and local_html not in existing_html:
                fail(f"{path.relative_to(REPO_ROOT)} links to missing local page {local_html}")

    for page, placeholder in REQUIRED_PLACEHOLDERS.items():
        text = read(SITE_PUBLIC / page)
        if placeholder not in text and placeholder.replace("<", "&lt;").replace(">", "&gt;") not in text:
            fail(f"{page} is missing required placeholder {placeholder}")

    for path in html_files():
        text = read(path)
        for pattern, description in FORBIDDEN_HTML_PATTERNS:
            match = pattern.search(text)
            if match:
                location = path.relative_to(REPO_ROOT)
                fail(f"{location} contains forbidden {description}: {match.group(0)!r}")

    checked_pages = ", ".join(path.name for path in html_files())
    print(f"Static site Paddle review smoke test passed for {len(html_files())} HTML files: {checked_pages}")


if __name__ == "__main__":
    main()
