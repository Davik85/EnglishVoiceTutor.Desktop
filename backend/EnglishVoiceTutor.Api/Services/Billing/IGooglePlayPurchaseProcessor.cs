namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayPurchaseProcessor
{
    Task<GooglePlayPurchaseProcessingResult> ProcessAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken);

    Task<GooglePlayPurchaseProcessingResult> ProcessAsync(
        Guid userId,
        string purchaseToken,
        GooglePlayPurchaseProcessingContext context,
        CancellationToken cancellationToken) => ProcessAsync(userId, purchaseToken, cancellationToken);
}

public sealed record GooglePlayPurchaseProcessingContext(bool ProviderConfirmedRevocation = false);

public enum GooglePlayPurchaseProcessingResultCode
{
    Verified,
    AcknowledgementPending,
    AcknowledgementInconsistent,
    TrialDeferralPending,
    TrialDeferralAmbiguous,
    Pending,
    InvalidPurchase,
    UnsupportedProduct,
    OwnershipConflict,
    NotConfigured,
    TemporarilyUnavailable
}

public sealed record GooglePlayPurchaseProcessingResult(GooglePlayPurchaseProcessingResultCode Code);
