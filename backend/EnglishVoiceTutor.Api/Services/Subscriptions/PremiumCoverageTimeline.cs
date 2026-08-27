using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

internal readonly record struct PremiumCoverageInterval(
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc);

internal readonly record struct PremiumCoverageWindow(
    bool HasCoverage,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc);

internal static class PremiumCoverageTimeline
{
    public static async Task<PremiumCoverageWindow> CalculateAsync(
        AppDbContext dbContext,
        Guid userId,
        DateTimeOffset referenceTimeUtc,
        CancellationToken cancellationToken)
    {
        var trialIntervals = await dbContext.TrialGrants
            .AsNoTracking()
            .Where(trial => trial.UserId == userId
                && trial.Status == SubscriptionConstants.Entitlements.StatusActive
                && trial.GrantedAtUtc <= referenceTimeUtc
                && trial.ExpiresAtUtc > referenceTimeUtc
                && trial.CreatedAt <= referenceTimeUtc)
            .Select(trial => new PremiumCoverageInterval(trial.GrantedAtUtc, trial.ExpiresAtUtc))
            .ToListAsync(cancellationToken);

        var entitlementIntervals = await dbContext.Entitlements
            .AsNoTracking()
            .Where(entitlement => entitlement.UserId == userId
                && entitlement.PlanId == SubscriptionConstants.Plans.PremiumPlanId
                && entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType
                && entitlement.Status == SubscriptionConstants.Entitlements.StatusActive
                && (entitlement.Source == SubscriptionConstants.Entitlements.SourceManualAdmin
                    || entitlement.Source == SubscriptionConstants.Entitlements.SourceProviderEvent)
                && (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc.Value > referenceTimeUtc))
            .Where(entitlement => entitlement.CreatedAt <= referenceTimeUtc)
            .Select(entitlement => new PremiumCoverageInterval(entitlement.StartsAtUtc, entitlement.ExpiresAtUtc))
            .ToListAsync(cancellationToken);

        return Calculate(referenceTimeUtc, trialIntervals.Concat(entitlementIntervals));
    }

    public static PremiumCoverageWindow Calculate(
        DateTimeOffset referenceTimeUtc,
        IEnumerable<PremiumCoverageInterval> intervals)
    {
        var candidates = intervals
            .Where(interval => !interval.EndsAtUtc.HasValue || interval.EndsAtUtc.Value > interval.StartsAtUtc)
            .OrderBy(interval => interval.StartsAtUtc)
            .ThenBy(interval => interval.EndsAtUtc)
            .ToArray();
        var current = candidates
            .Where(interval => interval.StartsAtUtc <= referenceTimeUtc
                && (!interval.EndsAtUtc.HasValue || interval.EndsAtUtc.Value > referenceTimeUtc))
            .ToArray();

        if (current.Length == 0)
        {
            return new PremiumCoverageWindow(false, null, null);
        }

        var startsAtUtc = current.Min(interval => interval.StartsAtUtc);
        if (current.Any(interval => !interval.EndsAtUtc.HasValue))
        {
            return new PremiumCoverageWindow(true, startsAtUtc, null);
        }

        var endsAtUtc = current.Max(interval => interval.EndsAtUtc!.Value);
        foreach (var interval in candidates)
        {
            if (interval.StartsAtUtc > endsAtUtc)
            {
                break;
            }

            if (!interval.EndsAtUtc.HasValue)
            {
                return new PremiumCoverageWindow(true, startsAtUtc, null);
            }

            if (interval.EndsAtUtc.Value > endsAtUtc)
            {
                endsAtUtc = interval.EndsAtUtc.Value;
            }
        }

        return new PremiumCoverageWindow(true, startsAtUtc, endsAtUtc);
    }
}
