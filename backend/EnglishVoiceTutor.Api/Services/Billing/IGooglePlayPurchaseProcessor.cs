namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayPurchaseProcessor
{
    Task<GooglePlayPurchaseProcessingResult> ProcessAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken);
}

public enum GooglePlayPurchaseProcessingResultCode
{
    Verified,
    AcknowledgementPending,
    AcknowledgementInconsistent,
    Pending,
    InvalidPurchase,
    UnsupportedProduct,
    OwnershipConflict,
    NotConfigured,
    TemporarilyUnavailable
}

public sealed record GooglePlayPurchaseProcessingResult(GooglePlayPurchaseProcessingResultCode Code);
