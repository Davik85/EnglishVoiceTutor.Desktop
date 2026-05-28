namespace EnglishVoiceTutor.Api.Data;

public static class EntityConstants
{
    public static class TableNames
    {
        public const string Users = "users";
        public const string UserProfiles = "user_profiles";
        public const string UserSettings = "user_settings";
        public const string Lessons = "lessons";
        public const string LessonSessions = "lesson_sessions";
        public const string LessonMessages = "lesson_messages";
        public const string FeedbackResults = "feedback_results";
        public const string LessonSummaries = "lesson_summaries";
        public const string UsageEvents = "usage_events";
        public const string DailyUsageCounters = "daily_usage_counters";
        public const string Subscriptions = "subscriptions";
        public const string Payments = "payments";
        public const string Devices = "devices";
        public const string Plans = "plans";
        public const string Entitlements = "entitlements";
        public const string TrialGrants = "trial_grants";
        public const string DailyFreeLessonUsage = "daily_free_lesson_usage";
        public const string BillingEvents = "billing_events";
        public const string PaddleWebhookEvents = "paddle_webhook_events";
        public const string AdminActions = "admin_actions";
    }

    public static class Lengths
    {
        public const int EmailMaxLength = 320;
        public const int PasswordHashMaxLength = 512;
        public const int StatusMaxLength = 64;
        public const int LanguageCodeMaxLength = 64;
        public const int DisplayNameMaxLength = 160;
        public const int TimezoneMaxLength = 128;
        public const int TutorIdMaxLength = 80;
        public const int LessonContentIdMaxLength = 128;
        public const int TopicIdMaxLength = 128;
        public const int TopicTitleMaxLength = 256;
        public const int SubtopicIdMaxLength = 128;
        public const int SubtopicTitleMaxLength = 256;
        public const int ContextIdMaxLength = 128;
        public const int ContextTitleMaxLength = 256;
        public const int LevelMaxLength = 64;
        public const int ModeMaxLength = 64;
        public const int ProviderMaxLength = 80;
        public const int ExternalIdMaxLength = 256;
        public const int OperationMaxLength = 80;
        public const int ModelMaxLength = 128;
        public const int RoleMaxLength = 64;
        public const int SourceMaxLength = 64;
        public const int FeedbackTypeMaxLength = 80;
        public const int PlanIdMaxLength = 128;
        public const int CurrencyMaxLength = 3;
        public const int PlatformMaxLength = 80;
        public const int DeviceNameMaxLength = 160;
        public const int AppVersionMaxLength = 80;
        public const int ShortTextMaxLength = 512;
        public const int MediumTextMaxLength = 4096;
        public const int LongTextMaxLength = 20000;
        public const int PlanDisplayNameMaxLength = 128;
        public const int PlanTierMaxLength = 64;
        public const int EntitlementTypeMaxLength = 128;
        public const int EntitlementSourceMaxLength = 64;
        public const int EntitlementReasonMaxLength = 512;
        public const int BillingEventTypeMaxLength = 128;
        public const int PaddleWebhookSignatureHeaderMaxLength = 1024;
        public const int ProviderEventIdMaxLength = 256;
        public const int DeviceFingerprintHashMaxLength = 256;
        public const int MetadataJsonMaxLength = 4096;
        public const int ErrorMessageMaxLength = 1024;
        public const int ActionTypeMaxLength = 128;
    }

    public static class Precision
    {
        public const int MoneyPrecision = 18;
        public const int MoneyScale = 2;
        public const int CostPrecision = 18;
        public const int CostScale = 6;
        public const int SpeechSpeedPrecision = 5;
        public const int SpeechSpeedScale = 2;
        public const int TranscriptConfidencePrecision = 5;
        public const int TranscriptConfidenceScale = 4;
    }
}
