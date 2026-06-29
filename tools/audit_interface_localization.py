#!/usr/bin/env python3
"""Deterministic desktop interface localization audit for release-ready languages."""
from __future__ import annotations
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "Localization" / "AppLocalization.cs"
INTERFACE = ROOT / "Models" / "InterfaceLanguageOptions.cs"

EXPECTED_UPDATE_KEYS = {
    "Checking for updates...",
    "Check for updates",
    "You are using the latest version. Current: {0}. Latest: {1}.",
    "This app version is newer than the public update manifest.",
    "A new version of Language Voice Tutor is available. Do you want to download and install it now?\\n\\nCurrent version: {0}\\nLatest version: {1}",
    "Update available",
    "Could not check for updates right now. Please check your internet connection and try again.",
    "The update could not be downloaded or verified. Please try again later.",
    "The update was downloaded and verified. Language Voice Tutor will close and restart during installation. Do you want to start the installer now?",
    "Start installer?",
    "App updates",
    "A new version of Language Voice Tutor is available. Do you want to download and install it now?",
    "Please finish your current lesson before starting the installer.",
    "The verified installer could not be found. Please check for updates again.",
    "The installer could not be started. Please try again, or restart the app and check for updates again.",
}

APPROVED_IDENTICAL_TRANSLATIONS = {
    # Some approved UI translations are intentionally identical to English cognates or product wording.
    "fr": {"Contacts"},
    "de": {"Website"},
}

REQUIRED_NORMAL_UI_KEYS = {
    "Forgot password?",
    "Change password",
    "Email",
    "Password",
    "Display name",
    "Account",
    "Current account",
    "Settings source",
    "Subscription status",
    "Plan",
    "Premium",
    "Trial",
    "Free lesson today",
    "Enforcement",
    "Source",
    "Checked",
    "Tutor voice",
    "Choose the voice used for normal lesson playback and Conversation Mode TTS.",
    "Avatar profile",
    "Age",
    "Location",
    "Role",
    "Interests",
    "Personality",
    "Speaking style",
}

# This audit is deterministic and intentionally limited: it checks exact keys,
# exact English fallback values, and known contamination fragments from screenshots.
# It is not full language detection, so product names, URLs, versions, emails,
# and backend/API identifiers are not treated as failures by themselves.
RU_SPANISH_FRAGMENTS = [
    "Conexión", "Endpoint del servidor", "Usada por", "Perfil del avatar", "Predeterminado del sistema",
    "Prueba de micrófono", "No se encontró", "Inicia sesión", "Lección gratis", "Cómo funciona",
    "Elige", "Practica", "Recibe", "Versión MVP", "Usaremos", "Selecciona", "Volver al chat",
    "Haz clic", "Versión corregida", "Consejo", "Traduciendo", "No se pudo", "Comprueba tu conexión", "Presentaciones", "Preséntate", "Charla con un vecino", "Pedir ayuda",
    "Hacer planes", "Hablar de tu día", "Facturación en aeropuerto", "Registro en hotel", "Equipaje perdido",
    "Completaste", "Sigue practicando",
]

AR_SPANISH_FRAGMENTS = [
    "Inicia sesión", "Perfil del avatar", "Ubicación", "Rol", "Intereses",
    "Personalidad", "Estilo al hablar", "Comprobado", "Lección gratis hoy", "Origen", "Conexión", "Predeterminado del sistema", "Prueba de micrófono",
]

SR_CONTAMINATION_FRAGMENTS = [
    "Inicia sesión", "Lección gratis hoy", "Comprobado", "Origen", "Conexión", "Predeterminado del sistema", "Prueba de micrófono",
    "الحساب", "إعدادات", "البريد الإلكتروني",
]


def parse_release_languages(text: str) -> list[str]:
    constants = dict(re.findall(r'public const string (\w+) = "([^"]+)"', text))
    match = re.search(r"ReleaseReadyInterfaceLanguageIds\s*=\s*\[(.*?)\];", text, re.S)
    if not match:
        raise SystemExit("ReleaseReadyInterfaceLanguageIds list is missing")
    ids: list[str] = []
    for literal, const_name in re.findall(r'"([^"]+)"|(\w+Id)', match.group(1)):
        ids.append(literal or constants[const_name])
    return ids


