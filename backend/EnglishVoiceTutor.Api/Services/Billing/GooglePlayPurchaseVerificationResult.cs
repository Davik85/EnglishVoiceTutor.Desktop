namespace EnglishVoiceTutor.Api.Services.Billing;

public enum GooglePlayPurchaseVerificationResultCode
{
    Verified,
    Pending,
    AlreadyProcessed,
    InvalidPurchase,
    UnsupportedProduct,
    OwnershipConflict,
    TemporarilyUnavailable,
    NotConfigured
}

public sealed record GooglePlayPurchaseVerificationResult(GooglePlayPurchaseVerificationResultCode Code);
