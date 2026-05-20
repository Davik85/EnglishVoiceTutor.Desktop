namespace EnglishVoiceTutor.Api.Contracts.LessonHistory;

public sealed record LessonHistoryListResponse(IReadOnlyList<LessonHistoryItemResponse> Items);
