using EnglishVoiceTutor.Api.Contracts.Billing;

namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IBillingCheckoutService
{
    Task<CreateBillingCheckoutSessionResponse> CreateCheckoutSessionAsync(
        Guid userId,
        CreateBillingCheckoutSessionRequest request,
        CancellationToken cancellationToken);
}
