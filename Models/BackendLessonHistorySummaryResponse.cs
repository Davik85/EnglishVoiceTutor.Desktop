namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendLessonHistorySummaryResponse
{
    public Guid Id { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string? Strengths { get; set; }

    public string? Improvements { get; set; }

    public string? Vocabulary { get; set; }

    public string? Grammar { get; set; }

    public string? NextSteps { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
