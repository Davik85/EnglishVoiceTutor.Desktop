using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ILessonChatService, MockLessonChatService>();

var app = builder.Build();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = ApiConstants.HealthOkStatus,
        service = ApiConstants.ServiceName
    });
});

app.MapPost("/api/lesson-chat/mock-reply", async (LessonChatRequest request, ILessonChatService lessonChatService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.UserMessage))
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyUserMessageError
        });
    }

    var response = await lessonChatService.CreateReplyAsync(request, cancellationToken);

    return Results.Ok(response);
});

app.Run();
