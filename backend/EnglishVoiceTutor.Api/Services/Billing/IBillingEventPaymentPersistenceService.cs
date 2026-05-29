namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IBillingEventPaymentPersistenceService
{
    Task<BillingEventPaymentPersistenceResult> ProcessProviderEventAsync(
        string billingProvider,
        string providerEventId,
        CancellationToken cancellationToken);
}
