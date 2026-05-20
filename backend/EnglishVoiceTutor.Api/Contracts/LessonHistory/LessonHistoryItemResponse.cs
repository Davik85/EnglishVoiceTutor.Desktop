namespace EnglishVoiceTutor.Api.Contracts.LessonHistory;

public sealed record LessonHistoryItemResponse(
    Guid SessionId,
    string LessonContentId,
    string StudyLanguage,
    string TopicTitle,
    string SubtopicTitle,
    string Level,
    string? SelectedContextTitle,
    string ModeUsed,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int ValidTurnCount,
    decimal EstimatedCost,
    bool HasSummary,
    string? SummaryPreview,
    int MessageCount,
    DateTimeOffset UpdatedAt);