def parse_blocks(text: str) -> dict[str, dict[str, str]]:
    blocks: dict[str, dict[str, str]] = {}
    for match in re.finditer(r'\["([^"]+)"\]\s*=\s*new Dictionary<string, string>\(StringComparer\.OrdinalIgnoreCase\)\s*\{(.*?)\n\s*\},', text, re.S):
        entries = dict(re.findall(r'\["((?:[^"\\]|\\.)*)"\]\s*=\s*"((?:[^"\\]|\\.)*)",', match.group(2)))
        blocks[match.group(1)] = entries
    return blocks


def parse_terms(text: str) -> dict[str, list[str]]:
    terms: dict[str, list[str]] = {}
    for match in re.finditer(r'private static readonly UiTerms (\w+)Terms = new\((.*?)\);', text, re.S):
        values = re.findall(r'"((?:[^"\\]|\\.)*)"', match.group(2))
        terms[match.group(1)] = values
    return terms


def main() -> None:
    app = APP.read_text(encoding="utf-8")
    language_ids = parse_release_languages(INTERFACE.read_text(encoding="utf-8"))
    expected = ["en", "es", "fr", "de", "it", "pt", "ru", "pl", "ar", "ja", "ko", "sr", "hr", "bg"]
    if language_ids != expected:
        raise SystemExit(f"Unexpected release-ready interface languages: {language_ids}")

    used_keys = set(re.findall(r'l\("((?:[^"\\]|\\.)*)"\)', app)) | EXPECTED_UPDATE_KEYS
    required_dictionary_keys = REQUIRED_NORMAL_UI_KEYS & used_keys
    blocks = parse_blocks(app)
    terms = parse_terms(app)
    for lang in language_ids:
        if lang == "en":
            continue
        if lang not in blocks:
            raise SystemExit(f"{lang} is missing learner UI dictionary")
        missing = sorted(used_keys - set(blocks[lang]))
        if missing:
            raise SystemExit(f"{lang} is missing learner UI keys: {missing[:10]}")
        missing_required = sorted(required_dictionary_keys - set(blocks[lang]))
        if missing_required:
            raise SystemExit(f"{lang} is missing required normal UI keys: {missing_required}")
        approved_identical = APPROVED_IDENTICAL_TRANSLATIONS.get(lang, set())
        english_fallback = sorted(k for k in used_keys if blocks[lang].get(k) == k and k not in approved_identical)
        if english_fallback:
            raise SystemExit(f"{lang} still uses English fallback values: {english_fallback[:10]}")

    required_term_indexes = {
        "Account": 2,
        "Premium": 48,
        "Login": 52,
        "Register": 53,
        "Logout": 54,
        "Email": 55,
        "Password": 56,
        "Display name": 57,
        "Current account": 58,
        "Settings source": 59,
        "Subscription status": 60,
        "Plan": 61,
        "Trial": 62,
    }
    for lang in language_ids:
        if lang == "en":
            continue
        values = terms.get(lang)
        if values is None:
            raise SystemExit(f"{lang} is missing UiTerms")
        english_terms = [key for key, index in required_term_indexes.items() if len(values) <= index or values[index] == key]
        if english_terms:
            raise SystemExit(f"{lang} still uses English UiTerms values: {english_terms}")

    ru_values = "\n".join(blocks["ru"].values())
    contaminated = [fragment for fragment in RU_SPANISH_FRAGMENTS if fragment in ru_values]
    if contaminated:
        raise SystemExit(f"Russian UI dictionary contains Spanish fragments: {contaminated}")

    ar_values = "\n".join(blocks["ar"].values())
    ar_contaminated = [fragment for fragment in AR_SPANISH_FRAGMENTS if fragment in ar_values]
    if ar_contaminated:
        raise SystemExit(f"Arabic UI dictionary contains Spanish fragments: {ar_contaminated}")

    sr_values = "\n".join(blocks["sr"].values())
    sr_contaminated = [fragment for fragment in SR_CONTAMINATION_FRAGMENTS if fragment in sr_values]
    if sr_contaminated:
        raise SystemExit(f"Serbian UI dictionary contains Spanish or Arabic fragments: {sr_contaminated}")

    print("Interface localization audit passed.")

if __name__ == "__main__":
    main()
