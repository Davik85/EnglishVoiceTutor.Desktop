#!/usr/bin/env python3
"""Static policy tests for Conversation Mode Realtime record readiness and audio path."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(text: str, needle: str, path: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {path}")


def main() -> None:
    vm = read("ViewModels/LessonChatViewModel.cs")
    xaml = read("Views/LessonChatView.xaml")
    engine = read("Services/Voice/RealtimeVoiceConversationEngine.cs")
    iface = read("Services/Voice/IVoiceConversationEngine.cs")
    mic = read("Services/Voice/RealtimeMicrophoneCaptureService.cs")
    backend = read("backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs")

    for needle in [
        "IsConversationRecordButtonEnabled => CanToggleVoiceRecording()",
        "IsEnabled=\"{Binding IsConversationRecordButtonEnabled}\"",
        "GetRealtimeRecordBlockReason",
        "LogRealtimeRecordState",
        "CurrentConversationModeState == ConversationModeState.Ready",
        "SetConversationModeState(ConversationModeState.Ready, \"session_ready_event\")",
        "isRealtimeSessionStarted = true;",
        "isStartingRealtimeSession = false;",
        "PrepareForRealtimeConversationStartup",
        "CancelCurrentBotVoice(BotVoiceCancellationReasons.RealtimeStartupCancel)",
        "audioPlaybackService.StopPlayback()",
        "IsBotVoicePlaying = false;",
        "!IsRealtimeConversationActive",
        "Skipping bot voice",
        "Conversation Mode became active",
        "await StartRealtimeVoiceRecordingAsync();",
        "realtimeMicrophoneCaptureService.Start(audioInputDeviceId)",
        "SetConversationModeState(isRealtimeSessionStarted ? ConversationModeState.Ready : ConversationModeState.Faulted, \"record_start_failed\")",
        "await realtimeVoiceEngine.AppendUserAudioAsync(audioChunk, CancellationToken.None)",
        "await realtimeVoiceEngine.CommitUserAudioAsync(CancellationToken.None)",
        "Ignoring stale realtime UI event",
    ]:
        require(vm + "\n" + xaml, needle, "ViewModels/LessonChatViewModel.cs or Views/LessonChatView.xaml")

    for needle in [
        "event EventHandler<VoiceSessionReadyEventArgs>? SessionReady",
        "SessionReady?.Invoke",
        "sessionStartCompletionSource?.TrySetResult(true)",
        "Ignoring stale realtime event",
        "Realtime user audio append requested",
        "Realtime user audio commit requested",
    ]:
        require(iface + "\n" + engine, needle, "Services/Voice realtime engine")

    for needle in [
        "Realtime microphone device selected",
        "Realtime microphone streaming started",
        "Realtime microphone bytes captured",
        "Realtime microphone streaming stopped",
        "RealtimeInputPcmSampleRate",
    ]:
        require(mic, needle, "Services/Voice/RealtimeMicrophoneCaptureService.cs")

    for needle in [
        "Realtime desktop start_recording event received",
        "Realtime desktop audio append received",
        "Realtime desktop commit received",
        "Realtime desktop stop/cancel received",
        "DesktopStopReason={DesktopStopReason}",
        "Realtime ignored unknown desktop message",
        "input_audio_buffer.append",
        "input_audio_buffer.commit",
    ]:
        require(backend, needle, "backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs")

    print("Realtime record button policy passed.")


if __name__ == "__main__":
    main()
