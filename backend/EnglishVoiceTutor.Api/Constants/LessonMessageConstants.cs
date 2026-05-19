namespace EnglishVoiceTutor.Api.Constants;

public static class LessonMessageConstants
{
    public const string User = "user";
    public const string Assistant = "assistant";
    public const string System = "system";

    public const string Typed = "typed";
    public const string VoiceTranscript = "voice_transcript";
    public const string BotReply = "bot_reply";
    public const string Hint = "hint";
    public const string Setup = "setup";
    public const string ContextSelection = "context_selection";
    public const string Summary = "summary";

    public const int MinTurnNumber = 0;
    public const bool DefaultIsValidLessonTurn = false;

    public static readonly string[] SupportedRoles =
    [
        User,
        Assistant,
        System
    ];

    public static readonly string[] SupportedSources =
    [
        Typed,
        VoiceTranscript,
        BotReply,
        Hint,
        Setup,
        ContextSelection,
        Summary
    ];

    public static bool IsSupportedRole(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && SupportedRoles.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsSupportedSource(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && SupportedSources.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static string ToCanonicalRole(string value)
    {
        return SupportedRoles.First(role => string.Equals(role, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static string ToCanonicalSource(string value)
    {
        return SupportedSources.First(source => string.Equals(source, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
