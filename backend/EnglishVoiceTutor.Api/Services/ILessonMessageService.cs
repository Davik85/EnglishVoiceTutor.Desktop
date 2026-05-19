using EnglishVoiceTutor.Api.Contracts.LessonMessages;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonMessageService
{
    Task<LessonMessageResponse> CreateDevLessonMessageAsync(Guid sessionId, CreateLessonMessageRequest request, CancellationToken cancellationToken);
    Task<LessonMessageListResponse> GetDevLessonMessagesAsync(Guid sessionId, CancellationToken cancellationToken);
}
