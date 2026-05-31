#!/usr/bin/env python3
"""Deterministic checks for neutral user-facing topic display names."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LESSONS_ROOT = ROOT / "Content" / "Lessons"
EVERYDAY_ENGLISH_ROOT = LESSONS_ROOT / "EverydayEnglish"


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def require_text(text: str, needle: str, source: str) -> None:
    require(needle in text, f"Missing {needle!r} in {source}")


def require_absent(text: str, needle: str, source: str) -> None:
    require(needle not in text, f"Unexpected {needle!r} in {source}")


def main() -> None:
    home_vm = read("ViewModels/HomeViewModel.cs")
    localization = read("Localization/AppLocalization.cs")
    prompt_builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")

    require_text(home_vm, 'new Topic(1, "Daily Life", "Small talk, introductions, and daily situations.")', "ViewModels/HomeViewModel.cs")
    require_absent(home_vm, 'new Topic(1, "Everyday English"', "ViewModels/HomeViewModel.cs")
    require_text(localization, '("Daily Life", l("Daily Life"), l("Small talk, introductions, and daily situations."))', "Localization/AppLocalization.cs")
    require_absent(localization, '("Everyday English",', "Localization/AppLocalization.cs")
    require_text(prompt_builder, "ChooseFirstNonEmpty(request.TopicTitle, request.Topic)", "backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")

    introductions = json.loads((EVERYDAY_ENGLISH_ROOT / "introductions.json").read_text(encoding="utf-8"))
    require(introductions["id"] == "everyday_english_introductions", "Internal introductions lesson ID must remain stable.")
    require(introductions["metadata"]["topic"] == "Daily Life", "Introductions display topic must be Daily Life.")

    lesson_files = list(LESSONS_ROOT.rglob("*.json"))
    require(len(lesson_files) == 26, f"Expected 26 lesson JSON files, found {len(lesson_files)}")
    require(EVERYDAY_ENGLISH_ROOT.is_dir(), "Legacy EverydayEnglish lesson folder must remain for compatibility.")
    daily_life_dirs = [path for path in LESSONS_ROOT.iterdir() if path.is_dir() and path.name == "DailyLife"]
    require(not daily_life_dirs, f"Lesson JSON folder must not be duplicated or renamed to DailyLife: {daily_life_dirs}")

    for lesson_file in EVERYDAY_ENGLISH_ROOT.glob("*.json"):
        lesson = json.loads(lesson_file.read_text(encoding="utf-8"))
        require(lesson["metadata"]["topic"] == "Daily Life", f"{lesson_file} must use Daily Life as display topic.")
        require(lesson["id"].startswith("everyday_english_"), f"{lesson_file} internal lesson ID must remain stable.")

    print("Topic display name policy checks passed.")


if __name__ == "__main__":
    main()
