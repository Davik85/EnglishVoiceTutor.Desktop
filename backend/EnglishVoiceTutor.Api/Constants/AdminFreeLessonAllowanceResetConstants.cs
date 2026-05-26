namespace EnglishVoiceTutor.Api.Constants;

public static class AdminFreeLessonAllowanceResetConstants
{
    public const string UsageDateFieldName = "usageDate";
    public const string ReasonFieldName = "reason";
    public const string UsageDateFormat = "yyyy-MM-dd";
    public const string ReasonRequiredError = "Reason is required.";
    public const string ReasonTooLongError = "Reason exceeds the maximum allowed length.";
    public const string UsageDateInvalidError = "Usage date must use yyyy-MM-dd format.";
    public const string TargetUserNotFoundError = "Target user was not found.";
    public const string DailyFreeLessonUsageNotFoundError = "Free lesson usage was not found for the selected date.";
    public const string AuthenticatedAdminUserNotFoundError = "Authenticated admin user id was not found.";

    public static class MetadataKeys
    {
        public const string RemovedDailyFreeLessonUsageId = "removedDailyFreeLessonUsageId";
        public const string UsageDate = "usageDate";
        public const string LessonSessionId = "lessonSessionId";
        public const string StudyLanguage = "studyLanguage";
        public const string ConsumedAtUtc = "consumedAtUtc";
        public const string ResetAtUtc = "resetAtUtc";
    }
}
