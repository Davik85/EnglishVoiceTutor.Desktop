namespace EnglishVoiceTutor.Api.Constants;

public static class AdminPremiumGrantConstants
{
    public const int MinDurationDays = 1;
    public const int MaxDurationDays = 365;

    public const string DurationDaysFieldName = "durationDays";
    public const string ReasonFieldName = "reason";

    public const string DurationDaysOutOfRangeError = "Duration days must be between 1 and 365.";
    public const string ReasonRequiredError = "Reason is required.";
    public const string ReasonTooLongError = "Reason exceeds the maximum allowed length.";
    public const string TargetUserNotFoundError = "Target user was not found.";
    public const string AuthenticatedAdminUserNotFoundError = "Authenticated admin user id was not found.";

    public static class MetadataKeys
    {
        public const string EntitlementId = "entitlementId";
        public const string DurationDays = "durationDays";
        public const string StartsAtUtc = "startsAtUtc";
        public const string ExpiresAtUtc = "expiresAtUtc";
        public const string Source = "source";
    }
}
