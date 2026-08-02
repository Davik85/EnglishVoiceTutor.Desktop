namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class DisabledGooglePlaySubscriptionsV2Client : IGooglePlaySubscriptionsV2Client
{
    public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) =>
        Task.FromException<GooglePlaySubscriptionV2Snapshot?>(new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable));

    public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) =>
        Task.FromException(new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable));
}
