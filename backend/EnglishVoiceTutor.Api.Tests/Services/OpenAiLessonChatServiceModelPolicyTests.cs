using EnglishVoiceTutor.Api.Services;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class OpenAiLessonChatServiceModelPolicyTests
{
    [Fact]
    public void Gpt55LessonTutorChatModelDoesNotSendTemperature()
    {
        Assert.Null(OpenAiLessonChatService.ResolveTemperature("gpt-5.5"));
        Assert.Null(OpenAiLessonChatService.ResolveTemperature("gpt-5.5-2026-06-01"));
        Assert.Equal(0.3, OpenAiLessonChatService.ResolveTemperature("gpt-5.2"));
    }
}
