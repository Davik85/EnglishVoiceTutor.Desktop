using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Subscription;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public sealed class SubscriptionStatusService(
    AppDbContext dbContext,
    IOptions<SubscriptionEnforcementOptions> subscriptionEnforcementOptions) : ISubscriptionStatusService
{
    public async Task<SubscriptionStatusResponse> GetStatusAsync(Guid userId, string source, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var todayUtc = DateOnly.FromDateTime(now.UtcDateTime);

        var response = new SubscriptionStatusResponse
        {
            UserId = userId,
            Source = source,
            CheckedAtUtc = now,
            EnforcementEnabled = subscriptionEnforcementOptions.Value.Enabled
        };

        var latestSubscription = await dbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.UserId == userId)
            .OrderByDescending(subscription => subscription.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSubscription is not null)
        {
            response.SubscriptionStatus = latestSubscription.Status;
            response.BillingProvider = latestSubscription.Provider;
            response.CurrentPeriodEndUtc = latestSubscription.CurrentPeriodEndUtc;
            response.CancelAtPeriodEnd = latestSubscription.CancelAtPeriodEnd;
            response.ScheduledChangeAction = latestSubscription.ScheduledChangeAction;
            response.ScheduledChangeEffectiveAtUtc = latestSubscription.ScheduledChangeEffectiveAtUtc;
        }

        var premiumEntitlement = await dbContext.Entitlements
            .AsNoTracking()
            .Where(entitlement =>
                entitlement.UserId == userId &&
                entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType &&
                entitlement.Status == SubscriptionConstants.Entitlements.StatusActive &&
                entitlement.StartsAtUtc <= now &&
                (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc > now))
            .OrderByDescending(entitlement => entitlement.ExpiresAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        response.PremiumActive = premiumEntitlement is not null;
        response.PremiumEntitlementExpiresAtUtc = premiumEntitlement?.ExpiresAtUtc;
        if (premiumEntitlement is not null)
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
