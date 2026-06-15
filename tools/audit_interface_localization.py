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

RU_SPANISH_FRAGMENTS = [
    "Conexión", "Endpoint del servidor", "Usada por", "Perfil del avatar", "Predeterminado del sistema",
    "Prueba de micrófono", "No se encontró", "Inicia sesión", "Lección gratis", "Cómo funciona",
    "Elige", "Practica", "Recibe", "Versión MVP", "Usaremos", "Selecciona", "Volver al chat",
    "Haz clic", "Versión corregida", "Consejo", "Traduciendo", "No se pudo", "Comprueba tu conexión", "Presentaciones", "Preséntate", "Charla con un vecino", "Pedir ayuda",
    "Hacer planes", "Hablar de tu día", "Facturación en aeropuerto", "Registro en hotel", "Equipaje perdido",
    "Completaste", "Sigue practicando",
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


def main() -> None:
    app = APP.read_text(encoding="utf-8")
    language_ids = parse_release_languages(INTERFACE.read_text(encoding="utf-8"))
    expected = ["en", "es", "fr", "de", "it", "pt", "ru", "pl", "ar", "ja", "ko", "sr", "hr", "bg"]
    if language_ids != expected:
        raise SystemExit(f"Unexpected release-ready interface languages: {language_ids}")

    used_keys = set(re.findall(r'l\("((?:[^"\\]|\\.)*)"\)', app)) | EXPECTED_UPDATE_KEYS
    blocks = parse_blocks(app)
    for lang in language_ids:
        if lang == "en":
            continue
        if lang not in blocks:
            raise SystemExit(f"{lang} is missing learner UI dictionary")
        missing = sorted(used_keys - set(blocks[lang]))
        if missing:
            raise SystemExit(f"{lang} is missing learner UI keys: {missing[:10]}")
        english_fallback = sorted(k for k in used_keys if blocks[lang].get(k) == k)
        if english_fallback:
            raise SystemExit(f"{lang} still uses English fallback values: {english_fallback[:10]}")

    ru_values = "\n".join(blocks["ru"].values())
    contaminated = [fragment for fragment in RU_SPANISH_FRAGMENTS if fragment in ru_values]
    if contaminated:
        raise SystemExit(f"Russian UI dictionary contains Spanish fragments: {contaminated}")

    print("Interface localization audit passed.")

if __name__ == "__main__":
    main()
