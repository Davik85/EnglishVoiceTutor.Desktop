namespace EnglishVoiceTutor.Desktop.Models;

public sealed class LessonChatBackendRequest
{
    public string SelectedLevel { get; init; } = string.Empty;

    public string TopicTitle { get; init; } = string.Empty;

    public string SubtopicTitle { get; init; } = string.Empty;

    public string UserMessage { get; init; } = string.Empty;

    public string LastBotMessage { get; init; } = string.Empty;

    public string NativeLanguageName { get; init; } = string.Empty;

    public string TutorAvatarId { get; init; } = string.Empty;

    public IReadOnlyList<RecentConversationMessage> RecentMessages { get; init; } = [];
}
