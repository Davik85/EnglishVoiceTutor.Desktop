namespace EnglishVoiceTutor.Api.Contracts.LessonSummaries;

public sealed record LessonSummaryListResponse(IReadOnlyList<LessonSummaryResponse> Items);
