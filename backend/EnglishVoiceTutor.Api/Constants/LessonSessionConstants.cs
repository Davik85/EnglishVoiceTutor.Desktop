namespace EnglishVoiceTutor.Api.Constants;

public static class LessonSessionConstants
{
    public const string ActiveStatus = "Active";
    public const string FinishedStatus = "Finished";
    public const string AbandonedStatus = "Abandoned";

    public const string TextMode = "text";
    public const string NormalVoiceMode = "normal_voice";
    public const string ConversationMode = "conversation_mode";
    public const string RealtimeFutureMode = "realtime_future";

    public const decimal DefaultEstimatedCost = 0m;
    public const int MaxRecentSessions = 50;
    public const int LessonSessionHeartbeatIntervalSeconds = 30;
    public const int ActiveLessonHeartbeatFreshnessMinutes = 2;

    public static readonly TimeSpan LessonSessionHeartbeatInterval = TimeSpan.FromSeconds(LessonSessionHeartbeatIntervalSeconds);
    public static readonly TimeSpan ActiveLessonHeartbeatFreshness = TimeSpan.FromMinutes(ActiveLessonHeartbeatFreshnessMinutes);

    public static readonly string[] ActiveStatuses =
    [
        ActiveStatus
    ];

    public static readonly string[] SupportedModes =
    [
        TextMode,
        NormalVoiceMode,
        ConversationMode,
        RealtimeFutureMode
    ];

    public static bool IsSupportedMode(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && SupportedModes.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static string ToCanonicalMode(string value)
    {
        return SupportedModes.First(mode => string.Equals(mode, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
