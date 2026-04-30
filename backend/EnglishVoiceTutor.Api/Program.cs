using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = ApiConstants.HealthOkStatus,
        service = ApiConstants.ServiceName
    });
});

app.MapPost("/api/lesson-chat/mock-reply", (LessonChatRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.UserMessage))
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyUserMessageError
        });
    }

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

    return Results.Ok(response);
});

app.Run();
