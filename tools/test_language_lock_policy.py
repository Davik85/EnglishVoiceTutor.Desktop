#!/usr/bin/env python3
"""Deterministic checks for target-language tutor output policy."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require(text: str, needle: str, source: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {source}")


def main() -> None:
    prompt = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    chat = read("backend/EnglishVoiceTutor.Api/Services/OpenAiLessonChatService.cs")
    guard = read("backend/EnglishVoiceTutor.Api/Services/AssistantOutputLanguageGuard.cs")
    docs = read("docs/VOICE_AND_REALTIME_REVIEW.md")

    for needle in [
        "TARGET STUDY LANGUAGE:",
        "Target-language lesson language lock",
        "Always speak {targetLanguage.LanguageLockName} in tutor messages.",
        "Do not switch to another language even if the learner asks.",
        "Speak Finnish.",
        "Can you speak Russian?",
        "Puhu suomea.",
        "Translate button",
    ]:
        require(prompt, needle, "backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")

    for language_id in ['"es"', '"fr"', '"de"', '"pt"', '"it"']:
        require(guard, language_id, "backend/EnglishVoiceTutor.Api/Services/AssistantOutputLanguageGuard.cs")

    require(chat, "AssistantOutputLanguageViolation", "normal chat language violation logging")
    require(chat, "CreateSafeTargetLanguageFallback", "normal chat safe target-language fallback")
    require(guard, "Practiquemos en español", "Spanish safe response")
    require(guard, "Pratiquons en français", "French safe response")
    require(guard, "Üben wir auf Deutsch", "German safe response")
    require(docs, "Translate button remains a separate review feature", "translation remains separate documentation")

    lesson_json = "\n".join(path.read_text(encoding="utf-8") for path in (ROOT / "Content" / "Lessons").rglob("*.json"))
    if "Target-language lesson language lock" in lesson_json:
        raise AssertionError("Lesson JSON must not duplicate language-lock prompt policy.")

    print("Language lock policy checks passed.")


if __name__ == "__main__":
    main()
