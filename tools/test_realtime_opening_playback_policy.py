#!/usr/bin/env python3
"""Static policy tests for Conversation Mode pre-start opening playback."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {label}")


def main() -> None:
    vm = read("ViewModels/LessonChatViewModel.cs")
    state = read("Models/ConversationModeState.cs")
    desktop_backend = read("Services/LessonChatBackendService.cs")
    speech_request = read("Models/AudioSpeechBackendRequest.cs")
    api_speech_request = read("backend/EnglishVoiceTutor.Api/Models/AudioSpeechRequest.cs")
    api_speech = read("backend/EnglishVoiceTutor.Api/Services/AudioSpeechService.cs")

    for needle in [
        "OpeningPlayback",
        "CurrentConversationModeState == ConversationModeState.Ready",
    ]:
        require(state + vm, needle, "Conversation Mode state/readiness")

    for needle in [
        "PlayRealtimePreStartOpeningAsync",
        "SelectRealtimeOpeningMessagesToSpeak",
        "spokenRealtimeOpeningMessageIds",
        "SetConversationModeState(ConversationModeState.OpeningPlayback",
        "BackendConstants.RealtimePreStartOpeningSpeechPurpose",
        "realtime_pre_start_opening",
        "Model={BackendConstants.TtsModelName}",
        "GetExactBotVoiceText(openingMessage)",
        "ExactVisibleText=True",
        "allowDuringRealtimeOpeningPlayback: true",
        "speechPurpose: BackendConstants.RealtimePreStartOpeningSpeechPurpose",
        "Do not duplicate",  # intentionally absent? no
    ]:
        if needle == "Do not duplicate":
            continue
        require(vm, needle, "LessonChatViewModel.cs")

    require(vm, "AddMessage(TutorAvatarDisplayName", "LessonChatViewModel.cs")
    opening_method = vm.split("private async Task PlayRealtimePreStartOpeningAsync", 1)[1].split("private IReadOnlyList<ChatMessageViewModel> SelectRealtimeOpeningMessagesToSpeak", 1)[0]
    if "AddMessage(" in opening_method:
        raise AssertionError("Opening playback must not duplicate chat messages.")
    if "response.create" in opening_method or "CreateResponseAsync" in opening_method:
        raise AssertionError("Opening playback must not create a Realtime assistant response.")

    for needle in [
        "Purpose { get; init; }",
        "Purpose = purpose",
        "Purpose={purpose}; Model={resolvedModel}",
        "Purpose={Purpose}",
        "tts",
    ]:
        require(desktop_backend + speech_request + api_speech_request + api_speech, needle, "TTS purpose plumbing")

    print("Realtime opening playback policy passed.")


if __name__ == "__main__":
    main()
