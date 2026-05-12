namespace EnglishVoiceTutor.Api.Models;

public sealed class LessonChatResponse
{
    public string BotReply { get; init; } = string.Empty;
    public FeedbackDto Feedback { get; init; } = new();
    public bool IsLessonComplete { get; init; }
}
