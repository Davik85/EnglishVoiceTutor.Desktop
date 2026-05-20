namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendLessonHistoryItemResponse
{
    public Guid SessionId { get; set; }

    public string LessonContentId { get; set; } = string.Empty;

    public string StudyLanguage { get; set; } = string.Empty;

    public string TopicTitle { get; set; } = string.Empty;

    public string SubtopicTitle { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public string? SelectedContextTitle { get; set; }

    public string ModeUsed { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public int ValidTurnCount { get; set; }

    public decimal EstimatedCost { get; set; }

    public bool HasSummary { get; set; }

    public string? SummaryPreview { get; set; }

    public int MessageCount { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
