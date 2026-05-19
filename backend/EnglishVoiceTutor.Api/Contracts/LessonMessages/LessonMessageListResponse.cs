namespace EnglishVoiceTutor.Api.Contracts.LessonMessages;

public sealed record LessonMessageListResponse(IReadOnlyList<LessonMessageResponse> Items);
