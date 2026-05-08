using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Constants;

public static class AvatarConstants
{
    private const string PackUriPrefix = "pack://application:,,,/";

    public const string IdleAnimationPath = "Assets/Avatars/avatar-idle.gif";
    public const string ListeningAnimationPath = "Assets/Avatars/avatar-listening.gif";
    public const string TranscribingAnimationPath = "Assets/Avatars/avatar-transcribing.gif";
    public const string ThinkingAnimationPath = "Assets/Avatars/avatar-thinking.gif";
    public const string SpeakingAnimationPath = "Assets/Avatars/avatar-speaking.gif";
    public const string FallbackImagePath = "Assets/Avatars/avatar-fallback.png";

    public const string IdleDisplayText = "Idle";
    public const string ListeningDisplayText = "Listening";
    public const string TranscribingDisplayText = "Transcribing";
    public const string ThinkingDisplayText = "Thinking";
    public const string SpeakingDisplayText = "Speaking";

    public static string GetAnimationPath(AvatarState state)
    {
        return state switch
        {
            AvatarState.Listening => ListeningAnimationPath,
            AvatarState.Transcribing => TranscribingAnimationPath,
            AvatarState.Thinking => ThinkingAnimationPath,
            AvatarState.Speaking => SpeakingAnimationPath,
            _ => IdleAnimationPath
        };
    }

    public static string GetDisplayText(AvatarState state)
    {
        return state switch
        {
            AvatarState.Listening => ListeningDisplayText,
            AvatarState.Transcribing => TranscribingDisplayText,
            AvatarState.Thinking => ThinkingDisplayText,
            AvatarState.Speaking => SpeakingDisplayText,
            _ => IdleDisplayText
        };
    }

    public static string ToPackUri(string assetPath)
    {
        return $"{PackUriPrefix}{assetPath}";
    }
}
