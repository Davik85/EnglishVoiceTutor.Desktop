using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonChatService
{
    Task<LessonChatResponse> CreateReplyAsync(
        LessonChatRequest request,
        CancellationToken cancellationToken = default);

    async Task<FeedbackDto> CreateFeedbackAsync(
        LessonChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await CreateReplyAsync(request, cancellationToken);
        return response.Feedback;
    }
}
