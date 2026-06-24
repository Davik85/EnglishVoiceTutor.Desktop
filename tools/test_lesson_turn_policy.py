#!/usr/bin/env python3
"""Deterministic checks for transcript validation, turn lifecycle, and realtime gating policy."""
from __future__ import annotations

import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
VALIDATOR = ROOT / "Shared" / "LessonPolicies" / "LessonTranscriptValidator.cs"
TURN_POLICY = ROOT / "Shared" / "LessonPolicies" / "LessonTurnPolicy.cs"
LESSON_VM = ROOT / "ViewModels" / "LessonChatViewModel.cs"
REALTIME_SERVICE = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "RealtimeVoiceSessionService.cs"
PROMPT_BUILDER = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "LessonPromptBuilder.cs"
INTRODUCTIONS_JSON = ROOT / "Content" / "Lessons" / "EverydayEnglish" / "introductions.json"

VOICE_PLACEHOLDER = "[Voice message]"
RETRY_TEXT = "[Voice not recognized. Please try again in English.]"


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains_between(text: str, start_needle: str, end_needle: str, forbidden: str, label: str) -> None:
    start = text.index(start_needle)
    end = text.index(end_needle, start)
    section = text[start:end]
    if forbidden in section:
        raise AssertionError(f"Unexpected {label}: {forbidden}")


def py_validate(transcript: str | None, allows_one_letter_answer: bool = False) -> bool:
    if transcript is None:
        return False
    normalized = " ".join(transcript.strip().split())
    if not normalized or normalized.lower() in {VOICE_PLACEHOLDER.lower(), RETRY_TEXT.lower()}:
        return False
    if not any(ch.isalnum() for ch in normalized):
        return False
    if any("\u0400" <= ch <= "\u052f" or "\u3400" <= ch <= "\u9fff" or "\u3040" <= ch <= "\u30ff" for ch in normalized):
        return False
    letters = [ch for ch in normalized if ch.isalpha()]
    latin = [ch for ch in letters if "A" <= ch <= "Z" or "a" <= ch <= "z" or "\u00c0" <= ch <= "\u024f"]
    english = [ch for ch in letters if "A" <= ch <= "Z" or "a" <= ch <= "z"]
    if not letters or not english:
        return False
    if (len(letters) - len(latin)) / len(letters) > 0.35:
        return False
    compact = "".join(letters)
    if not allows_one_letter_answer and len(compact) < 2:
        return False
    if not allows_one_letter_answer and len(compact) <= 2 and compact.lower() not in {"a", "i", "ok", "no", "yes", "hi"}:
        return False
    return True


def resolve_final(lesson_type: str, level: str, content_final: int | None = None) -> int:
    if content_final and content_final > 0:
        return content_final
    if lesson_type == "free_conversation":
        return 30
    return 15 if level.startswith(("A1", "A2")) else 25


def resolve_wrap(lesson_type: str, level: str, content_wrap: int | None = None) -> int:
    if content_wrap and content_wrap > 0:
        return content_wrap
    if lesson_type == "free_conversation":
        return 25
    return 10 if level.startswith(("A1", "A2")) else 20


def evaluate_turn(phase: str, current_count: int, valid: bool, lesson_type: str = "guided_roleplay", level: str = "A1 Beginner", content_wrap: int | None = None, content_final: int | None = None) -> dict[str, object]:
    final = resolve_final(lesson_type, level, content_final)
    wrap = resolve_wrap(lesson_type, level, content_wrap)
    counted = valid and phase in {"ActiveRoleplay", "WrapUp"}
    after = min(current_count + 1, final) if counted else current_count
    return {
        "counted": counted,
        "before": current_count,
        "after": after,
        "wrap": wrap,
        "final": final,
        "wrapping": counted and wrap <= after < final,
        "final_message": counted and after >= final,
    }


