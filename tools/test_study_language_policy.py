#!/usr/bin/env python3
"""Deterministic checks for study-language and interface-language configuration."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXPECTED_STUDY_LANGUAGE_IDS = ["en", "fr", "de", "pt", "es", "it"]
EXPECTED_RELEASE_READY_INTERFACE_LANGUAGE_IDS = [
    "en",
    "es",
    "fr",
    "de",
    "it",
    "pt",
    "ru",
    "pl",
    "ar",
    "ja",
    "ko",
    "sr",
    "hr",
    "bg",
]
CORE_UI_TERM_NAMES = [
    "Settings",
    "Learning",
    "Account",
    "Audio",
    "Progress",
    "Diagnostics",
    "Save",
    "Back",
    "Retry",
    "Loading",
    "Error",
    "StudyLanguage",
    "NativeLanguage",
    "InterfaceLanguage",
    "TutorAvatar",
    "LessonChat",
    "Topic",
    "Situation",
    "Level",
    "ConversationMode",
    "Send",
    "StartRecording",
    "StopRecording",
    "Hint",
    "Translation",
    "ShowTranslation",
    "HideTranslation",
    "PlayVoice",
    "FinishLesson",
    "Summary",
    "WhatWentWell",
    "WhatToImprove",
    "UsefulPhrases",
    "MistakesToReview",
    "NextSteps",
    "BackToLessons",
    "Home",
    "Login",
    "Register",
    "Logout",
    "CurrentAccount",
    "SubscriptionStatus",
    "Microphone",
    "TestMicrophone",
    "RefreshMicrophones",
]

REQUIRED_RELEASE_READY_PHRASES = [
    "SettingsSubtitle",
    "NativeLanguageSubtitle",
    "TutorAvatarSubtitle",
    "SettingsSavedMessage",
    "StudyLanguageSubtitle",
    "HomeSubtitle",
    "DailyLimitText",
    "AutoSendVoiceLabel",
    "AutoSendVoiceToolTip",
    "AutoPlayBotVoiceLabel",
    "AutoPlayBotVoiceToolTip",
]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def require_text(text: str, needle: str, source: str) -> None:
    require(needle in text, f"Missing {needle!r} in {source}")


def parse_interface_release_ready_ids(source: str) -> list[str]:
    match = re.search(r"ReleaseReadyInterfaceLanguageIds\s*=\s*\[(.*?)\];", source, re.S)
    require(match is not None, "ReleaseReadyInterfaceLanguageIds list is missing")
    return re.findall(r'"([^"]+)"|([A-Za-z]+Id)', match.group(1))


def resolve_interface_ids(source: str) -> list[str]:
    constants = dict(re.findall(r'public const string (\w+) = "([^"]+)";', source))
    resolved: list[str] = []
    for literal, constant in parse_interface_release_ready_ids(source):
        resolved.append(literal or constants[constant])
    return resolved


def parse_native_language_ids(source: str) -> list[str]:
    constants = dict(re.findall(r'public const string (\w+) = "([^"]+)";', source))
    ids = []
    for literal, constant in re.findall(r'new\((?:"([^"]+)"|(\w+))', source):
        ids.append(literal or constants[constant])
    return ids


def parse_ui_terms(source: str) -> tuple[list[str], dict[str, list[str]], dict[str, str]]:
    record_match = re.search(r"private sealed record UiTerms\((.*?)\);", source, re.S)
    require(record_match is not None, "UiTerms record is missing")
    term_names = [re.sub(r"\s*string\s+", "", item.strip()) for item in record_match.group(1).split(",")]

    terms_by_variable: dict[str, list[str]] = {}
    for match in re.finditer(r"private static readonly UiTerms (\w+) = new\((.*?)\);", source, re.S):
        values = re.findall(r'"((?:[^"\\]|\\.)*)"', match.group(2))
        terms_by_variable[match.group(1)] = values

    language_to_variable = dict(re.findall(r'\["([^"]+)"\]\s*=\s*(\w+)', source))
    return term_names, terms_by_variable, language_to_variable


def parse_ui_phrases(source: str) -> tuple[list[str], dict[str, list[str]]]:
    record_match = re.search(r"private sealed record UiPhrases\((.*?)\);", source, re.S)
    require(record_match is not None, "UiPhrases record is missing")
    phrase_names = [re.sub(r"\s*string\s+", "", item.strip()) for item in record_match.group(1).split(",")]

    english_match = re.search(r"private static readonly UiPhrases EnglishPhrases = new\((.*?)\);", source, re.S)
    require(english_match is not None, "EnglishPhrases is missing")
    phrases_by_language = {
        "en": re.findall(r'"((?:[^"\\]|\\.)*)"', english_match.group(1))
    }

    dictionary_match = re.search(r"PhrasesByLanguageId = new Dictionary<string, UiPhrases>\(StringComparer.OrdinalIgnoreCase\)\s*\{(.*?)\n\s*\};", source, re.S)
    require(dictionary_match is not None, "PhrasesByLanguageId dictionary is missing")
    for match in re.finditer(r'\["([^"]+)"\]\s*=\s*new\((.*?)\)(?=,?\s*\n|\Z)', dictionary_match.group(1), re.S):
        language_id = match.group(1)
        if language_id == "en":
            continue
        phrases_by_language[language_id] = re.findall(r'"((?:[^"\\]|\\.)*)"', match.group(2))

    return phrase_names, phrases_by_language


def main() -> None:
    config = json.loads(read("Content/StudyLanguages/study_languages.json"))
    ids = [item["id"] for item in config]
    require(ids == EXPECTED_STUDY_LANGUAGE_IDS, f"Unexpected study language ids: {ids}")
    require(sum(1 for item in config if item.get("isDefault")) == 1, "Exactly one default study language is required")
    require(next(item for item in config if item["isDefault"])["id"] == "en", "English must be default")

    catalog = read("Shared/StudyLanguages/StudyLanguageCatalog.cs")
    for needle in ["DefaultStudyLanguageId = \"en\"", "French", "German", "Portuguese", "Spanish", "Italian"]:
        require_text(catalog, needle, "Shared/StudyLanguages/StudyLanguageCatalog.cs")

    interface_options = read("Models/InterfaceLanguageOptions.cs")
    release_ready_ids = resolve_interface_ids(interface_options)
    require(
        release_ready_ids == EXPECTED_RELEASE_READY_INTERFACE_LANGUAGE_IDS,
        f"Unexpected release-ready interface language ids: {release_ready_ids}",
    )
    require_text(interface_options, "public static readonly IReadOnlyList<InterfaceLanguageOption> All = ReleaseReadyInterfaceLanguageIds", "Models/InterfaceLanguageOptions.cs")
    require_text(interface_options, "?? English", "Models/InterfaceLanguageOptions.cs")

    native_catalog = read("Shared/NativeLanguages/NativeLanguageCatalog.cs")
    native_ids = parse_native_language_ids(native_catalog)
    require(len(native_ids) >= 50, f"Native language catalog is no longer broad: {len(native_ids)} languages")
    require(set(release_ready_ids).issubset(native_ids), "Every release-ready interface language must exist in the native catalog")

    app_localization = read("Localization/AppLocalization.cs")
    term_names, terms_by_variable, language_to_variable = parse_ui_terms(app_localization)
    english_values = terms_by_variable["EnglishTerms"]
    term_index = {name: index for index, name in enumerate(term_names)}
    for language_id in release_ready_ids:
        variable = language_to_variable[language_id]
        values = terms_by_variable[variable]
        require(len(values) == len(english_values), f"{language_id} has an incomplete UiTerms value list")
        if language_id == "en":
            continue

        untranslated = [name for name in CORE_UI_TERM_NAMES if values[term_index[name]] == english_values[term_index[name]]]
        require(
            len(untranslated) <= 4,
            f"{language_id} has too many English core UI terms: {untranslated}",
        )

    phrase_names, phrases_by_language = parse_ui_phrases(app_localization)
    phrase_index = {name: index for index, name in enumerate(phrase_names)}
    english_phrases = phrases_by_language["en"]
    require(len(english_phrases) == len(phrase_names), "EnglishPhrases has an incomplete UiPhrases value list")
    for required_phrase in REQUIRED_RELEASE_READY_PHRASES:
        require(required_phrase in phrase_index, f"Required phrase {required_phrase} is missing from UiPhrases")

    for language_id in release_ready_ids:
        require(language_id in phrases_by_language, f"{language_id} is missing from PhrasesByLanguageId")
        phrases = phrases_by_language[language_id]
        require(len(phrases) == len(english_phrases), f"{language_id} has an incomplete UiPhrases value list")
        missing = [name for name in REQUIRED_RELEASE_READY_PHRASES if not phrases[phrase_index[name]].strip()]
        require(not missing, f"{language_id} has blank required UI phrases: {missing}")
        if language_id == "en":
            continue

        untranslated_phrases = [
            name
            for name in REQUIRED_RELEASE_READY_PHRASES
            if phrases[phrase_index[name]] == english_phrases[phrase_index[name]]
        ]
        require(
            not untranslated_phrases,
            f"{language_id} has English fallback text in release-ready UI phrases: {untranslated_phrases}",
        )

    settings_vm = read("ViewModels/SettingsViewModel.cs")
    settings_xaml = read("Views/SettingsView.xaml")
    user_settings = read("Models/UserSettings.cs")
    main_vm = read("ViewModels/MainViewModel.cs")
    for needle in ["AvailableStudyLanguages", "SelectedStudyLanguage", "StudyLanguageTitle", "DiagnosticsStudyLanguageText"]:
        require_text(settings_vm, needle, "ViewModels/SettingsViewModel.cs")
    for needle in ["StudyLanguageTitle", "AvailableStudyLanguages", "SelectedStudyLanguage"]:
        require_text(settings_xaml, needle, "Views/SettingsView.xaml")
    require_text(settings_vm, "AvailableInterfaceLanguages", "ViewModels/SettingsViewModel.cs")
    require_text(settings_vm, "InterfaceLanguageOptions.All", "ViewModels/SettingsViewModel.cs")
    require_text(user_settings, "StudyLanguageId", "Models/UserSettings.cs")
    require_text(main_vm, "userSettings.StudyLanguageId", "ViewModels/MainViewModel.cs")
    require_text(main_vm, "StudyLanguageCatalog.GetById(userSettings.StudyLanguageId)", "ViewModels/MainViewModel.cs")

    lesson_files = list((ROOT / "Content" / "Lessons").rglob("*.json"))
    require(len(lesson_files) == 26, f"Expected 26 lesson JSON files, found {len(lesson_files)}")
    for language_id in ["fr", "de", "pt", "es", "it"]:
        duplicated = [path for path in lesson_files if f"/{language_id}/" in path.as_posix() or path.name.startswith(f"{language_id}_")]
        require(not duplicated, f"Lesson JSON must not be duplicated per language: {duplicated}")

    print("Study language and release-ready interface language policy checks passed.")


if __name__ == "__main__":
    main()
