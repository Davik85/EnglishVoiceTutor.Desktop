namespace EnglishVoiceTutor.Desktop.Models;

public sealed class LessonSummaryInput
{
    public string SelectedLevel { get; init; } = string.Empty;
    public string TopicTitle { get; init; } = string.Empty;
    public string SubtopicTitle { get; init; } = string.Empty;
    public string SelectedContextTitle { get; init; } = string.Empty;
    public string SelectedContextVariantId { get; init; } = string.Empty;
    public string LearningGoal { get; init; } = string.Empty;
    public string TargetLanguage { get; init; } = "English";
    public string LessonType { get; init; } = string.Empty;
    public int FinalUserTurnCount { get; init; }
    public IReadOnlyList<LessonSummaryMessage> Messages { get; init; } = [];
}

public sealed class LessonSummaryMessage
{
    public int Id { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public int LessonTurnNumber { get; init; }
    public string LessonPhase { get; init; } = string.Empty;
    public Feedback? Feedback { get; init; }
}
