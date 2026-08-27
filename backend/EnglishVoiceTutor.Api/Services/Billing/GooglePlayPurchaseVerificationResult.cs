namespace EnglishVoiceTutor.Api.Services.Billing;

public enum GooglePlayPurchaseVerificationResultCode
{
    Verified,
    Pending,
    InvalidPurchase,
    UnsupportedProduct,
    TemporarilyUnavailable,
    NotConfigured
}

public enum GooglePlayPurchaseAcknowledgementState
{
    Pending,
    Acknowledged
}

public enum GooglePlaySubscriptionLifecycleState
{
    Active,
    InGracePeriod,
    Canceled,
    OnHold,
    Paused,
    Expired,
    Revoked
}

public sealed record GooglePlayVerifiedPurchase(
    string PackageName,
    string ProductId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    GooglePlayPurchaseAcknowledgementState AcknowledgementState,
    bool IsTestPurchase,
    GooglePlaySubscriptionLifecycleState LifecycleState = GooglePlaySubscriptionLifecycleState.Active)
{
    internal string? LinkedPurchaseToken { get; init; }
    internal GooglePlayInitialPremiumDeferralEvidence? InitialPremiumDeferralEvidence { get; init; }
}

public sealed record GooglePlayPurchaseVerificationResult(
    GooglePlayPurchaseVerificationResultCode Code,
    GooglePlayVerifiedPurchase? VerifiedPurchase = null);
