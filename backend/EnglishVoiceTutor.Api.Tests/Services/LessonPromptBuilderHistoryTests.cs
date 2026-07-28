using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class LessonPromptBuilderHistoryTests
{
    [Fact]
    public void ResolveHistoryMessageLimitUsesBoundedLevelAwareRule()
    {
        Assert.Equal(33, LessonPromptBuilder.ResolveHistoryMessageLimit(15));
        Assert.Equal(43, LessonPromptBuilder.ResolveHistoryMessageLimit(20));
        Assert.Equal(53, LessonPromptBuilder.ResolveHistoryMessageLimit(25));
        Assert.Equal(67, LessonPromptBuilder.ResolveHistoryMessageLimit(32));
        Assert.Equal(10, LessonPromptBuilder.ResolveHistoryMessageLimit(0));
        Assert.Equal(70, LessonPromptBuilder.ResolveHistoryMessageLimit(100));
    }

    [Fact]
    public void BuildInputRetainsAllSixtySevenB2MessagesInChronologicalOrder()
    {
        var recentMessages = new List<RecentConversationMessage>
        {
            new() { Sender = "User", Text = "My name is David." },
            new() { Sender = "You", Text = "I am from Georgia." }
        };
        recentMessages.AddRange(Enumerable.Range(3, 65).Select(index => new RecentConversationMessage
        {
            Sender = index % 2 == 0 ? "Runtime Tutor" : "User",
            Text = $"B2 history {index}."
        }));

        var prompt = CreateBuilder().BuildInput(new LessonChatRequest
        {
            SelectedLevel = "B2",
            HardLearnerTurnLimit = 32,
            UserMessage = "This is learner turn thirty-one.",
            RecentMessages = recentMessages
        });
        var transcript = ExtractTranscript(prompt);

        Assert.Equal(67, TranscriptLines(transcript).Count);
        Assert.Contains("- User: My name is David.", transcript);
        Assert.Contains("- You: I am from Georgia.", transcript);
        Assert.True(transcript.IndexOf("My name is David.", StringComparison.Ordinal)
            < transcript.IndexOf("I am from Georgia.", StringComparison.Ordinal));
        Assert.True(transcript.IndexOf("I am from Georgia.", StringComparison.Ordinal)
            < transcript.IndexOf("B2 history 67.", StringComparison.Ordinal));
        Assert.Equal(1, CountOccurrences(prompt, "This is learner turn thirty-one."));
        Assert.Equal(1, CountOccurrences(prompt, AntiRepeatInstruction));
    }

    [Fact]
    public void BuildInputFiltersBeforeApplyingAbsoluteCapAndPreservesOrder()
    {
        var recentMessages = new List<RecentConversationMessage>
        {
            new() { Sender = "User", Text = "   " }
        };
        recentMessages.AddRange(Enumerable.Range(1, 75).Select(index => new RecentConversationMessage
        {
            Sender = index % 2 == 0 ? "Runtime Tutor" : "User",
            Text = $"history {index}"
        }));

        var transcript = ExtractTranscript(CreateBuilder().BuildInput(new LessonChatRequest
        {
            HardLearnerTurnLimit = 100,
            UserMessage = "current message",
            RecentMessages = recentMessages
        }));
        var lines = TranscriptLines(transcript);

        Assert.Equal(70, lines.Count);
        Assert.Equal("- Runtime Tutor: history 6", lines.First());
        Assert.Equal("- User: history 75", lines.Last());
        Assert.DoesNotContain("- User: history 1\r\n", transcript);
    }

    [Fact]
    public void BuildInputRemovesOnlyTrailingLearnerDuplicate()
    {
        var prompt = CreateBuilder().BuildInput(new LessonChatRequest
        {
            SelectedLevel = "A1",
            HardLearnerTurnLimit = 15,
            UserMessage = "same text",
            RecentMessages =
            [
                new RecentConversationMessage { Sender = "User", Text = "same text" },
                new RecentConversationMessage { Sender = "Runtime Tutor", Text = "Please continue." },
                new RecentConversationMessage { Sender = "Runtime Tutor", Text = "same text" },
                new RecentConversationMessage { Sender = "You", Text = "same text" }
            ]
        });
        var transcript = ExtractTranscript(prompt);

        Assert.Equal(3, CountOccurrences(prompt, "same text"));
        Assert.Equal(2, CountOccurrences(transcript, "same text"));
        Assert.Contains("- Runtime Tutor: same text", transcript);
        Assert.Contains("Learner latest message:\r\nsame text", prompt);
    }

    private const string TranscriptHeader = "Recent active lesson conversation context (oldest to newest):";
    private const string UserMessageHeader = "Learner latest message:";
    private const string AntiRepeatInstruction = "Use facts already supplied in the conversation";

    private static LessonPromptBuilder CreateBuilder() => new(
        new TutorAvatarProfileProvider(NullLogger<TutorAvatarProfileProvider>.Instance));

    private static string ExtractTranscript(string prompt)
    {
        var start = prompt.IndexOf(TranscriptHeader, StringComparison.Ordinal) + TranscriptHeader.Length;
        var end = prompt.IndexOf(UserMessageHeader, start, StringComparison.Ordinal);
        return prompt[start..end];
    }

    private static IReadOnlyList<string> TranscriptLines(string transcript) => transcript
        .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
        .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
        .ToArray();

    private static int CountOccurrences(string value, string expected)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(expected, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += expected.Length;
        }

        return count;
    }
}