def main() -> int:
    validator = read(VALIDATOR)
    turn_policy = read(TURN_POLICY)
    vm = read(LESSON_VM)
    realtime = read(REALTIME_SERVICE)
    prompt = read(PROMPT_BUILDER)
    introductions = json.loads(read(INTRODUCTIONS_JSON))

    for sample in ["David.", "My name is David.", "Russia.", "I am from Russia.", "Moscow.", "I live in Moscow.", "Yes.", "No.", "Good.", "I work.", "I study."]:
        if not py_validate(sample):
            raise AssertionError(f"Expected valid transcript: {sample}")

    for sample in ["", " ", VOICE_PLACEHOLDER, RETRY_TEXT, "记", "Лонг", "да", "привет"]:
        if py_validate(sample):
            raise AssertionError(f"Expected invalid transcript: {sample!r}")

    invalid_turn = evaluate_turn("ActiveRoleplay", 3, False)
    if invalid_turn["after"] != 3 or invalid_turn["counted"]:
        raise AssertionError("Invalid transcript changed learner turn count.")

    valid_turn = evaluate_turn("ActiveRoleplay", 3, True)
    if valid_turn["after"] != 4 or not valid_turn["counted"]:
        raise AssertionError("Valid transcript did not increment exactly once.")

    before_wrap = evaluate_turn("ActiveRoleplay", 8, True)
    if before_wrap["wrapping"]:
        raise AssertionError("Phase before wrap threshold must remain ActiveRoleplay.")

    if not evaluate_turn("ActiveRoleplay", 9, True)["wrapping"]:
        raise AssertionError("A1/A2 guided lesson enters WrapUp at turn 10.")

    continued_wrap = evaluate_turn("WrapUp", 10, True)
    if continued_wrap["after"] != 11 or not continued_wrap["wrapping"]:
        raise AssertionError("Wrap-up turns must continue counting without re-entering setup.")

    final_a1 = evaluate_turn("ActiveRoleplay", 14, True)
    if final_a1["final"] != 15 or not final_a1["final_message"]:
        raise AssertionError("A1/A2 final message is not at turn 15.")

    if resolve_final("guided_roleplay", "B1 Intermediate") != 25:
        raise AssertionError("B1/B2 guided lesson final turn should be 25.")

    if resolve_final("free_conversation", "A1 Beginner") != 30:
        raise AssertionError("Free Conversation final turn should be 30.")

    setup_turn = evaluate_turn("SetupContextSelection", 0, True)
    if setup_turn["counted"] or setup_turn["after"] != 0:
        raise AssertionError("Setup/context selection turns must not count.")

    assert_contains(validator, "InvalidTranscriptUserMessage", "shared retry text")
    assert_contains(turn_policy, "LessonTurnPolicy", "shared turn policy")
    assert_contains(vm, "LessonTranscriptValidator.Validate(transcriptionText)", "Lesson Chat voice validation")
    assert_contains(vm, "SendLessonMessageAsync(trimmedTranscriptionText, ChatMessageSource.LessonChatVoice)", "valid voice auto-send path")
    assert_contains(vm, "return false;\n        }\n\n        userMessage = activeTurnTranscriptValidation.NormalizedTranscript", "Lesson Chat invalid transcript short-circuit")
    assert_contains(realtime, "waiting for transcript before response.create", "Realtime transcript-gated commit")
    assert_contains(realtime, "HandleUserTranscriptCompletedAsync", "Realtime transcript completion handler")
    assert_contains(realtime, "NormalAssistantResponseCreated=False", "Realtime invalid no-response logging")
    assert_not_contains_between(realtime, 'case "user.audio.commit":', 'case "session.stop":', 'await CreateResponseAsync(cancellationToken);', "Realtime response.create before transcript validation")

    expected_opening = "Hi! I'm {tutorName}. I live next door. What's your name?"
    neighbor = next(variant for variant in introductions["controlledVariation"]["contextVariants"] if variant["id"] == "new_neighbor")
    if neighbor["openingLine"] != expected_opening:
        raise AssertionError("New-neighbor opening must self-introduce with the tutorName placeholder.")
    if "Lana" in neighbor["openingLine"]:
        raise AssertionError("New-neighbor opening must not hardcode the active tutor profile name.")

    final_json = introductions["conversationFlow"]["finalMessage"]
    if "Great work today!" not in final_json:
        raise AssertionError("Scenario final message should come from lesson JSON.")

    assert_contains(prompt, "Scenario conversation flow from lesson JSON", "scenario flow prompt")
    assert_contains(prompt, "isFirstWrapUpInstruction", "one-time wrap-up prompt state")
    assert_contains(prompt, "Continue closing the current selected scenario; do not repeat the first wrap-up transition", "repeat wrap-up guard")
    assert_contains(prompt, "GetRuntimePhase", "backend phase derivation")
    assert_contains(prompt, "Exact final message from lesson JSON", "realtime final JSON prompt")
    assert_contains(prompt, "A1 introductions/new-neighbor rules", "A1 introductions flow guard")
    assert_contains(prompt, "Nice to meet you, David. Where are you from?", "A1 first-turn example")
    assert_contains(prompt, "Nice. Do you live here now?", "A1 second-turn example")
    assert_contains(vm, "GetSelectedContextConfirmationLine(matchedVariant)", "selected context confirmation line")
    assert_contains(vm, "GetSelectedContextOpeningLine()", "profile-resolved selected context opening")
    assert_contains(vm, "await PlayConversationModeBotVoiceAsync(botMessage);", "Conversation Mode TTS final assistant playback before completion")
    assert_contains(vm, "await TryAutoPlayNewestBotVoiceAsync(botMessage);", "normal final assistant message playback before completion")
    assert_contains(vm, "CurrentLessonPhase = LessonPhase.Completed", "final assistant message before completion")

    print("Lesson turn policy checks passed.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (AssertionError, ValueError) as exc:
        print(exc, file=sys.stderr)
        raise SystemExit(1)
