namespace EnglishVoiceTutor.Desktop.Models;

public sealed class LessonChatBackendResponse
{
    public string BotReply { get; init; } = string.Empty;

    public BackendFeedbackDto Feedback { get; init; } = new();

    public bool IsLessonComplete { get; init; }
}
