namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendLessonSummaryResponse
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }
    public string LessonContentId { get; init; } = string.Empty;
    public string StudyLanguage { get; init; } = string.Empty;
    public string TopicTitle { get; init; } = string.Empty;
    public string SubtopicTitle { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string? Strengths { get; init; }
    public string? Improvements { get; init; }
    public string? Vocabulary { get; init; }
    public string? Grammar { get; init; }
    public string? NextSteps { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
