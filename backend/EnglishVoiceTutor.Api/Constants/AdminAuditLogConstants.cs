namespace EnglishVoiceTutor.Api.Constants;

public static class AdminAuditLogConstants
{
    public const int DefaultLimit = 50;
    public const int MinLimit = 1;
    public const int MaxLimit = 100;

    public const string LimitQueryKey = "limit";
    public const string LimitOutOfRangeError = "Limit must be between 1 and 100.";
    public const string TargetUserNotFoundError = "Target user was not found.";
}
