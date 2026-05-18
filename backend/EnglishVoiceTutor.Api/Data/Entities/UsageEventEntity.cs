namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class UsageEventEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? SessionId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? Model { get; set; }
    public long? InputTokens { get; set; }
    public long? OutputTokens { get; set; }
    public long? AudioInputTokens { get; set; }
    public long? AudioOutputTokens { get; set; }
    public int? AudioDurationMs { get; set; }
    public long? InputChars { get; set; }
    public long? OutputBytes { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public LessonSessionEntity? Session { get; set; }
}
