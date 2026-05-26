namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminUserLessonSessionSnapshot
{
    public Guid SessionId { get; set; }
    public Guid? LessonContentId { get; set; }
    public string StudyLanguage { get; set; } = string.Empty;
    public Guid? TopicId { get; set; }
    public string? TopicTitle { get; set; }
    public Guid? SubtopicId { get; set; }
    public string? SubtopicTitle { get; set; }
    public string? Level { get; set; }
    public Guid? SelectedContextId { get; set; }
    public string? SelectedContextTitle { get; set; }
    public string? ModeUsed { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int ValidTurnCount { get; set; }
    public decimal EstimatedCost { get; set; }
}
