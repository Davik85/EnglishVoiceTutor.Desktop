using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class MockLessonChatService : ILessonChatService
{
    // TODO: Replace this mock implementation with an OpenAI-backed backend service in a future step.
    public Task<LessonChatResponse> CreateReplyAsync(
        LessonChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var shouldEndLessonNow = LessonLimitHelper.ShouldEndLessonNow(request);
        var shouldStartWrappingUp = LessonLimitHelper.ShouldStartWrappingUp(request);
        var response = new LessonChatResponse
        {
            BotReply = shouldEndLessonNow
                ? $"Great work today. We'll stop this lesson here. You practiced {request.SubtopicTitle}. You can come back later to repeat it or choose another topic."
                : shouldStartWrappingUp
                    ? $"Great. We have only a few turns left in this lesson, so let's practice one more useful phrase about {request.SubtopicTitle}."
                    : ApiConstants.MockBotReplyText,
            IsLessonComplete = shouldEndLessonNow,
            Feedback = new FeedbackDto
            {
                ShortText = "Good start. Here is a more natural version.",
                CorrectedVersion = "Yes, I am ready.",
                GrammarTip = "Use a full sentence when you want to sound clearer and more confident.",
                VocabularyTip = "Short answers are understandable, but complete phrases sound more natural in practice.",
                CultureTip = "In everyday conversation, a friendly full answer helps keep the dialogue going.",
                NaturalVersion = "Yes, I am ready. Let's start."
            }
        };

        return Task.FromResult(response);
    }
}
