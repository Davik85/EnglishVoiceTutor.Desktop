namespace EnglishVoiceTutor.Api.Contracts.LessonHistory;

public sealed record LessonHistorySummaryResponse(
    Guid Id,
    string Summary,
    string? Strengths,
    string? Improvements,
    string? Vocabulary,
    string? Grammar,
    string? NextSteps,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
