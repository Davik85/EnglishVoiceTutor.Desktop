from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(text: str, needle: str, path: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {path}")


def main() -> None:
    vm = read("ViewModels/LessonChatViewModel.cs")
    state = read("Models/ConversationModeState.cs")
    for name in ["NotStarted", "Starting", "Ready", "Recording", "WaitingForTranscript", "WaitingForAssistant", "PlayingAssistantAudio", "Stopping", "Faulted", "CompletedAwaitingFinish"]:
        require(state, name, "Models/ConversationModeState.cs")
    for needle in [
        "realtimeLifecycleSemaphore",
        "realtimeRecordingSemaphore",
        "SetConversationModeState",
        "CleanupRealtimeAfterFaultAsync",
        "Microphone is not available",
        "Conversation Mode could not start. Please try again.",
        "OnRealtimeDisconnected",
        "RecoverRealtimeErrorAsync",
        "CanStartRealtimeRecording",
        "CurrentConversationModeState == ConversationModeState.Ready",
        "isRealtimeSessionStarted",
        "isStartingRealtimeSession = false;",
        "SafeStopRealtimeMicrophone(reason)",
        "realtimeAudioPlaybackService.Stop(reason)",
        "await realtimeVoiceEngine.StopSessionAsync(CancellationToken.None)",
        "SetConversationModeState(IsCompletedAwaitingFinish ? ConversationModeState.CompletedAwaitingFinish : ConversationModeState.NotStarted, reason)",
        "IsActiveRealtimeSessionEvent(args.SessionId",
        "Ignoring stale realtime UI event",
        "Conversation Mode toggle ignored because lifecycle operation is active",
        "PrepareForRealtimeConversationStartup",
        "RealtimeStartupCancel",
        "Normal bot voice playback stopped before Conversation Mode startup",
    ]:
        require(vm, needle, "ViewModels/LessonChatViewModel.cs")
    engine = read("Services/Voice/RealtimeVoiceConversationEngine.cs")
    for needle in [
        "Disconnected?.Invoke",
        "session.startup_failed",
        "session.runtime_failed",
        "if (!isSessionReady)",
        "sessionStartCompletionSource?.TrySetException",
        "Ignoring stale realtime event",
    ]:
        require(engine, needle, "Services/Voice/RealtimeVoiceConversationEngine.cs")


if __name__ == "__main__":
    main()
