using EnglishVoiceTutor.Api.Contracts.Billing;

namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IBillingSubscriptionCancellationService
{
    Task<CancelBillingSubscriptionResponse> CancelCurrentUserSubscriptionRenewalAsync(Guid userId, CancellationToken cancellationToken);
}
