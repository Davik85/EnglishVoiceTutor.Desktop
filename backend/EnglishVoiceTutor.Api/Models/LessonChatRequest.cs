namespace EnglishVoiceTutor.Api.Models;

public sealed class LessonChatRequest
{
    public string SelectedLevel { get; init; } = string.Empty;
    public string TopicTitle { get; init; } = string.Empty;
    public string SubtopicTitle { get; init; } = string.Empty;
    public string UserMessage { get; init; } = string.Empty;
    public string LastBotMessage { get; init; } = string.Empty;
    public string NativeLanguageName { get; init; } = string.Empty;
    public string TutorAvatarId { get; init; } = string.Empty;
    public string UserDisplayName { get; init; } = string.Empty;
    public string LearningGoal { get; init; } = string.Empty;
    public int LearnerTurnCount { get; init; }
    public int SoftLearnerTurnLimit { get; init; }
    public int HardLearnerTurnLimit { get; init; }
    public int RemainingLearnerTurns { get; init; }
    public bool ShouldStartWrappingUp { get; init; }
    public bool ShouldEndLessonNow { get; init; }
    public IReadOnlyList<RecentConversationMessage> RecentMessages { get; init; } = [];
}
