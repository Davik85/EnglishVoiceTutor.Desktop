namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlaySubscriptionsV2Client
{
    Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken);
    Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken);
    Task<GooglePlaySubscriptionDeferResponseSnapshot> DeferAsync(
        string packageName,
        string purchaseToken,
        string etag,
        TimeSpan duration,
        CancellationToken cancellationToken) =>
        Task.FromException<GooglePlaySubscriptionDeferResponseSnapshot>(
            new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable));
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
    internal string? Etag { get; init; }

    internal GooglePlaySubscriptionV2Snapshot(
        string? subscriptionState,
        DateTimeOffset? startTimeUtc,
        IReadOnlyList<GooglePlaySubscriptionLineItemSnapshot> lineItems,
        GooglePlayPurchaseAcknowledgementState? acknowledgementState,
        bool isTestPurchase,
        string? linkedPurchaseToken,
        string? etag = null)
        : this(subscriptionState, startTimeUtc, lineItems, acknowledgementState, isTestPurchase)
    {
        LinkedPurchaseToken = linkedPurchaseToken;
        Etag = etag;
    }
}

public sealed record GooglePlaySubscriptionLineItemSnapshot(
    string? ProductId,
    DateTimeOffset? ExpiryTimeUtc,
    string? DeferredItemReplacementProductId = null)
{
    internal bool HasDeferredItemRemoval { get; init; }
    internal bool HasAutoRenewingPlan { get; init; }
    internal bool? AutoRenewEnabled { get; init; }
    internal bool HasPrepaidPlan { get; init; }
    internal string? BasePlanId { get; init; }
    internal string? OfferId { get; init; }
    internal GooglePlaySubscriptionOfferPhase? OfferPhase { get; init; }
    internal bool HasSignupPromotion { get; init; }
    internal bool HasItemReplacement { get; init; }
    internal bool HasLatestSuccessfulOrderId { get; init; }
}

public enum GooglePlaySubscriptionOfferPhase
{
    BasePrice,
    FreeTrial,
    IntroductoryPrice,
    Proration,
    Ambiguous
}

public sealed record GooglePlaySubscriptionDeferItemSnapshot(string? ProductId, DateTimeOffset? ExpiryTimeUtc);
public sealed record GooglePlaySubscriptionDeferResponseSnapshot(IReadOnlyList<GooglePlaySubscriptionDeferItemSnapshot> Items);

public enum GooglePlaySubscriptionsV2ClientFailure
{
    InvalidPurchase,
    TemporarilyUnavailable,
    PreconditionFailed,
    ProviderOutcomeUnknown
}

public sealed class GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure failure) : Exception
{
    public GooglePlaySubscriptionsV2ClientFailure Failure { get; } = failure;
}
