namespace EnglishVoiceTutor.Api.Constants;

public static class SubscriptionConstants
{
    public static class Plans
    {
        public const string FreePlanId = "free";
        public const string PremiumPlanId = "premium";
        public const string FreePlanName = "Free";
        public const string PremiumPlanName = "Premium";
        public const string FreeTier = "free";
        public const string PremiumTier = "premium";
    }

    public static class SubscriptionStatuses
    {
        public const string None = "none";
        public const string Active = "active";
        public const string Trialing = "trialing";
        public const string PastDue = "past_due";
        public const string Canceled = "canceled";
        public const string Expired = "expired";
        public const string Paused = "paused";
    }

    public static class BillingProviders
    {
        public const string None = "none";
        public const string Paddle = "paddle";
        public const string AppleAppStore = "apple_app_store";
        public const string GooglePlay = "google_play";
        public const string Manual = "manual";
        public const string InternalTrial = "internal_trial";
    }

    public static class Entitlements
    {
        public const string PremiumAccessType = "premium_access";
        public const string SourceSubscription = "subscription";
        public const string SourceTrial = "trial";
        public const string SourceManualAdmin = "manual_admin";
        public const string SourceProviderEvent = "provider_event";
        public const string StatusActive = "active";
        public const string StatusInactive = "inactive";
        public const string StatusRevoked = "revoked";
        public const string StatusExpired = "expired";
    }

    public static class BillingEventStatuses
    {
        public const string Received = "received";
        public const string Processed = "processed";
        public const string Failed = "failed";
        public const string Ignored = "ignored";
    }

    public static class AdminActionTypes
    {
        public const string GrantPremium = "grant_premium";
        public const string RevokePremium = "revoke_premium";
        public const string ResetFreeLesson = "reset_free_lesson";
        public const string ForcePlanRefresh = "force_plan_refresh";
    }

    public const int PremiumTrialDays = 7;
    public const int FreeLessonsPerDay = 1;
    public const int FreeLessonConsumptionMessageThreshold = 3;
    public const bool EnforcementEnabled = false;
    public const string FreeLessonConsumptionRule = "A free lesson is consumed after a lesson session is started and the learner sends at least 3 user messages in that lesson session.";
}
