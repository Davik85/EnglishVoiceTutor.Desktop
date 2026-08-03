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
    bool IsTestPurchase)
{
    // The token is intentionally internal: public snapshots and their record string
    // representation must never carry provider token material.
    internal string? LinkedPurchaseToken { get; init; }

    internal GooglePlaySubscriptionV2Snapshot(
        string? subscriptionState,
        DateTimeOffset? startTimeUtc,
        IReadOnlyList<GooglePlaySubscriptionLineItemSnapshot> lineItems,
        GooglePlayPurchaseAcknowledgementState? acknowledgementState,
        bool isTestPurchase,
        string? linkedPurchaseToken)
        : this(subscriptionState, startTimeUtc, lineItems, acknowledgementState, isTestPurchase)
    {
        LinkedPurchaseToken = linkedPurchaseToken;
    }
}

public sealed record GooglePlaySubscriptionLineItemSnapshot(
    string? ProductId,
    DateTimeOffset? ExpiryTimeUtc,
    string? DeferredItemReplacementProductId = null);

public enum GooglePlaySubscriptionsV2ClientFailure
{
    InvalidPurchase,
    TemporarilyUnavailable
}

public sealed class GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure failure) : Exception
{
    public GooglePlaySubscriptionsV2ClientFailure Failure { get; } = failure;
}
