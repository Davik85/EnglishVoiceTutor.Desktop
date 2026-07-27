namespace EnglishVoiceTutor.Api.Services.Billing;

public enum GooglePlayPurchaseClaimResultCode
{
    Claimed,
    AlreadyOwned,
    OwnershipConflict,
    InvalidInput,
    TemporarilyUnavailable
}

public sealed record GooglePlayPurchaseClaimResult(GooglePlayPurchaseClaimResultCode Code);
