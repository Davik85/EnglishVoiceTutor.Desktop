#!/usr/bin/env python3
"""Deterministic checks for multilingual prompt request wiring."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require_text(text: str, needle: str, source: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {source}")


def main() -> None:
    prompt = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    desktop_request = read("Models/LessonChatBackendRequest.cs")
    api_request = read("backend/EnglishVoiceTutor.Api/Models/LessonChatRequest.cs")
    vm = read("ViewModels/LessonChatViewModel.cs")
    summary = read("Models/LessonSummaryInput.cs")

    for source_name, source in [("desktop", desktop_request), ("api", api_request), ("summary", summary)]:
        for needle in ["TargetLanguageId", "TargetLanguageName", "TargetLanguageNativeName", "TargetLanguageCode"]:
            require_text(source, needle, source_name)

    for needle in [
        "TARGET STUDY LANGUAGE:",
        "All tutor-facing lesson content must be in",
        "Use {targetLanguage.TutorInstructionName} for tutor replies, roleplay, hints, feedback, corrections, examples, and summary.",
        "Do not switch to another language even if the learner asks.",
        "lesson JSON scenario text is a semantic plan",
        "AppendTargetLanguageLock",
    ]:
        require_text(prompt, needle, "backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")

    for needle in ["TargetLanguageId = studyLanguage.Id", "TargetLanguageName = studyLanguage.EnglishName", "BuildConversationModeTtsInstructions"]:
        require_text(vm, needle, "ViewModels/LessonChatViewModel.cs")

    print("Multilingual prompt policy checks passed.")


if __name__ == "__main__":
    main()
