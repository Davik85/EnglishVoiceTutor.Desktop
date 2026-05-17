#!/usr/bin/env python3
"""Deterministic checks for Lesson Chat feedback target binding."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")


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


def main() -> int:
    xaml = read("Views/LessonChatView.xaml")
    vm = read("ViewModels/LessonChatViewModel.cs")
    msg = read("ViewModels/ChatMessageViewModel.cs")
    desktop_request = read("Models/LessonChatBackendRequest.cs")
    api_request = read("backend/EnglishVoiceTutor.Api/Models/LessonChatRequest.cs")
    prompt = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")

    assert_contains(msg, "public int MessageId => Id;", "stable public MessageId")
    assert_contains(xaml, "CommandParameter=\"{Binding}\"", "View feedback passes clicked message")

    view_feedback = extract_method(vm, "private async Task ViewFeedbackAsync(ChatMessageViewModel? message)")
    for needle in [
        "var requestedMessage = message;",
        "var requestedMessageId = requestedMessage.MessageId;",
        "SelectedFeedbackMessageId = requestedMessageId;",
        "feedbackByMessageId.TryGetValue(requestedMessageId",
        "feedbackByMessageId[requestedMessageId] = feedback;",
        "SelectedFeedbackMessageId != requestedMessageId",
        "Feedback result ignored as stale",
        "DisplayFeedbackForRequestedMessage(requestedMessageId",
    ]:
        assert_contains(view_feedback, needle, f"feedback exact-target logic {needle}")

    build_request = extract_method(vm, "private LessonChatBackendRequest BuildLessonFeedbackRequest(ChatMessageViewModel message)")
    for needle in [
        "UserMessage = message.Text.Trim()",
        "SourceMessageId = message.MessageId",
        "SourceMessageKind = GetFeedbackSourceMessageKind(message)",
    ]:
        assert_contains(build_request, needle, f"feedback request source {needle}")

    source_kind = extract_method(vm, "private static string GetFeedbackSourceMessageKind(ChatMessageViewModel message)")
    for kind in ["ContextSelection", "ActiveRoleplay", "RealtimeTranscript", "NormalVoiceTranscript"]:
        assert_contains(source_kind, kind, f"feedback source kind {kind}")

    for model_text, label in [(desktop_request, "desktop request"), (api_request, "api request")]:
        assert_contains(model_text, "public int SourceMessageId", f"{label} source id")
        assert_contains(model_text, "public string SourceMessageKind", f"{label} source kind")

    for needle in [
        "Create feedback only for the exact learner source message above",
        "Context-selection feedback mode:",
        "not answering the active roleplay yet",
        "Do not criticize the learner for not giving a roleplay answer",
        "Active roleplay feedback mode:",
    ]:
        assert_contains(prompt, needle, f"backend feedback prompt {needle}")

    print("Feedback target binding policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
