using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Api.Models;

public sealed class LessonUsageMetrics
{
    public string LessonId { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public string Subtopic { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string LessonType { get; init; } = string.Empty;
    public string SelectedContext { get; init; } = string.Empty;
    public string TutorProfileId { get; init; } = string.Empty;
    public DateTimeOffset StartUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndUtc { get; init; }
    public bool UsedTypedChat { get; set; }
    public bool UsedChainedVoice { get; set; }
    public bool UsedRealtime { get; set; }
    public bool UsedManualPlayVoice { get; set; }
    public bool UsedAutoPlayVoice { get; set; }
    public int TypedUserTurns { get; set; }
    public int ChainedVoiceUserTurns { get; set; }
    public int RealtimeUserTurns { get; set; }
    public int ValidLearnerTurns { get; set; }
    public int InvalidTranscriptRetries { get; set; }
    public int AssistantTurns { get; set; }
    public int TotalUserTranscriptCharacters { get; set; }
    public int TotalAssistantTranscriptCharacters { get; set; }
    public long TotalTtsInputCharacters { get; set; }
    public long TotalTtsOutputBytes { get; set; }
    public long TotalNormalTranscriptionAudioBytes { get; set; }
    public double EstimatedNormalTranscriptionAudioDurationSeconds { get; set; }
    public long TotalRealtimeInputAudioBytes { get; set; }
    public double EstimatedRealtimeInputAudioDurationSeconds { get; set; }
    public long TotalRealtimeOutputAudioBytes { get; set; }
    public double EstimatedRealtimeOutputAudioDurationSeconds { get; set; }
    public string LessonChatModel { get; set; } = string.Empty;
    public string FeedbackModel { get; set; } = string.Empty;
    public string SummaryModel { get; set; } = string.Empty;
    public string TranscriptionModel { get; set; } = string.Empty;
    public string TtsModel { get; set; } = string.Empty;
    public string RealtimeModel { get; set; } = string.Empty;
    public OpenAiCallUsageMetrics OpenAiUsage { get; set; } = new();
}

public sealed class OpenAiCallUsageMetrics
{
    public string Operation { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string ResponseId { get; init; } = string.Empty;
    public long? InputTokens { get; init; }
    public long? OutputTokens { get; init; }
    public long? TotalTokens { get; init; }
    public long? CachedInputTokens { get; init; }
    public long? AudioInputTokens { get; init; }
    public long? AudioOutputTokens { get; init; }
    public bool HasExactUsage => InputTokens.HasValue || OutputTokens.HasValue || TotalTokens.HasValue || AudioInputTokens.HasValue || AudioOutputTokens.HasValue;
}

public sealed class AudioUsageMetrics
{
    public string Operation { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string Voice { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public long InputCharacters { get; init; }
    public long OutputBytes { get; init; }
    public long InputAudioBytes { get; init; }
    public double? EstimatedDurationSeconds { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class RealtimeUsageMetrics
{
    public string SessionId { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string Voice { get; init; } = string.Empty;
    public string InputTranscriptionModel { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public long TotalInputAudioBytes { get; set; }
    public long TotalCommittedAudioBytes { get; set; }
    public int AudioCommits { get; set; }
    public int UserTranscriptCharacters { get; set; }
    public int AssistantTranscriptCharacters { get; set; }
    public long AssistantAudioBytes { get; set; }
    public long? FirstAudioDeltaMs { get; set; }
    public long? ResponseCompleteMs { get; set; }
    public string DisconnectReason { get; set; } = string.Empty;
    public OpenAiCallUsageMetrics Usage { get; set; } = new();
}

public sealed class LessonCostEstimate
{
    public decimal NormalChatUsd { get; init; }
    public decimal TtsUsd { get; init; }
    public decimal TranscriptionUsd { get; init; }
    public decimal RealtimeUsd { get; init; }
    public decimal TotalUsd => NormalChatUsd + TtsUsd + TranscriptionUsd + RealtimeUsd;
    public bool IsComplete { get; init; }
    public bool IsApproximate { get; init; } = true;
    public IReadOnlyList<string> MissingFields { get; init; } = [];
}

public sealed class CostEstimationOptions
{
    public decimal ChatTextInputPerMillionTokensUsd { get; init; } = PricingConstants.OpenAi.ChatTextInputPerMillionTokensUsd;
    public decimal ChatTextOutputPerMillionTokensUsd { get; init; } = PricingConstants.OpenAi.ChatTextOutputPerMillionTokensUsd;
    public decimal TranscriptionPerMinuteUsd { get; init; } = PricingConstants.OpenAi.TranscriptionPerMinuteUsd;
    public decimal Tts1PerMillionCharactersUsd { get; init; } = PricingConstants.OpenAi.Tts1PerMillionCharactersUsd;
    public decimal RealtimeTextInputPerMillionTokensUsd { get; init; } = PricingConstants.OpenAi.RealtimeTextInputPerMillionTokensUsd;
    public decimal RealtimeTextOutputPerMillionTokensUsd { get; init; } = PricingConstants.OpenAi.RealtimeTextOutputPerMillionTokensUsd;
    public decimal RealtimeAudioInputPerMillionTokensUsd { get; init; } = PricingConstants.OpenAi.RealtimeAudioInputPerMillionTokensUsd;
    public decimal RealtimeAudioOutputPerMillionTokensUsd { get; init; } = PricingConstants.OpenAi.RealtimeAudioOutputPerMillionTokensUsd;
}

public static class PricingConstants
{
    public static class OpenAi
    {
        // Developer placeholder values only. Update manually from the OpenAI pricing page before enabling exact estimates.
        public const decimal TranscriptionPerMinuteUsd = 0m;
        public const decimal Tts1PerMillionCharactersUsd = 0m;
        public const decimal RealtimeTextInputPerMillionTokensUsd = 0m;
        public const decimal RealtimeTextOutputPerMillionTokensUsd = 0m;
        public const decimal RealtimeAudioInputPerMillionTokensUsd = 0m;
        public const decimal RealtimeAudioOutputPerMillionTokensUsd = 0m;
        public const decimal ChatTextInputPerMillionTokensUsd = 0m;
        public const decimal ChatTextOutputPerMillionTokensUsd = 0m;
    }
}
