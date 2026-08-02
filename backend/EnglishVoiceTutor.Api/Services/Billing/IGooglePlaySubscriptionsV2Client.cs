namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlaySubscriptionsV2Client
{
    Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken);
    Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken);
}

public sealed record GooglePlaySubscriptionV2Snapshot(
    string? SubscriptionState,
    DateTimeOffset? StartTimeUtc,
    IReadOnlyList<GooglePlaySubscriptionLineItemSnapshot> LineItems,
    GooglePlayPurchaseAcknowledgementState? AcknowledgementState,
    bool IsTestPurchase,
    bool HasLinkedPurchaseToken);

public sealed record GooglePlaySubscriptionLineItemSnapshot(
    string? ProductId,
    DateTimeOffset? ExpiryTimeUtc);

public enum GooglePlaySubscriptionsV2ClientFailure
{
    InvalidPurchase,
    TemporarilyUnavailable
}

public sealed class GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure failure) : Exception
{
    public GooglePlaySubscriptionsV2ClientFailure Failure { get; } = failure;
}
