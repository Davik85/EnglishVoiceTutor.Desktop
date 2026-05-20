namespace EnglishVoiceTutor.Desktop.Models;

public sealed record TutorAvatarOption(
    string Id,
    string DisplayName,
    double ChatImageScale = 1.0,
    double ChatOffsetX = 0.0,
    double ChatOffsetY = 0.0,
    double ConversationImageScale = 1.0,
    double ConversationOffsetX = 0.0,
    double ConversationOffsetY = 0.0);
