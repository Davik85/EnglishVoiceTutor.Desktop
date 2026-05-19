namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendLessonSessionResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string LessonContentId { get; set; } = string.Empty;
    public string StudyLanguage { get; set; } = string.Empty;
    public string TopicId { get; set; } = string.Empty;
    public string TopicTitle { get; set; } = string.Empty;
    public string SubtopicId { get; set; } = string.Empty;
    public string SubtopicTitle { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? SelectedContextId { get; set; }
    public string? SelectedContextTitle { get; set; }
    public string ModeUsed { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int ValidTurnCount { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
