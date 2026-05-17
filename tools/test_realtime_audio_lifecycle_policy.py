#!/usr/bin/env python3
"""Static policy tests for Realtime assistant audio lifecycle ownership."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {label}")


def extract_method(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        raise AssertionError(f"Missing method {signature}")
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
    raise AssertionError(f"Could not extract method {signature}")


def main() -> int:
    vm = read("ViewModels/LessonChatViewModel.cs")
    playback = read("Services/Voice/RealtimeAudioPlaybackService.cs")
    engine = read("Services/Voice/RealtimeVoiceConversationEngine.cs")

    assistant_audio = extract_method(vm, "private void OnRealtimeAssistantAudioChunkReceived")
    require(assistant_audio, "SetConversationModeState(ConversationModeState.PlayingAssistantAudio, \"assistant_audio_delta\")", "assistant audio handler")
    if "StopRealtimeConversationAsync" in assistant_audio:
        raise AssertionError("Assistant audio start must not stop Conversation Mode.")

    assistant_completed = extract_method(vm, "private void OnRealtimeAssistantTurnCompleted")
    require(assistant_completed, "realtimeAudioPlaybackService.CompleteResponse(args.SessionId, args.ResponseId)", "assistant completion marks playback complete")
    require(assistant_completed, "assistant_turn_completed_waiting_for_playback", "assistant completion waits for playback drain")
    if "StopRealtimeConversationAsync" in assistant_completed:
        raise AssertionError("Assistant turn completion must not stop Conversation Mode.")

    playback_completed = extract_method(vm, "private void OnRealtimePlaybackCompleted")
    require(playback_completed, "SetConversationModeState(ConversationModeState.Ready, \"assistant_playback_completed\")", "playback completion returns Ready")
    require(playback_completed, "RefreshAllCommandStates();", "playback completion refreshes record button")
    if "StopRealtimeConversationAsync" in playback_completed:
        raise AssertionError("Assistant playback completion must not stop Conversation Mode.")

    opening = extract_method(vm, "private async Task PlayRealtimePreStartOpeningAsync")
    require(opening, "realtime_pre_start_opening_playback_finished", "opening playback only changes state")
    if "StopRealtimeConversationAsync" in opening or "StopSessionAsync" in opening:
        raise AssertionError("Opening pre-start playback cleanup must not stop an active Realtime session.")

    stop = extract_method(vm, "private async Task StopRealtimeConversationAsync")
    for reason in ["user_clicked_back", "user_clicked_conversation_mode_exit", "final_cleanup", "runtime_failure"]:
        require(vm, reason, f"mapped realtime lifecycle reason {reason}")
    require(stop, "Realtime engine StopSessionAsync requested", "explicit stop logging")
    require(stop, "await realtimeVoiceEngine.StopSessionAsync(CancellationToken.None)", "explicit stop sends client stop")

    require(playback, "event EventHandler<RealtimePlaybackCompletedEventArgs>? PlaybackCompleted", "playback completion event")
    require(playback, "StopConversationModeRequested=False", "playback logs document no conversation stop")
    require(playback, "TryCompletePlaybackUnderLock", "playback drain completion")
    require(playback, "public bool IsPlaybackActive", "VM can keep record disabled while audio drains")

    require(engine, "Realtime voice session.stop sending", "engine stop/cancel log")
    require(engine, "Realtime voice CloseAsync requested", "engine close log")

    print("Realtime audio lifecycle policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
