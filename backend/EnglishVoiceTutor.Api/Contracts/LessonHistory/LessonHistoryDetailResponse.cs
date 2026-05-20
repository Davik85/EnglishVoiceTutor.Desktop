namespace EnglishVoiceTutor.Api.Contracts.LessonHistory;

public sealed record LessonHistoryDetailResponse(
    Guid SessionId,
    Guid UserId,
    string LessonContentId,
    string StudyLanguage,
    string TopicId,
    string TopicTitle,
    string SubtopicId,
    string SubtopicTitle,
    string Level,
    string? SelectedContextId,
    string? SelectedContextTitle,
    string ModeUsed,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int ValidTurnCount,
    decimal EstimatedCost,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    LessonHistorySummaryResponse? Summary,
    IReadOnlyList<LessonHistoryMessageResponse> Messages);
