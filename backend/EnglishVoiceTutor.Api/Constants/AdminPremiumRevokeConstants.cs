namespace EnglishVoiceTutor.Api.Constants;

public static class AdminPremiumRevokeConstants
{
    public const string ReasonFieldName = "reason";

    public const string ReasonRequiredError = "Reason is required.";
    public const string ReasonTooLongError = "Reason exceeds the maximum allowed length.";
    public const string TargetUserNotFoundError = "Target user was not found.";
    public const string EntitlementNotFoundError = "Manual Premium entitlement was not found.";
    public const string EntitlementNotRevokableError = "Only active manual Premium entitlements can be revoked.";
    public const string AuthenticatedAdminUserNotFoundError = "Authenticated admin user id was not found.";

    public static class MetadataKeys
    {
        public const string EntitlementId = "entitlementId";
        public const string PreviousStatus = "previousStatus";
        public const string NewStatus = "newStatus";
        public const string PreviousExpiresAtUtc = "previousExpiresAtUtc";
        public const string RevokedAtUtc = "revokedAtUtc";
        public const string Source = "source";
    }
}
