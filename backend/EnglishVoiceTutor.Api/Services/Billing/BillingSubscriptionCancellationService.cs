using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Billing;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingSubscriptionCancellationService(
    AppDbContext dbContext,
    IEnumerable<IBillingProviderSubscriptionCancellationAdapter> adapters) : IBillingSubscriptionCancellationService
{
    public async Task<CancelBillingSubscriptionResponse> CancelCurrentUserSubscriptionRenewalAsync(Guid userId, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.Subscriptions
            .Where(candidate => candidate.UserId == userId
                && candidate.Provider == SubscriptionConstants.BillingProviders.Paddle)
            .OrderByDescending(candidate => candidate.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null || string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId) || !IsActiveProviderSubscription(subscription.Status))
        {
            return CreateResponse(false, false, false, "No active paid subscription was found.", SubscriptionConstants.BillingProviders.Paddle, subscription?.Status ?? SubscriptionConstants.SubscriptionStatuses.None, subscription?.CancelAtPeriodEnd ?? false, subscription?.ScheduledChangeEffectiveAtUtc, subscription?.CurrentPeriodEndUtc);
        }

        if (subscription.CancelAtPeriodEnd || string.Equals(subscription.ScheduledChangeAction, SubscriptionConstants.ScheduledChangeActions.Cancel, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResponse(true, true, true, "Subscription renewal is already scheduled to cancel.", subscription.Provider, subscription.Status, true, subscription.ScheduledChangeEffectiveAtUtc ?? subscription.CurrentPeriodEndUtc, subscription.CurrentPeriodEndUtc);
        }

        var adapter = adapters.FirstOrDefault(candidate => string.Equals(candidate.ProviderId, subscription.Provider, StringComparison.OrdinalIgnoreCase));
        if (adapter is null)
        {
            return CreateResponse(false, false, false, "Subscription cancellation is not available yet.", subscription.Provider, subscription.Status, false, subscription.ScheduledChangeEffectiveAtUtc, subscription.CurrentPeriodEndUtc);
        }

        var result = await adapter.CancelSubscriptionRenewalAsync(new BillingProviderSubscriptionCancelRequest
        {
            UserId = userId,
            ProviderSubscriptionId = subscription.ProviderSubscriptionId,
            CurrentPeriodEndUtc = subscription.CurrentPeriodEndUtc,
            RequestedAtUtc = DateTimeOffset.UtcNow
        }, cancellationToken);

        if (!result.Accepted)
        {
            return CreateResponse(false, false, false, result.Message, result.Provider, subscription.Status, subscription.CancelAtPeriodEnd, subscription.ScheduledChangeEffectiveAtUtc, subscription.CurrentPeriodEndUtc);
        }

        var now = DateTimeOffset.UtcNow;
        subscription.CancelAtPeriodEnd = true;
        subscription.ScheduledChangeAction = SubscriptionConstants.ScheduledChangeActions.Cancel;
        subscription.ScheduledChangeEffectiveAtUtc = result.ScheduledChangeEffectiveAtUtc ?? subscription.CurrentPeriodEndUtc;
        subscription.LastSyncedAtUtc = now;
        subscription.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateResponse(true, true, false, result.Message, subscription.Provider, result.SubscriptionStatus, true, subscription.ScheduledChangeEffectiveAtUtc, subscription.CurrentPeriodEndUtc);
    }

    private static bool IsActiveProviderSubscription(string status) =>
        string.Equals(status, SubscriptionConstants.SubscriptionStatuses.Active, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, SubscriptionConstants.SubscriptionStatuses.Trialing, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, SubscriptionConstants.SubscriptionStatuses.PastDue, StringComparison.OrdinalIgnoreCase);

    private static CancelBillingSubscriptionResponse CreateResponse(bool accepted, bool success, bool alreadyCanceling, string message, string provider, string status, bool cancelAtPeriodEnd, DateTimeOffset? effectiveAt, DateTimeOffset? periodEnd) => new()
    {
        Accepted = accepted,
        Success = success,
        AlreadyCanceling = alreadyCanceling,
        Message = message,
        Provider = provider,
        SubscriptionStatus = status,
        CancelAtPeriodEnd = cancelAtPeriodEnd,
        ScheduledChangeEffectiveAtUtc = effectiveAt,
        CurrentPeriodEndUtc = periodEnd
    };
}
