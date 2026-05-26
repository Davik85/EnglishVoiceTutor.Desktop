namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminUserDailyUsageCounterSnapshot
{
    public DateOnly UsageDate { get; set; }
    public string StudyLanguage { get; set; } = string.Empty;
    public int LessonsStarted { get; set; }
    public int LessonsCompleted { get; set; }
    public int ChatReplyCount { get; set; }
    public int HintsUsed { get; set; }
    public int FeedbackRequests { get; set; }
    public int TranscriptionSeconds { get; set; }
    public int TtsSeconds { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
