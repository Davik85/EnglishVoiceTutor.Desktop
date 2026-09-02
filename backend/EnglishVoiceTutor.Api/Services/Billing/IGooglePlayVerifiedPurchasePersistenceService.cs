namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayVerifiedPurchasePersistenceService
{
    Task<GooglePlayVerifiedPurchasePersistenceResult> PersistAsync(GooglePlayVerifiedPurchasePersistenceRequest request, CancellationToken cancellationToken);
    Task UpdateAcknowledgementStateAsync(string purchaseToken, bool acknowledgementPending, string? safeResultCode, CancellationToken cancellationToken);
}

public sealed record GooglePlayVerifiedPurchasePersistenceRequest(
    Guid UserId,
    string PurchaseToken,
    GooglePlayVerifiedPurchase VerifiedPurchase,
    string ProtectedPurchaseToken,
    bool IsAuthoritativeTrialDeferralPersistence = false);
public enum GooglePlayVerifiedPurchasePersistenceResultCode { Applied, AlreadyCurrent, InvalidInput, OwnershipConflict, ProductMismatch, ConsistencyConflict, TestPurchaseNotSupported, TemporarilyUnavailable }
public sealed record GooglePlayVerifiedPurchasePersistenceResult(GooglePlayVerifiedPurchasePersistenceResultCode Code);
