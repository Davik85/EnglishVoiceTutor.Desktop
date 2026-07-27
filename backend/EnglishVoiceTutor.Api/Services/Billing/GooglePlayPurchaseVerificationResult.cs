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

public sealed record GooglePlayVerifiedPurchase(
    string ProductId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    GooglePlayPurchaseAcknowledgementState AcknowledgementState,
    bool IsTestPurchase);

public sealed record GooglePlayPurchaseVerificationResult(
    GooglePlayPurchaseVerificationResultCode Code,
    GooglePlayVerifiedPurchase? VerifiedPurchase = null);
