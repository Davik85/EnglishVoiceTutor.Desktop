from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(text: str, needle: str, path: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {path}")


def main() -> None:
    usage = read("backend/EnglishVoiceTutor.Api/Models/UsageMetrics.cs")
    for name in [
        "LessonUsageMetrics",
        "OpenAiCallUsageMetrics",
        "AudioUsageMetrics",
        "RealtimeUsageMetrics",
        "LessonCostEstimate",
        "CostEstimationOptions",
        "PricingConstants",
    ]:
        require(usage, name, "backend/EnglishVoiceTutor.Api/Models/UsageMetrics.cs")

    require(usage, "public const decimal Tts1PerMillionCharactersUsd = 0m", "backend/EnglishVoiceTutor.Api/Models/UsageMetrics.cs")
    require(usage, "public const decimal RealtimeAudioInputPerMillionTokensUsd = 0m", "backend/EnglishVoiceTutor.Api/Models/UsageMetrics.cs")

    responses = read("backend/EnglishVoiceTutor.Api/Models/OpenAiResponsesResponse.cs")
    for field in ["input_tokens", "output_tokens", "total_tokens", "cached_tokens", "audio_tokens"]:
        require(responses, field, "backend/EnglishVoiceTutor.Api/Models/OpenAiResponsesResponse.cs")

    chat = read("backend/EnglishVoiceTutor.Api/Services/OpenAiLessonChatService.cs")
    require(chat, "Developer usage summary", "backend/EnglishVoiceTutor.Api/Services/OpenAiLessonChatService.cs")
    require(chat, "HasExactUsage", "backend/EnglishVoiceTutor.Api/Services/OpenAiLessonChatService.cs")

    tts = read("backend/EnglishVoiceTutor.Api/Services/AudioSpeechService.cs")
    require(tts, "Operation=tts", "backend/EnglishVoiceTutor.Api/Services/AudioSpeechService.cs")
    require(tts, "request.Model, request.Voice", "backend/EnglishVoiceTutor.Api/Services/AudioSpeechService.cs")

    transcription = read("backend/EnglishVoiceTutor.Api/Services/AudioTranscriptionService.cs")
    require(transcription, "Operation=audio_transcription", "backend/EnglishVoiceTutor.Api/Services/AudioTranscriptionService.cs")
    require(transcription, "EstimatedDurationSeconds", "backend/EnglishVoiceTutor.Api/Services/AudioTranscriptionService.cs")

    realtime = read("backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs")
    for needle in ["Operation=realtime_session", "TotalCommittedAudioBytes", "AudioCommits", "Operation=realtime_response"]:
        require(realtime, needle, "backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs")

    docs = read("docs/COST_MODEL.md")
    require(docs, "tts-1", "docs/COST_MODEL.md")
    require(docs, "gpt-realtime", "docs/COST_MODEL.md")


if __name__ == "__main__":
    main()
