namespace EnglishVoiceTutor.Api.Constants;

public static class FreePlanLimitConstants
{
    public const string PlanId = "free_dev";
    public const string Source = "daily_usage_counters";

    public const int ChatReplyLimitPerDay = 20;
    public const int HintLimitPerDay = 5;
    public const int TranscriptionSecondsLimitPerDay = 300;
    public const int TtsSecondsLimitPerDay = 300;
    public const decimal EstimatedCostLimitPerDay = 0.25m;
}
