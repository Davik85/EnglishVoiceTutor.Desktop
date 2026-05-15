#!/usr/bin/env python3
"""Static policy tests for Realtime transcript placeholder recovery and fallback transcription."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {label}")


def main() -> None:
    vm = read("ViewModels/LessonChatViewModel.cs")
    validator = read("Shared/LessonPolicies/LessonTranscriptValidator.cs")
    realtime = read("backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs")

    for needle in [
        "ResetRealtimeCommittedAudioBuffer",
        "BufferRealtimeAudioChunkForFallback",
        "SaveRealtimeFallbackAudioFileAsync",
        "SendAudioForTranscriptionAsync(fallbackFilePath",
        "BackendConstants.TranscriptionModelName",
        "fallback_valid_user_transcript",
        "await realtimeVoiceEngine.SendUserTextAsync(normalizedTranscript",
        "DuplicateLearnerTurnCreated=False",
        "DuplicateAssistantResponseCreated=False",
        "ResolveRealtimePlaceholderAsStatus",
        "ApplyRealtimeUserTranscriptFailure(itemId, args.SessionId)",
        "MarkAsInvalidLearnerTranscript(RealtimeVoiceTranscriptionUnavailableText)",
        "LogInvalidRealtimeTranscriptDecision",
        "EstimatedBufferedAudioDurationSeconds",
        "WasNonEnglish",
        "WasTooShort",
        "WasPlaceholder",
    ]:
        require(vm, needle, "LessonChatViewModel.cs")

    for sample in ["David", "Russia", "Moscow", "Yes", "No", "I work", "I study"]:
        if len(sample.replace(" ", "")) < 2:
            continue
    require(validator, "InvalidTranscriptUserMessage", "LessonTranscriptValidator.cs")
    require(validator, "LessonTranscriptValidationReason.Placeholder", "LessonTranscriptValidator.cs")

    for needle in [
        "committedAudioBytesAwaitingTranscript",
        "committedAudioChunkCountAwaitingTranscript",
        "LogRealtimeInvalidTranscriptDecision",
        "Realtime stale user transcript ignored",
        "TranscriptionTimeoutMs={TimeoutMs}",
        "ValidationReason={ValidationReason}",
        "RetryPromptShown={RetryPromptShown}",
        "EstimatedBufferedAudioDurationSeconds={EstimatedBufferedAudioDurationSeconds}",
    ]:
        require(realtime, needle, "RealtimeVoiceSessionService.cs")

    print("Realtime transcription recovery policy passed.")


if __name__ == "__main__":
    main()
