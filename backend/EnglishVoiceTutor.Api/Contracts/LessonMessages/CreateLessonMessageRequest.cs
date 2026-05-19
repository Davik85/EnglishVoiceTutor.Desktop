namespace EnglishVoiceTutor.Api.Contracts.LessonMessages;

public sealed record CreateLessonMessageRequest(
    string Role,
    string Text,
    string Source,
    int TurnNumber,
    bool IsValidLessonTurn,
    string StudyLanguage,
    decimal? TranscriptConfidence,
    int? AudioDurationMs);
