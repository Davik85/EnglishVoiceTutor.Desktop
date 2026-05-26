namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminFreeLessonAllowanceResetResponse
{
    public Guid UserId { get; init; }
    public string UsageDate { get; init; } = string.Empty;
    public bool ResetApplied { get; init; }
    public Guid RemovedDailyFreeLessonUsageId { get; init; }
    public Guid LessonSessionId { get; init; }
    public string StudyLanguage { get; init; } = string.Empty;
    public DateTimeOffset ConsumedAtUtc { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset ResetAtUtc { get; init; }
    public bool AuditWritten { get; init; }
}
