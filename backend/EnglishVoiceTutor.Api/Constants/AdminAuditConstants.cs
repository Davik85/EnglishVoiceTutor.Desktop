namespace EnglishVoiceTutor.Api.Constants;

public static class AdminAuditConstants
{
    public static class ActionTypes
    {
        public const string ManualPremiumGrant = "manual_premium_grant";
        public const string ManualPremiumRevoke = "manual_premium_revoke";
        public const string FreeLessonAllowanceReset = "free_lesson_allowance_reset";
        public const string AdminBillingCancelRenewalCompleted = "admin_billing_cancel_renewal_completed";
    }

    public static class ValidationErrors
    {
        public const string AdminUserIdRequiredError = "Admin user id is required.";
        public const string TargetUserIdRequiredError = "Target user id is required.";
        public const string ActionTypeRequiredError = "Action type is required.";
        public const string ReasonRequiredError = "Reason is required.";
        public const string ActionTypeTooLongError = "Action type exceeds the maximum allowed length.";
        public const string ReasonTooLongError = "Reason exceeds the maximum allowed length.";
        public const string SafeMetadataTooLongError = "Safe metadata exceeds the maximum allowed length.";
    }
}
