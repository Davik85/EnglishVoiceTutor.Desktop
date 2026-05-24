using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Subscription;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public sealed class SubscriptionStatusService(AppDbContext dbContext) : ISubscriptionStatusService
{
    public async Task<SubscriptionStatusResponse> GetStatusAsync(Guid userId, string source, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var todayUtc = DateOnly.FromDateTime(now.UtcDateTime);

        var response = new SubscriptionStatusResponse
        {
            UserId = userId,
            Source = source,
            CheckedAtUtc = now
        };

        var activeSubscription = await dbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.UserId == userId && subscription.Status == SubscriptionConstants.SubscriptionStatuses.Active)
            .OrderByDescending(subscription => subscription.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSubscription is not null)
        {
            response.SubscriptionStatus = activeSubscription.Status;
            response.BillingProvider = activeSubscription.Provider;
            response.CurrentPeriodEndUtc = activeSubscription.CurrentPeriodEndUtc;
        }

        var premiumEntitlementActive = await dbContext.Entitlements
            .AsNoTracking()
            .AnyAsync(entitlement =>
                entitlement.UserId == userId &&
                entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType &&
                entitlement.Status == SubscriptionConstants.Entitlements.StatusActive &&
                entitlement.StartsAtUtc <= now &&
                (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc > now),
                cancellationToken);

        response.PremiumActive = premiumEntitlementActive;
        if (premiumEntitlementActive)
        {
            response.PlanId = SubscriptionConstants.Plans.PremiumPlanId;
            response.PlanName = SubscriptionConstants.Plans.PremiumPlanName;
        }

        var trialGrant = await dbContext.TrialGrants
            .AsNoTracking()
            .Where(trial => trial.UserId == userId && trial.Status == SubscriptionConstants.Entitlements.StatusActive && trial.ExpiresAtUtc > now)
            .OrderByDescending(trial => trial.ExpiresAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var trialEntitlement = await dbContext.Entitlements
            .AsNoTracking()
            .Where(entitlement => entitlement.UserId == userId &&
                                  entitlement.Source == SubscriptionConstants.Entitlements.SourceTrial &&
                                  entitlement.Status == SubscriptionConstants.Entitlements.StatusActive &&
                                  entitlement.StartsAtUtc <= now &&
                                  (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc > now))
            .OrderByDescending(entitlement => entitlement.ExpiresAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        response.TrialActive = trialGrant is not null || trialEntitlement is not null;
        response.TrialEndsAtUtc = trialGrant?.ExpiresAtUtc ?? trialEntitlement?.ExpiresAtUtc;

        response.FreeLessonUsedToday = await dbContext.DailyFreeLessonUsages
            .AsNoTracking()
            .AnyAsync(usage => usage.UserId == userId && usage.UsageDate == todayUtc, cancellationToken);

        response.FreeLessonRemainingToday = response.FreeLessonUsedToday ? 0 : SubscriptionConstants.FreeLessonsPerDay;

        return response;
    }
}
