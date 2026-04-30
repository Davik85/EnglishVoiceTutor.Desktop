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
        var response = new LessonChatResponse
        {
            BotReply = ApiConstants.MockBotReplyText,
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
