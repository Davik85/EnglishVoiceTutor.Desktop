namespace EnglishVoiceTutor.Api.Contracts.Usage;

public sealed class FreeLimitStatusResponse
{
    public Guid UserId { get; set; }
    public DateOnly UsageDate { get; set; }
    public string StudyLanguage { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;

    public int ChatReplyCount { get; set; }
    public int ChatReplyLimit { get; set; }
    public int ChatReplyRemaining { get; set; }
    public bool ChatReplyLimitExceeded { get; set; }

    public int HintsUsed { get; set; }
    public int HintLimit { get; set; }
    public int HintRemaining { get; set; }
    public bool HintLimitExceeded { get; set; }

    public int TranscriptionSeconds { get; set; }
    public int TranscriptionSecondsLimit { get; set; }
    public int TranscriptionSecondsRemaining { get; set; }
    public bool TranscriptionLimitExceeded { get; set; }

    public int TtsSeconds { get; set; }
    public int TtsSecondsLimit { get; set; }
    public int TtsSecondsRemaining { get; set; }
    public bool TtsLimitExceeded { get; set; }

    public decimal EstimatedCost { get; set; }
    public decimal EstimatedCostLimit { get; set; }
    public decimal EstimatedCostRemaining { get; set; }
    public bool EstimatedCostLimitExceeded { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? CounterUpdatedAt { get; set; }
    public DateTimeOffset CheckedAtUtc { get; set; }
    public string Source { get; set; } = string.Empty;
}
