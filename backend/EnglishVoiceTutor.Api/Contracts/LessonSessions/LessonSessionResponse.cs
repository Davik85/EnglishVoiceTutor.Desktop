namespace EnglishVoiceTutor.Api.Contracts.LessonSessions;

public sealed record LessonSessionResponse(
    Guid Id,
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
    DateTimeOffset UpdatedAt);
