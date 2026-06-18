namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IBillingProviderSubscriptionCancellationAdapter
{
    string ProviderId { get; }

    Task<BillingProviderSubscriptionCancelResult> CancelSubscriptionRenewalAsync(
        BillingProviderSubscriptionCancelRequest request,
        CancellationToken cancellationToken);
}
