namespace EnglishVoiceTutor.Desktop.Models;

public sealed class UpsertBackendLessonSummaryRequest
{
    public string Summary { get; init; } = string.Empty;
    public string? Strengths { get; init; }
    public string? Improvements { get; init; }
    public string? Vocabulary { get; init; }
    public string? Grammar { get; init; }
    public string? NextSteps { get; init; }
}
