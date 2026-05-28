namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IBillingEventEntitlementActivationService
{
    Task<BillingEventEntitlementActivationResult> ActivatePendingEntitlementsAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<BillingEventEntitlementActivationResult> ActivateProviderEventAsync(
        string billingProvider,
        string providerEventId,
        CancellationToken cancellationToken);
}
