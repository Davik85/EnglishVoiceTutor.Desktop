namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlaySubscriptionsV2Client
{
    Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken);
}

public sealed record GooglePlaySubscriptionV2Snapshot(
    string? SubscriptionState,
    IReadOnlyList<string> ProductIds,
    string? AcknowledgementState,
    bool HasLinkedPurchaseToken);

public enum GooglePlaySubscriptionsV2ClientFailure
{
    InvalidPurchase,
    TemporarilyUnavailable
}

public sealed class GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure failure) : Exception
{
    public GooglePlaySubscriptionsV2ClientFailure Failure { get; } = failure;
}
