#!/usr/bin/env python3
"""Deterministic checks for English-only tutor output policy."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require(text: str, needle: str, source: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {source}")


def main() -> None:
    prompt = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    realtime = read("backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs")
    chat = read("backend/EnglishVoiceTutor.Api/Services/OpenAiLessonChatService.cs")
    guard = read("backend/EnglishVoiceTutor.Api/Services/AssistantOutputLanguageGuard.cs")
    docs = read("docs/VOICE_AND_REALTIME_REVIEW.md")

    for needle in [
        "English-only lesson language lock",
        "Always speak English in tutor messages.",
        "Do not switch to another language even if the learner asks.",
        "Speak Finnish.",
        "Can you speak Russian?",
        "Puhu suomea.",
        "Говори по-русски.",
        "Finnish, Russian, Spanish",
        "Translate button",
    ]:
        require(prompt, needle, "backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")

    require(prompt, "AppendCanonicalTeachingPolicy(prompt, chatRequest, avatarProfile, RealtimeVoiceMode)", "realtime prompt uses canonical policy")
    require(realtime, "lessonPromptBuilder.BuildRealtimeInstructions", "realtime instructions use shared prompt")
    require(realtime, "RealtimeAssistantLanguageViolation", "realtime language violation logging")
    require(realtime, "BuildCorrectiveEnglishOnlyInstructions", "realtime corrective language instruction")
    require(chat, "AssistantOutputLanguageViolation", "normal chat language violation logging")
    require(chat, "AssistantOutputLanguageGuard.CreateSafeEnglishFallback", "normal chat safe fallback")
    require(guard, "Let's practice in English. What's your name?", "Speak Finnish safe A1 response")
    require(guard, "Let's keep this lesson in English. I can help you with this situation in English.", "B1/B2 safe response")
    require(docs, "Translate button remains a separate review feature", "translation remains separate documentation")

    lesson_json = "\n".join(path.read_text(encoding="utf-8") for path in (ROOT / "Content" / "Lessons").rglob("*.json"))
    if "English-only lesson language lock" in lesson_json:
        raise AssertionError("Lesson JSON must not duplicate language-lock prompt policy.")

    print("Language lock policy checks passed.")


if __name__ == "__main__":
    main()
