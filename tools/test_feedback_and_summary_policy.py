#!/usr/bin/env python3
"""Deterministic checks for feedback eligibility, summary input, and audio model routing."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def main() -> int:
    vm = read("ViewModels/LessonChatViewModel.cs")
    msg = read("ViewModels/ChatMessageViewModel.cs")
    summary = read("ViewModels/LessonSummaryViewModel.cs")
    xaml = read("Views/LessonChatView.xaml")
    backend_constants = read("backend/EnglishVoiceTutor.Api/Constants/OpenAiConstants.cs")
    api_program = read("backend/EnglishVoiceTutor.Api/Program.cs")
    realtime_service = read("backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs")

    for source in ["ChatMessageSource.Typed", "ChatMessageSource.LessonChatVoice", "ChatMessageSource.RealtimeVoice"]:
        assert_contains(vm, source, f"message source {source}")

    assert_contains(msg, "IsFeedbackEligible", "feedback eligibility state")
    assert_contains(vm, "countsAsValidLessonTurn: true", "valid turns become counted messages")
    assert_contains(vm, "MarkAsValidLearnerTurn(validation.NormalizedTranscript", "realtime transcript normalization")
    assert_contains(vm, "MarkAsInvalidLearnerTranscript(RealtimeVoiceTranscriptionUnavailableText)", "invalid realtime transcript exclusion")
    assert_contains(xaml, "{Binding IsFeedbackEligible}", "View feedback button eligibility binding")

    feedback_request = re.search(r"private LessonChatBackendRequest BuildLessonFeedbackRequest[\s\S]+?\n    }", vm)
    if not feedback_request:
        raise AssertionError("Feedback request builder not found.")
    assert_contains(feedback_request.group(0), "UserMessage = message.Text.Trim()", "selected message text in feedback request")
    assert_contains(feedback_request.group(0), "RecentMessages = GetRecentConversationMessages()", "surrounding messages in feedback request")

    summary_builder = re.search(r"private LessonSummaryInput BuildLessonSummaryInput[\s\S]+?\n    }", vm)
    if not summary_builder:
        raise AssertionError("Summary input builder not found.")
    assert_contains(summary_builder.group(0), "Messages", "summary includes message list")
    assert_contains(summary_builder.group(0), "CountsAsValidLessonTurn", "summary requires counted learner turns")
    assert_contains(summary_builder.group(0), "ChatMessageSource.RealtimeVoice", "summary logs realtime-origin turns")
    assert_contains(vm, "LessonTranscriptValidator.VoiceMessagePlaceholder", "summary excludes voice placeholder")
    assert_contains(vm, "LessonTranscriptValidator.InvalidTranscriptUserMessage", "summary excludes invalid retry text")
    assert_contains(summary, "GetValidUserTurns(summaryInput)", "summary view consumes full summary input")

    assert_contains(backend_constants, 'NormalChatTtsModel = "tts-1"', "normal chat TTS model")
    assert_contains(backend_constants, "DefaultBotVoiceSpeechModel = NormalChatTtsModel", "audio/speech uses normal chat model constant")
    assert_contains(backend_constants, 'DefaultRealtimeVoiceModel = "gpt-realtime"', "realtime model unchanged")
    assert_contains(realtime_service, "DefaultRealtimeVoiceModel", "realtime service uses realtime model constant")
    if "AudioSpeech" in realtime_service or "/api/audio/speech" in realtime_service:
        raise AssertionError("Realtime service must not route generated turns through audio/speech.")
    assert_contains(api_program, "LessonChatFeedbackRoute", "feedback endpoint route")

    print("Feedback, summary, and audio routing policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
