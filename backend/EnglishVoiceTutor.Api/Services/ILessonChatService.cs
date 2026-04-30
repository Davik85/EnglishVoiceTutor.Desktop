using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonChatService
{
    Task<LessonChatResponse> CreateReplyAsync(
        LessonChatRequest request,
        CancellationToken cancellationToken = default);
}
