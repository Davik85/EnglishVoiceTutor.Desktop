namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IBillingEventSubscriptionSnapshotService
{
    Task<BillingEventSubscriptionSnapshotResult> ProcessProviderEventAsync(
        string billingProvider,
        string providerEventId,
        CancellationToken cancellationToken);
}
