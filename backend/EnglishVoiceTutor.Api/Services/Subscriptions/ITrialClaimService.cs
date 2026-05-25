using EnglishVoiceTutor.Api.Contracts.Subscription;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public interface ITrialClaimService
{
    Task<TrialClaimResponse> ClaimTrialAsync(Guid userId, string source, CancellationToken cancellationToken);
}
