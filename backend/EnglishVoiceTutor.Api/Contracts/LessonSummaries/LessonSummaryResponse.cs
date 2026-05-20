namespace EnglishVoiceTutor.Api.Contracts.LessonSummaries;

public sealed record LessonSummaryResponse(
    Guid Id,
    Guid SessionId,
    Guid UserId,
    string LessonContentId,
    string StudyLanguage,
    string TopicTitle,
    string SubtopicTitle,
    string Level,
    string Summary,
    string? Strengths,
    string? Improvements,
    string? Vocabulary,
    string? Grammar,
    string? NextSteps,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
