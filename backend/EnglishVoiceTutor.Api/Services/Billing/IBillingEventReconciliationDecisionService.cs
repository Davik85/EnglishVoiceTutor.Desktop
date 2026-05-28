namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IBillingEventReconciliationDecisionService
{
    Task<BillingEventReconciliationDecisionResult> ProcessReceivedEventsAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<BillingEventReconciliationDecisionResult> ProcessProviderEventAsync(
        string billingProvider,
        string providerEventId,
        CancellationToken cancellationToken);
}
