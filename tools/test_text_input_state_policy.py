#!/usr/bin/env python3
"""Deterministic checks for lesson text input/send gating."""
from __future__ import annotations

import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
LESSON_VM = ROOT / "ViewModels" / "LessonChatViewModel.cs"
LESSON_VIEW = ROOT / "Views" / "LessonChatView.xaml"
TURN_POLICY = ROOT / "Shared" / "LessonPolicies" / "LessonTurnPolicy.cs"
INTRODUCTIONS_JSON = ROOT / "Content" / "Lessons" / "EverydayEnglish" / "introductions.json"
PROMPT_BUILDER = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "LessonPromptBuilder.cs"


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def extract_method(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        raise AssertionError(f"Missing method: {signature}")
    brace = text.find("{", start)
    depth = 0
    for index in range(brace, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[start:index + 1]
    raise AssertionError(f"Could not extract method: {signature}")


def py_final(lesson_type: str, level: str, content_final: int | None = None) -> int:
    if content_final and content_final > 0:
        return content_final
    if lesson_type == "free_conversation":
        return 30
    return 15 if level.startswith(("A1", "A2")) else 25


def main() -> int:
    vm = read(LESSON_VM)
    xaml = read(LESSON_VIEW)
    turn_policy = read(TURN_POLICY)
    introductions = json.loads(read(INTRODUCTIONS_JSON))
    prompt_builder = read(PROMPT_BUILDER)

    assert_contains(vm, "public bool CanTypeText => CanAcceptLessonInput;", "independent text edit enabled property")
    assert_contains(xaml, "IsEnabled=\"{Binding CanTypeText}\"", "TextBox enabled binding is edit gate")
    if "IsEnabled=\"{Binding IsLessonInputEnabled}\"\n                                             Text=\"{Binding UserInput" in xaml:
        raise AssertionError("TextBox must not bind directly to the send/input command state alias.")

    can_send = extract_method(vm, "private bool CanSendMessage()")
    assert_contains(can_send, "CanAcceptLessonInput && !string.IsNullOrWhiteSpace(UserInput)", "send requires text plus input gate")
    assert_contains(can_send, "LogTextInputState(\"send_can_execute_blocked\", canSend);", "send blocked diagnostics")

    assert_contains(vm, "private bool CanAcceptLessonInput => !hasFinishedLesson && !IsCompletedAwaitingFinish && !IsLessonLimitReached && !IsLessonBusyForInput && !IsRealtimeConversationActive;", "text input gate blocks only terminal/busy/realtime states")
    normal_record = extract_method(vm, "private bool CanStartNormalRecording()")
    for expected in ["!hasFinishedLesson", "!IsCompletedAwaitingFinish", "!IsLessonLimitReached", "!IsSending", "!IsRealtimeSessionStarting"]:
        assert_contains(normal_record, expected, f"normal voice shared gate {expected}")

    block_reason = extract_method(vm, "private string GetTextSendBlockReason()")
    for reason in ["lesson_completed_awaiting_finish", "lesson_limit_reached", "realtime_conversation_active", "assistant_turn_or_text_send_in_progress", "recording_in_progress", "realtime_session_starting", "empty_text"]:
        assert_contains(block_reason, reason, f"text block reason {reason}")
    log_method = extract_method(vm, "private void LogTextInputState(string reason, bool canSend)")
    for field in ["CurrentLessonPhase", "IsLessonCompleteAwaitingFinish", "ConversationModeState", "IsConversationModeActive", "IsSending", "IsBotTyping", "IsBotVoicePlaying", "IsRecording", "IsTranscribing", "LearnerTurnCount", "TextLength", "CanSend", "BlockReason"]:
        assert_contains(log_method, field, f"text diagnostic field {field}")

    refresh = extract_method(vm, "private void RefreshAllCommandStates()")
    assert_contains(refresh, "SendMessageCommand.NotifyCanExecuteChanged();", "send command refresh")
    assert_contains(refresh, "OnPropertyChanged(nameof(IsLessonInputEnabled));", "input enabled refresh")
    assert_contains(refresh, "OnPropertyChanged(nameof(CanTypeText));", "text edit refresh")

    context_handler = extract_method(vm, "private async Task<bool> HandleContextSelectionMessageAsync(string userMessage)")
    assert_contains(context_handler, "isTechnicalMessage: true", "context selection is technical")
    assert_contains(vm, "LearnerTurnCountAfter={LearnerTurnCount}", "context selection keeps learner count unchanged")

    send_lesson = extract_method(vm, "private async Task<bool> SendLessonMessageAsync(string userMessage, string messageSource = ChatMessageSource.Typed)")
    assert_contains(send_lesson, "if (response.IsLessonComplete && !shouldEndLessonNow)", "ignore early backend completion")
    assert_contains(send_lesson, "Ignoring early backend lesson completion", "early completion diagnostic")
    assert_contains(send_lesson, "if (shouldEndLessonNow)", "local final turn controls completion")
    if "if (response.IsLessonComplete || shouldEndLessonNow)" in send_lesson:
        raise AssertionError("Backend lesson completion must not force early Awaiting Finish before the local final turn.")

    assert_contains(turn_policy, "BeginnerGuidedFinalTurn = 15", "A1/A2 guided final turn")
    assert_contains(turn_policy, "AdvancedGuidedFinalTurn = 25", "B1/B2 guided final turn")
    assert_contains(turn_policy, "FreeConversationFinalTurn = 30", "free conversation final turn")
    assert_contains(turn_policy, "context.CurrentPhase == LessonTurnPhase.ActiveRoleplay", "setup messages do not count")
    assert_contains(turn_policy, "isValidEnglishTranscript", "invalid transcript does not count")

    metadata = introductions["metadata"]
    a1_profile = introductions["levelProfiles"]["A1 Beginner"]
    if py_final(metadata["lessonType"], "A1 Beginner", a1_profile.get("finalMessageAtUserTurn")) != 15:
        raise AssertionError("A1 Introductions must not complete at user turn 2.")
    if "Nastya" in read(INTRODUCTIONS_JSON) or "Nastya" in prompt_builder:
        raise AssertionError("Lesson JSON and prompt builder must not hardcode Nastya as tutor identity.")
    assert_contains(prompt_builder, "TutorProfileId", "active tutor profile remains in prompt request")

    print("Text input state policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
