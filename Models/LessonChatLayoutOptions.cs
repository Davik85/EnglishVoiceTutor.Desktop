using System.Windows.Media;

namespace EnglishVoiceTutor.Desktop.Models;

public static class LessonChatLayoutOptions
{
    public const double NormalAvatarFrameMaxWidth = 360;
    public const double NormalAvatarFrameHeight = 500;

    public const double ConversationAvatarFrameMaxWidth = 520;
    public const double ConversationAvatarFrameHeight = 560;

    public static Stretch NormalAvatarStretchMode { get; } = Stretch.UniformToFill;

    // Conversation Mode uses UniformToFill so each tutor GIF fills the smaller frame without gray side bars.
    public static Stretch ConversationAvatarStretchMode { get; } = Stretch.UniformToFill;
}
