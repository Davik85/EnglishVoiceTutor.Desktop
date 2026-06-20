#!/usr/bin/env python3
"""Targeted policy checks for Desktop billing/account subscription UI localization."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "Localization" / "AppLocalization.cs"
INTERFACE = ROOT / "Models" / "InterfaceLanguageOptions.cs"

BILLING_KEYS = [
    "Buy Premium",
    "Cancel subscription",
    "Refresh status",
    "Opening checkout...",
    "Checkout opened in your browser. After payment, return here and refresh your account status.",
    "Premium purchase is not available yet.",
    "Could not open Premium checkout. Please try again later.",
    "Cancel renewal?",
    "Canceling renewal stops future charges. Your paid Premium access remains until the end of the current paid period.",
    "Keep subscription",
    "Renewal canceled. Paid access remains until the end of the current paid period.",
    "Renewal canceled. Paid access remains until {0}.",
    "Renewal is already canceled.",
    "Renewal is already canceled. Premium access remains until {0}.",
    "Renewal is already canceled. Paid Premium access remains until the end of the current paid period.",
    "No active paid subscription was found.",
    "No paid subscription to cancel.",
    "Could not cancel the subscription. Please try again later.",
    "Free lessons: no daily limit",
    "Free lessons remaining today: {0}",
    "Active",
    "Not active",
    "Active until",
    "Used",
    "On",
    "Off",
    "authenticated",
    "Current access: {0}",
    "Trial Premium",
    "Paid Premium",
    "Admin Premium",
    "Development Premium",
    "Premium",
    "Free",
    "Trial active until: {0}",
    "Paid Premium starts: {0}",
    "Paid Premium access until: {0}",
]

EXPECTED_LANGUAGES = ["en", "es", "fr", "de", "it", "pt", "ru", "pl", "ar", "ja", "ko", "sr", "hr", "bg"]
RU_SPANISH_FRAGMENTS = ["Activo", "No activo", "autenticado", "suscripción"]
PL_ENGLISH_FRAGMENTS = [
    "No active paid subscription",
    "Cancel subscription",
    "Refresh status",
    "Opening checkout",
]


def parse_release_languages(text: str) -> list[str]:
    constants = dict(re.findall(r'public const string (\w+) = "([^"]+)"', text))
    match = re.search(r"ReleaseReadyInterfaceLanguageIds\s*=\s*\[(.*?)\];", text, re.S)
    if not match:
        raise AssertionError("ReleaseReadyInterfaceLanguageIds list is missing")
    ids: list[str] = []
    for literal, const_name in re.findall(r'"([^"]+)"|(\w+Id)', match.group(1)):
        ids.append(literal or constants[const_name])
    return ids


def parse_billing_blocks(text: str) -> dict[str, dict[str, str]]:
    match = re.search(
        r"private static void AddBillingLearnerUiText.*?var billing = new Dictionary<string, Dictionary<string, string>>.*?\{(.*?)\n\s*\};",
        text,
        re.S,
    )
    if not match:
        raise AssertionError("Billing localization block is missing")

    blocks: dict[str, dict[str, str]] = {}
    for lang_match in re.finditer(r'\["([^"]+)"\]\s*=\s*new\(StringComparer\.OrdinalIgnoreCase\)\s*\{(.*?)\n\s*\},', match.group(1), re.S):
        entries = dict(re.findall(r'\["((?:[^"\\]|\\.)*)"\]\s*=\s*"((?:[^"\\]|\\.)*)",', lang_match.group(2)))
        blocks[lang_match.group(1)] = entries
    return blocks


def assert_no_fragments(language: str, values: list[str], fragments: list[str]) -> None:
    joined = "\n".join(values)
    found = [fragment for fragment in fragments if fragment in joined]
    if found:
        raise AssertionError(f"{language} billing localization contains wrong-language fragments: {found}")


def main() -> None:
    app = APP.read_text(encoding="utf-8")
    languages = parse_release_languages(INTERFACE.read_text(encoding="utf-8"))
    if languages != EXPECTED_LANGUAGES:
        raise AssertionError(f"Unexpected release-ready interface languages: {languages}")

    blocks = parse_billing_blocks(app)
    for language in EXPECTED_LANGUAGES:
        if language not in blocks:
            raise AssertionError(f"{language} is missing billing localization entries")
        missing = sorted(set(BILLING_KEYS) - set(blocks[language]))
        if missing:
            raise AssertionError(f"{language} is missing billing localization keys: {missing}")

    english = blocks["en"]
    non_english = [key for key in BILLING_KEYS if english.get(key) != key]
    if non_english:
        raise AssertionError(f"English billing values must remain English fallback keys: {non_english}")

    assert_no_fragments("Russian", list(blocks["ru"].values()), RU_SPANISH_FRAGMENTS)
    assert_no_fragments("Polish", list(blocks["pl"].values()), PL_ENGLISH_FRAGMENTS)
    print("Billing localization policy passed.")


if __name__ == "__main__":
    main()
