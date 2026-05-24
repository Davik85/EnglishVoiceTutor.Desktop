using EnglishVoiceTutor.Api.Contracts.LessonMessages;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonMessageService
{
    Task<LessonMessageResponse> CreateLessonMessageAsync(Guid sessionId, CreateLessonMessageRequest request, CancellationToken cancellationToken);
    Task<LessonMessageListResponse> GetLessonMessagesAsync(Guid sessionId, CancellationToken cancellationToken);
}
