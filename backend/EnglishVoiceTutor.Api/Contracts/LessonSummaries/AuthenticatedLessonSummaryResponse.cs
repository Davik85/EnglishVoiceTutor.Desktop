namespace EnglishVoiceTutor.Api.Contracts.LessonSummaries;

/// <summary>Safe, backend-owned lesson summary returned to authenticated learners.</summary>
public sealed record AuthenticatedLessonSummaryResponse(
    string Status,
    string? LessonContentId,
    string? StudyLanguage,
    string? TopicTitle,
    string? SubtopicTitle,
    string? Level,
    string? Summary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Improvements,
    IReadOnlyList<string> Vocabulary,
    IReadOnlyList<string> Grammar,
    IReadOnlyList<string> NextSteps,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
