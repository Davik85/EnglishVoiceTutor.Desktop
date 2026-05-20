namespace EnglishVoiceTutor.Api.Contracts.Usage;

public sealed class DailyUsageCounterResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateOnly UsageDate { get; init; }
    public string StudyLanguage { get; init; } = string.Empty;
    public int LessonsStarted { get; init; }
    public int LessonsCompleted { get; init; }
    public int HintsUsed { get; init; }
    public int FeedbackRequests { get; init; }
    public int TranscriptionSeconds { get; init; }
    public int TtsSeconds { get; init; }
    public decimal EstimatedCost { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
