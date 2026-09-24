using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class TutorIdentityGuardTests
{
    [Theory]
    [InlineData("I am looking for a software developer role.", "I am looking for a software developer role.")]
    [InlineData("I am interested in this role because I want to gain more experience.", "I am interested in this role because I want to gain more experience.")]
    [InlineData("Good. You can say, \"I am looking for a software developer role.\" Why are you interested in this role?", "Good. You can say, \"I am looking for a software developer role.\" Why are you interested in this role?")]
    [InlineData("Good reason. You can say, \"I am interested in this role because I want to gain more experience and work on interesting projects.\"", "Good reason. You can say, \"I am interested in this role because I want to gain more experience and work on interesting projects.\"")]
    [InlineData("Hello, I'm David. Nice to meet you.", "Hello, I'm Lana. Nice to meet you.")]
    [InlineData("hello, i'm David.", "hello, i'm Lana.")]
    [InlineData("Hello, I'm Lana.", "Hello, I'm Lana.")]
    [InlineData("I am ready.", "I am ready.")]
    [InlineData("i am David.", "i am Lana.")]
    [InlineData("my name is David.", "my name is Lana.")]
    public void PreventWrongTutorSelfIntroductionOnlyReplacesProperNameCandidates(string input, string expected)
    {
        var response = new LessonChatResponse { BotReply = input };
        var tutor = new TutorAvatarProfile { DisplayName = "Lana" };
        var guard = new TutorIdentityGuard(NullLogger<TutorIdentityGuard>.Instance);

        var result = guard.PreventWrongTutorSelfIntroduction(response, tutor);

        Assert.Equal(expected, result.BotReply);
        if (expected == input)
        {
            Assert.Same(response, result);
        }
    }
}
