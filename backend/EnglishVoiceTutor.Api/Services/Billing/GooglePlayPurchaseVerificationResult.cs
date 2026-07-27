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

public sealed record GooglePlayVerifiedPurchase(string ProductId);

public sealed record GooglePlayPurchaseVerificationResult(
    GooglePlayPurchaseVerificationResultCode Code,
    GooglePlayVerifiedPurchase? VerifiedPurchase = null);
