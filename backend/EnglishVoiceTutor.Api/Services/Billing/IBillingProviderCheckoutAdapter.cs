namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IBillingProviderCheckoutAdapter
{
    string ProviderId { get; }

    Task<BillingProviderCheckoutResult> CreateCheckoutSessionAsync(
        BillingProviderCheckoutRequest request,
        CancellationToken cancellationToken);
}
