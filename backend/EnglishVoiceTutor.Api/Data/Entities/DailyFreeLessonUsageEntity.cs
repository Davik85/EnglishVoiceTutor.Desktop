namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class DailyFreeLessonUsageEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly UsageDate { get; set; }
    public string StudyLanguage { get; set; } = string.Empty;
    public Guid LessonSessionId { get; set; }
    public int UserMessageCountAtConsumption { get; set; }
    public DateTimeOffset ConsumedAtUtc { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public LessonSessionEntity LessonSession { get; set; } = null!;
}
