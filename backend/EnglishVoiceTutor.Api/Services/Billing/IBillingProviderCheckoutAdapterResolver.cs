namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IBillingProviderCheckoutAdapterResolver
{
    IBillingProviderCheckoutAdapter Resolve(string provider);
}
