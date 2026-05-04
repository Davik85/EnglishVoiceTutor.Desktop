using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class MockLessonHintService : ILessonHintService
{
    public Task<LessonHintResponse> CreateHintAsync(LessonChatRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LessonHintResponse
        {
            HintText = ApiConstants.MockHintText
        });
    }
}
