#!/usr/bin/env python3
"""Deterministic checks for study-language configuration and settings wiring."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def require_text(text: str, needle: str, source: str) -> None:
    require(needle in text, f"Missing {needle!r} in {source}")


def main() -> None:
    config = json.loads(read("Content/StudyLanguages/study_languages.json"))
    ids = {item["id"] for item in config}
    require(ids == {"en", "fr", "de", "pt", "es", "it"}, f"Unexpected study language ids: {ids}")
    require(sum(1 for item in config if item.get("isDefault")) == 1, "Exactly one default study language is required")
    require(next(item for item in config if item["isDefault"])["id"] == "en", "English must be default")

    catalog = read("Shared/StudyLanguages/StudyLanguageCatalog.cs")
    for needle in ["DefaultStudyLanguageId = \"en\"", "French", "German", "Portuguese", "Spanish", "Italian"]:
        require_text(catalog, needle, "Shared/StudyLanguages/StudyLanguageCatalog.cs")

    settings_vm = read("ViewModels/SettingsViewModel.cs")
    settings_xaml = read("Views/SettingsView.xaml")
    user_settings = read("Models/UserSettings.cs")
    main_vm = read("ViewModels/MainViewModel.cs")
    for needle in ["AvailableStudyLanguages", "SelectedStudyLanguage", "StudyLanguageTitle", "DiagnosticsStudyLanguageText"]:
        require_text(settings_vm, needle, "ViewModels/SettingsViewModel.cs")
    for needle in ["StudyLanguageTitle", "AvailableStudyLanguages", "SelectedStudyLanguage"]:
        require_text(settings_xaml, needle, "Views/SettingsView.xaml")
    require_text(user_settings, "StudyLanguageId", "Models/UserSettings.cs")
    require_text(main_vm, "userSettings.StudyLanguageId", "ViewModels/MainViewModel.cs")
    require_text(main_vm, "StudyLanguageCatalog.GetById(userSettings.StudyLanguageId)", "ViewModels/MainViewModel.cs")

    lesson_files = list((ROOT / "Content" / "Lessons").rglob("*.json"))
    require(len(lesson_files) == 26, f"Expected 26 lesson JSON files, found {len(lesson_files)}")
    for language_id in ["fr", "de", "pt", "es", "it"]:
        duplicated = [path for path in lesson_files if f"/{language_id}/" in path.as_posix() or path.name.startswith(f"{language_id}_")]
        require(not duplicated, f"Lesson JSON must not be duplicated per language: {duplicated}")

    print("Study language policy checks passed.")


if __name__ == "__main__":
    main()
