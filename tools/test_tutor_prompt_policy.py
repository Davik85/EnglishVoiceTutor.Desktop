#!/usr/bin/env python3
"""Deterministic checks for shared tutor prompt policy and avatar-neutral lesson content."""
from __future__ import annotations

import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROMPT_BUILDER = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "LessonPromptBuilder.cs"
REALTIME_SERVICE = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "RealtimeVoiceSessionService.cs"
INTRODUCTIONS_JSON = ROOT / "Content" / "Lessons" / "EverydayEnglish" / "introductions.json"
FREE_CONVERSATION_JSON = ROOT / "Content" / "Lessons" / "FreeConversation" / "open_conversation.json"
RUNTIME_PATHS = [ROOT / "backend", ROOT / "Content"]


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Unexpected {label}: {needle}")


def runtime_text() -> str:
    chunks: list[str] = []
    for root in RUNTIME_PATHS:
        for path in root.rglob("*"):
            if path.is_file() and "bin" not in path.parts and "obj" not in path.parts:
                chunks.append(read(path))
    return "\n".join(chunks)


def main() -> int:
    prompt_builder = read(PROMPT_BUILDER)
    realtime_service = read(REALTIME_SERVICE)
    runtime = runtime_text()
    introductions = json.loads(read(INTRODUCTIONS_JSON))
    free_conversation = read(FREE_CONVERSATION_JSON)

    for needle in [
        "A1 strict output rules",
        "Use very simple English",
        "Ask one question at a time",
        "Where did you move from?",
        "How long have you been living here?",
    ]:
        assert_contains(prompt_builder, needle, "A1 shared rule")

    assert_contains(prompt_builder, "BuildRealtimeInstructions", "realtime adapter")
    assert_contains(prompt_builder, "BuildInput", "chat adapter")
    assert_contains(prompt_builder, "Tutor identity comes only from the selected TutorProfile", "tutor profile injection")
    assert_contains(prompt_builder, "Guided roleplay must not become generic AI chat", "guided retention")
    assert_contains(prompt_builder, "Guided scenario flexibility:", "shared guided scenario flexibility block")
    assert_contains(prompt_builder, "Answer natural learner questions that fit the scenario", "natural reciprocal questions allowed")
    assert_contains(prompt_builder, "Use the active tutor profile for simple personal answers", "active tutor profile simple personal answers")
    assert_contains(prompt_builder, "No, I'm your neighbor", "forbidden neighbor study/work answer")
    assert_contains(prompt_builder, "For A1, answer with one short sentence plus one simple question.", "A1 reciprocal answer shape")
    assert_contains(prompt_builder, "AppendGuidedScenarioFlexibilityPolicy(prompt)", "shared flexibility method consumed by canonical policy")
    assert_contains(prompt_builder, "AppendCanonicalTeachingPolicy(prompt, request, avatarProfile, NormalChatMode)", "normal chat uses canonical policy")
    assert_contains(prompt_builder, "AppendCanonicalTeachingPolicy(prompt, chatRequest, avatarProfile, RealtimeVoiceMode)", "realtime uses canonical policy")
    assert_contains(prompt_builder, "Free Conversation allows safe open topic selection", "free conversation open topic behavior")
    assert_contains(realtime_service, "lessonPromptBuilder.BuildRealtimeInstructions", "shared realtime session instructions")
    assert_contains(realtime_service, "lessonPromptBuilder.BuildRealtimeResponseInstructions", "shared realtime response instructions")

    for stale in ["I'm Alex", "I am Alex", "my name is Alex"]:
        assert_not_contains(runtime, stale, "stale tutor identity")

    for generic in [
        "How can I assist you today",
        "What would you like to discuss",
        "What would you like to talk about",
        "Want some tips",
        "How can I help you",
    ]:
        assert_not_contains(runtime, generic, "generic guided assistant phrase")

    dumped_introductions = json.dumps(introductions)
    assert_not_contains(dumped_introductions, "Elena", "avatar-specific scenario content")
    assert_not_contains(dumped_introductions, "Alex", "stale scenario tutor name")
    assert_not_contains(dumped_introductions, "levelSpecificTurnPlans", "new duplicated level-specific turn plan field")
    assert_not_contains(dumped_introductions, "forbiddenByLevel", "new duplicated level-specific forbidden field")
    assert_not_contains(dumped_introductions, "A1-only", "A1-only scenario rule")

    new_neighbor = next(variant for variant in introductions["controlledVariation"]["contextVariants"] if variant["id"] == "new_neighbor")
    assert_contains(new_neighbor["openingLine"], "{tutorName}", "profile-driven tutor name placeholder")
    assert_contains(new_neighbor["openingLine"], "I live next door", "neighbor self-introduction opening")
    assert_contains(new_neighbor["contextConfirmationLine"], "meet a new neighbor", "context confirmation line")
    assert_contains(dumped_introductions, "roleplayBeats", "scenario roleplay beats")
    assert_contains(dumped_introductions, "reciprocalQuestionHandling", "reciprocal question handling")
    assert_contains(dumped_introductions, "ifUserAsksSimplePersonalQuestion", "simple personal reciprocal question handling")
    assert_contains(dumped_introductions, "mustNotRefuseScenarioCompatibleQuestions", "scenario-compatible reciprocal question handling")

    for needle in [
        "ResolveScenarioPlaceholders(request.SelectedContextOpeningLine, avatarProfile)",
        "Roleplay beats:",
        "Reciprocal question handling:",
        "SelectedContextConfirmationLine = request.SelectedContextConfirmationLine",
        "using the active tutor profile name",
        "ReciprocalQuestionIfUserAsksSimplePersonalQuestion",
        "Must not refuse scenario-compatible questions",
    ]:
        assert_contains(prompt_builder, needle, "prompt-builder shared scenario rule consumption")

    assert_contains(free_conversation, "Which topic would you like to practice?", "free conversation open-topic prompt")

    print("Tutor prompt policy checks passed.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(exc, file=sys.stderr)
        raise SystemExit(1)
