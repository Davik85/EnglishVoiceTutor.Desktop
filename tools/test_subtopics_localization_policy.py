#!/usr/bin/env python3
"""Deterministic checks for Subtopics/Situations display-layer localization."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXPECTED_INTERFACE_IDS = ["en", "es", "fr", "de", "it", "pt", "ru", "pl", "ar", "ja", "ko", "sr", "hr", "bg"]
EXPECTED_STUDY_IDS = ["en", "fr", "de", "pt", "es", "it"]
DAILY_LIFE_KEYS = [
    "Introductions",
    "Small talk with a neighbor",
    "Asking for help",
    "Making plans",
    "Talking about your day",
]
SPANISH_FALLBACK_TEXTS = {
    "Presentaciones",
    "Preséntate y haz preguntas personales básicas.",
    "Hablar con un vecino",
    "Ten una conversación breve y amable cerca de casa.",
    "Pedir ayuda",
    "Pide ayuda en una situación cotidiana sencilla.",
    "Hacer planes",
    "Planifica una actividad y acuerda hora y lugar.",
    "Hablar de tu día",
    "Describe tu día y tu rutina diaria.",
}
ENGLISH_SUBTITLE = "Choose a realistic situation for your short speaking lesson."
SPANISH_SUBTITLE = "Elige una situación realista para tu lección oral corta."


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def parse_release_ready_ids(interface_options: str) -> list[str]:
    constants = dict(re.findall(r'public const string (\w+) = "([^"]+)"', interface_options))
    match = re.search(r"ReleaseReadyInterfaceLanguageIds\s*=\s*\[(.*?)\];", interface_options, re.S)
    require(match is not None, "ReleaseReadyInterfaceLanguageIds list is missing.")
    ids: list[str] = []
    for literal, constant in re.findall(r'"([^"]+)"|(\w+Id)', match.group(1)):
        ids.append(literal or constants[constant])
    return ids


def parse_screen_text(source: str) -> dict[str, tuple[str, str, str, str]]:
    return {
        language_id: (title, subtitle, free_title, free_subtitle)
        for language_id, title, subtitle, free_title, free_subtitle in re.findall(
            r'\["([^"]+)"\]\s*=\s*new\("([^"]*)",\s*"([^"]*)",\s*"([^"]*)",\s*"([^"]*)"\)',
            source,
        )
    }


def parse_subtopic_blocks(source: str) -> dict[str, str]:
    blocks: dict[str, str] = {}
    for language_id in EXPECTED_INTERFACE_IDS:
        start_match = re.search(rf'\["{re.escape(language_id)}"\]\s*=\s*Map\(', source)
        require(start_match is not None, f"{language_id} subtopic map is missing.")
        start = start_match.end()
        next_match = re.search(r'\n\s*\["[^"]+"\]\s*=\s*Map\(', source[start:])
        end = start + next_match.start() if next_match else source.find("\n        };", start)
        blocks[language_id] = source[start:end]
    return blocks


def parse_entries(block: str) -> dict[str, tuple[str, str]]:
    return {
        key: (title, description)
        for key, title, description in re.findall(r'\("([^"]+)",\s*"([^"]*)",\s*"([^"]*)"\)', block)
    }


def main() -> None:
    interface_options = read("Models/InterfaceLanguageOptions.cs")
    require(parse_release_ready_ids(interface_options) == EXPECTED_INTERFACE_IDS, "Release-ready Interface language list changed.")

    study_languages = json.loads(read("Content/StudyLanguages/study_languages.json"))
    study_ids = [language["id"] for language in study_languages]
    require(study_ids == EXPECTED_STUDY_IDS, f"Study language IDs changed: {study_ids}")

    app_localization = read("Localization/AppLocalization.cs")
    subtopics_localization = read("Localization/SubtopicsLocalization.cs")
    subtopics_vm = read("ViewModels/SubtopicsViewModel.cs")

    require('return InterfaceLanguageOptions.GetById(languageId).Id;' in app_localization, "Interface language normalization must use InterfaceLanguageOptions.")
    require('TextByLanguageId.Value[InterfaceLanguageOptions.EnglishId]' in app_localization, "Unsupported Interface languages must fall back to English.")
    require('SubtopicsLocalization.GetTitleTemplate(languageId)' in app_localization, "Subtopics title must use the display-layer title template.")
    require('SubtopicsLocalization.GetSubtitle(languageId)' in app_localization, "Subtopics subtitle must use display-layer localization.")
    require('SubtopicsLocalization.GetFreeConversationTitle(localizedText.LanguageId)' in subtopics_vm, "Free Conversation title must use SubtopicsLocalization.")
    require('SubtopicsLocalization.GetFreeConversationSubtitle(localizedText.LanguageId)' in subtopics_vm, "Free Conversation subtitle must use SubtopicsLocalization.")

    screen_text = parse_screen_text(subtopics_localization)
    require(set(screen_text) == set(EXPECTED_INTERFACE_IDS), f"Unexpected Subtopics screen languages: {sorted(screen_text)}")
    for language_id in EXPECTED_INTERFACE_IDS:
        title, subtitle, start_free, free_subtitle = screen_text[language_id]
        for value_name, value in [
            ("title template", title),
            ("subtitle", subtitle),
            ("free conversation title", start_free),
            ("free conversation subtitle", free_subtitle),
        ]:
            require(value.strip(), f"{language_id} has blank Subtopics {value_name}.")
        require("{0}" in title, f"{language_id} title template must preserve {{0}} placeholder.")
        if language_id != "en":
            require(" for " not in title.lower(), f"{language_id} title template contains hardcoded English 'for': {title}")
            require(subtitle != ENGLISH_SUBTITLE, f"{language_id} subtitle uses English fallback.")
        if language_id not in {"en", "es"}:
            require(subtitle != SPANISH_SUBTITLE, f"{language_id} subtitle uses Spanish fallback.")

    subtopic_blocks = parse_subtopic_blocks(subtopics_localization)
    spanish_entries = parse_entries(subtopic_blocks["es"])
    for language_id, block in subtopic_blocks.items():
        entries = parse_entries(block)
        require(len(entries) == 26, f"{language_id} must localize all 26 visible Subtopics/Situations; found {len(entries)}.")
        for key in DAILY_LIFE_KEYS:
            require(key in entries, f"{language_id} is missing Daily Life subtopic {key}.")
            title, description = entries[key]
            require(title.strip(), f"{language_id} has blank title for {key}.")
            require(description.strip(), f"{language_id} has blank description for {key}.")
            if language_id not in {"en", "es"}:
                require(title not in SPANISH_FALLBACK_TEXTS, f"{language_id} title for {key} uses Spanish fallback: {title}")
                require(description not in SPANISH_FALLBACK_TEXTS, f"{language_id} description for {key} uses Spanish fallback: {description}")
                require((title, description) != spanish_entries[key], f"{language_id} {key} matches Spanish fallback pair.")

    for required_text in ["StartLessonButtonText", "BackButtonText", "CurrentLevelText", "TopicText"]:
        require(required_text in subtopics_vm, f"SubtopicsViewModel is missing {required_text}.")

    print("Subtopics localization policy checks passed.")


if __name__ == "__main__":
    main()
