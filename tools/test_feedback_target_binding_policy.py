#!/usr/bin/env python3
"""Deterministic checks for Lesson Chat feedback target binding."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
READ_ONLY_FEEDBACK_PROPERTIES = [
    "ShortText",
    "CorrectedVersion",
    "GrammarTip",
    "VocabularyTip",
    "CultureTip",
    "NaturalVersion",
    "MoreNaturalVersion",
    "SourceText",
]


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Unexpected {label}: {needle}")


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


def assert_feedback_textbox_bindings_are_one_way(xaml: str) -> None:
    for match in re.finditer(r"<TextBox\b[^>]*\bText=\"\{Binding (?P<binding>[^\"]+)\}\"", xaml):
        binding = match.group("binding")
        for property_name in READ_ONLY_FEEDBACK_PROPERTIES:
            if re.search(rf"(?:^|\.){re.escape(property_name)}(?:\s*,|$)", binding) and "Mode=OneWay" not in binding:
                raise AssertionError(
                    f"Feedback TextBox binding to read-only property {property_name} must specify Mode=OneWay: {binding}"
                )


def main() -> int:
    xaml = read("Views/LessonChatView.xaml")
    vm = read("ViewModels/LessonChatViewModel.cs")
    msg = read("ViewModels/ChatMessageViewModel.cs")
    desktop_request = read("Models/LessonChatBackendRequest.cs")
    api_request = read("backend/EnglishVoiceTutor.Api/Models/LessonChatRequest.cs")
    prompt = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")

    for needle in [
        "public int MessageId => Id;",
        "public string SourceMessageKind",
        "public bool CanShowFeedbackAction",
    ]:
        assert_contains(msg, needle, f"message feedback targeting state {needle}")

    assert_contains(xaml, "CommandParameter=\"{Binding}\"", "View feedback passes clicked message")
    assert_contains(xaml, "{Binding CanShowFeedbackAction}", "technical and bot messages do not show feedback action")
    assert_not_contains(xaml, "{Binding IsFeedbackVisible}", "inline feedback visibility under message")
    assert_not_contains(xaml, "{Binding Feedback.ShortText}", "inline quick summary binding")
    assert_contains(xaml, "Grid.Row=\"1\"", "global bottom feedback panel row")
    assert_contains(xaml, "{Binding HasSelectedFeedbackPanel}", "global selected feedback panel visibility")
    assert_contains(xaml, "Text=\"Feedback for\"", "feedback source phrase label")
    assert_contains(xaml, "Text=\"{Binding SelectedFeedbackSourceText, Mode=OneWay}\"", "selected source phrase binding")
    assert_contains(xaml, "{Binding SelectedFeedback.HasCorrectedVersion}", "empty corrected section hidden")
    assert_contains(xaml, "{Binding SelectedFeedback.HasGrammarTip}", "empty grammar section hidden")
    assert_contains(xaml, "{Binding SelectedFeedback.HasVocabularyTip}", "empty vocabulary section hidden")
    assert_contains(xaml, "{Binding SelectedFeedback.HasCultureTip}", "empty culture section hidden")
    assert_contains(xaml, "{Binding SelectedFeedback.HasNaturalVersion}", "empty natural section hidden")
    assert_contains(xaml, "ToggleFeedbackTranslationCommand", "feedback translate button remains available")
    assert_feedback_textbox_bindings_are_one_way(xaml)

    view_feedback = extract_method(vm, "private async Task ViewFeedbackAsync(ChatMessageViewModel? message)")
    for needle in [
        "var requestedMessage = message;",
        "var requestedMessageId = requestedMessage.MessageId;",
        "var requestedText = requestedMessage.Text.Trim();",
        "var requestedSourceKind = requestedMessage.SourceMessageKind;",
        "new FeedbackRequestTarget(",
        "SelectedFeedbackMessageId = requestedMessageId;",
        "SelectedFeedbackSourceText = requestedText;",
        "feedbackByMessageId.TryGetValue(requestedMessageId",
        "feedbackByMessageId[requestedMessageId] = feedback;",
        "SelectedFeedbackMessageId != requestedMessageId",
        "Feedback result ignored as stale",
        "staleResultIgnored=True",
        "DisplayFeedbackForRequestedMessage(requestedTarget",
    ]:
        assert_contains(view_feedback, needle, f"feedback exact-target logic {needle}")

    display_feedback = extract_method(vm, "private void DisplayFeedbackForRequestedMessage(FeedbackRequestTarget requestedTarget, Feedback feedback, bool fromCache)")
    for needle in [
        "requestedTarget.Message.SetFeedback(feedback);",
        "SelectedFeedbackSourceText = requestedTarget.Text;",
        "SelectedFeedback = feedback;",
        "displayedUnderMessageId={SelectedFeedbackMessageId}",
        "staleResultIgnored=False",
    ]:
        assert_contains(display_feedback, needle, f"global feedback displays requested message {needle}")

    for needle in [
        "private int selectedFeedbackMessageId;",
        "private string selectedFeedbackSourceText = string.Empty;",
        "public bool HasSelectedFeedbackPanel",
    ]:
        assert_contains(vm, needle, f"selected global feedback state {needle}")

    build_request = extract_method(vm, "private LessonChatBackendRequest BuildLessonFeedbackRequest(FeedbackRequestTarget target)")
    for needle in [
        "UserMessage = target.Text",
        "SourceMessageId = target.MessageId",
        "SourceMessageKind = target.SourceMessageKind",
        "LessonPhase = target.LessonPhase",
        "UserTurnNumber = target.LessonTurnNumber",
    ]:
        assert_contains(build_request, needle, f"feedback request captured source {needle}")
    if "LastUserMessage" in build_request:
        raise AssertionError("Feedback request must not fall back to a last-user-message source.")

    source_kind = extract_method(vm, "private static string GetFeedbackSourceMessageKind(ChatMessageViewModel message)")
    assert_contains(source_kind, "return message.SourceMessageKind;", "source kind comes from clicked message")
    for kind in ["ContextSelection", "ActiveRoleplay", "RealtimeTranscript", "NormalVoiceTranscript"]:
        assert_contains(msg, kind, f"feedback source kind {kind}")

    for model_text, label in [(desktop_request, "desktop request"), (api_request, "api request")]:
        assert_contains(model_text, "public int SourceMessageId", f"{label} source id")
        assert_contains(model_text, "public string SourceMessageKind", f"{label} source kind")

    for needle in [
        "Create feedback only for the exact learner source message above",
        "SourceMessageId",
        "SourceMessageKind",
        "Context-selection feedback mode",
        "not answering the active roleplay yet",
        "clear for choosing the situation",
        "correct only capitalization and punctuation",
        "Suggest a more natural full sentence",
        "starts the scenario and is not the learner's roleplay answer yet",
        "Do not say the learner did not give an update",
        "Do not say this does not fit a daily standup update",
        "Do not say the learner repeated the situation name",
        "Do not treat the context phrase as an active roleplay reply",
        "Active roleplay feedback mode",
        "RealtimeTranscript",
        "NormalVoiceTranscript",
    ]:
        assert_contains(prompt + msg, needle, f"feedback prompt/source policy {needle}")

    print("Feedback target binding policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
