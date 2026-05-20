namespace EnglishVoiceTutor.Api.Contracts.LessonHistory;

public sealed record LessonHistoryMessageResponse(
    Guid Id,
    string Role,
    string Text,
    string Source,
    int TurnNumber,
    bool IsValidLessonTurn,
    string StudyLanguage,
    decimal? TranscriptConfidence,
    int? AudioDurationMs,
    DateTimeOffset CreatedAt);
