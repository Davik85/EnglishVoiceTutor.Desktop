using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonHintService
{
    Task<LessonHintResponse> CreateHintAsync(LessonChatRequest request, CancellationToken cancellationToken = default);
}
