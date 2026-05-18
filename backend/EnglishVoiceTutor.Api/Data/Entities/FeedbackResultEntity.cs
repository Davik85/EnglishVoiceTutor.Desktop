namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class FeedbackResultEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid MessageId { get; set; }
    public string FeedbackType { get; set; } = string.Empty;
    public string? CorrectedText { get; set; }
    public string? Explanation { get; set; }
    public string? GrammarTip { get; set; }
    public string? VocabularyTip { get; set; }
    public string? CultureTip { get; set; }
    public string? Praise { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public LessonSessionEntity Session { get; set; } = null!;
    public LessonMessageEntity Message { get; set; } = null!;
}
