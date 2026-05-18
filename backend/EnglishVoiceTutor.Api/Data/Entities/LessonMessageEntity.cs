namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class LessonMessageEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public bool IsValidLessonTurn { get; set; }
    public string StudyLanguage { get; set; } = string.Empty;
    public decimal? TranscriptConfidence { get; set; }
    public int? AudioDurationMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public LessonSessionEntity Session { get; set; } = null!;
    public ICollection<FeedbackResultEntity> FeedbackResults { get; set; } = [];
}
