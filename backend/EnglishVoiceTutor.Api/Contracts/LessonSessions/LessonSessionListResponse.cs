namespace EnglishVoiceTutor.Api.Contracts.LessonSessions;

public sealed record LessonSessionListResponse(IReadOnlyList<LessonSessionResponse> Items);
