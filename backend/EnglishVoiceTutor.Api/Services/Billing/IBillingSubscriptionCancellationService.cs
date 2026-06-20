using EnglishVoiceTutor.Api.Contracts.Billing;

namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IBillingSubscriptionCancellationService
{
    Task<CancelBillingSubscriptionResponse> CancelCurrentUserSubscriptionRenewalAsync(Guid userId, CancellationToken cancellationToken);
    Task<CancelBillingSubscriptionResponse> CancelUserSubscriptionRenewalAsync(Guid userId, CancellationToken cancellationToken);
}
