#!/usr/bin/env python3
"""Deterministic checks for lesson input and message-review command separation."""
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
LESSON_VM = ROOT / "ViewModels" / "LessonChatViewModel.cs"
CHAT_MESSAGE_VM = ROOT / "ViewModels" / "ChatMessageViewModel.cs"
LESSON_VIEW = ROOT / "Views" / "LessonChatView.xaml"


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_regex(text: str, pattern: str, label: str) -> None:
    if not re.search(pattern, text, re.S):
        raise AssertionError(f"Missing {label}: {pattern}")


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


def main() -> int:
    lesson_vm = read(LESSON_VM)
    chat_message_vm = read(CHAT_MESSAGE_VM)
    lesson_view = read(LESSON_VIEW)

    assert_contains(lesson_vm, "private bool CanReviewExistingMessages => !hasFinishedLesson;", "review state separation")
    assert_contains(lesson_vm, "private bool CanAcceptLessonInput => !hasFinishedLesson && !IsCompletedAwaitingFinish && !IsLessonLimitReached && !IsLessonBusyForInput && !IsRealtimeConversationActive;", "send disabled while awaiting finish")

    for signature, blocked_state in [
        ("private bool CanToggleVoiceRecording()", "!IsCompletedAwaitingFinish"),
        ("private bool CanRequestHint()", "CanAcceptLessonInput"),
        ("private bool CanToggleConversationMode()", "!IsCompletedAwaitingFinish"),
        ("private bool CanGoBack()", "!IsCompletedAwaitingFinish"),
    ]:
        method = extract_method(lesson_vm, signature)
        assert_contains(method, blocked_state, f"awaiting-finish block in {signature}")

    finish_method = extract_method(lesson_vm, "private bool CanFinishLesson()")
    assert_contains(finish_method, "!hasFinishedLesson", "finish enabled before summary")
    assert_contains(finish_method, "!IsSending", "finish blocks sending")
    assert_contains(finish_method, "!IsRecording", "finish blocks recording")

    view_feedback_method = extract_method(lesson_vm, "private bool CanViewFeedback(ChatMessageViewModel? message)")
    assert_contains(view_feedback_method, "CanReviewExistingMessages", "feedback uses review state")
    assert_contains(view_feedback_method, "message.IsFeedbackEligible", "feedback eligibility")
    if "message.CountsAsValidLessonTurn" in view_feedback_method:
        raise AssertionError("View feedback must not require active turn counting; setup context learner English can be feedback eligible.")
    if "IsLessonOptionsEnabled" in view_feedback_method or "IsCompletedAwaitingFinish" in view_feedback_method:
        raise AssertionError("View feedback must not be disabled merely because the lesson is awaiting Finish lesson.")

    play_voice_method = extract_method(lesson_vm, "private bool CanPlayBotVoice(ChatMessageViewModel? message)")
    assert_contains(play_voice_method, "CanReviewExistingMessages", "play voice uses review state")
    assert_contains(play_voice_method, "message.ShowPlayVoiceButton", "play voice bot-message requirement")
    assert_contains(play_voice_method, "!string.IsNullOrWhiteSpace(message.Text)", "play voice visible text requirement")
    if "IsLessonOptionsEnabled" in play_voice_method or "IsCompletedAwaitingFinish" in play_voice_method:
        raise AssertionError("Play voice must not be disabled merely because the lesson is awaiting Finish lesson.")

    play_voice_dispatch = extract_method(lesson_vm, "private async Task PlayBotVoiceAsync(ChatMessageViewModel? message)")
    assert_contains(play_voice_dispatch, "await PlayBotVoiceForMessageAsync(message, isAutoPlay: false);", "manual play dispatch")
    assert_contains(lesson_vm, "var exactBotVoiceText = GetExactBotVoiceText(message);", "manual play exact visible text")

    assert_contains(chat_message_vm, "IsFeedbackEligible = !IsFromBot && !string.IsNullOrWhiteSpace(normalizedText);", "valid transcript feedback eligibility")
    assert_contains(chat_message_vm, "public void MarkAsInvalidLearnerTranscript", "invalid transcript marker")
    invalid_marker = extract_method(chat_message_vm, "public void MarkAsInvalidLearnerTranscript(string retryText)")
    assert_contains(invalid_marker, "IsFeedbackEligible = false;", "invalid transcript feedback exclusion")
    assert_contains(invalid_marker, "CountsAsValidLessonTurn = false;", "invalid transcript turn exclusion")

    assert_contains(lesson_view, "Command=\"{Binding ToggleTranslationCommand}\"", "message translate command binding")
    assert_contains(lesson_view, "Command=\"{Binding DataContext.PlayBotVoiceCommand", "play voice command binding")
    assert_contains(lesson_view, "Command=\"{Binding DataContext.ViewFeedbackCommand", "view feedback command binding")

    print("Lesson command state policy checks passed.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(exc, file=sys.stderr)
        raise SystemExit(1)
