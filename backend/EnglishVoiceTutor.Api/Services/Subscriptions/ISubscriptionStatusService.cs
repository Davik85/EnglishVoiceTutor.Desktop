using EnglishVoiceTutor.Api.Contracts.Subscription;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public interface ISubscriptionStatusService
{
    Task<SubscriptionStatusResponse> GetStatusAsync(Guid userId, string source, CancellationToken cancellationToken);
}
