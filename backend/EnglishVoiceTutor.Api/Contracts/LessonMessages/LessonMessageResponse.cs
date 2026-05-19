namespace EnglishVoiceTutor.Api.Contracts.LessonMessages;

public sealed record LessonMessageResponse(
    Guid Id,
    Guid SessionId,
    string Role,
    string Text,
    string Source,
    int TurnNumber,
    bool IsValidLessonTurn,
    string StudyLanguage,
    decimal? TranscriptConfidence,
    int? AudioDurationMs,
    DateTimeOffset CreatedAt);
