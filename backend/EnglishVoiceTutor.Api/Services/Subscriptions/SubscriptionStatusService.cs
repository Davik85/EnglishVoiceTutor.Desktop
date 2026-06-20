using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Subscription;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
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
            response.ProviderSubscriptionPresent = !string.IsNullOrWhiteSpace(latestSubscription.ProviderSubscriptionId);
            response.LastProviderEventId = latestSubscription.LastProviderEventId;
            response.LastProviderEventType = latestSubscription.LastProviderEventType;
            response.LastProviderEventOccurredAtUtc = latestSubscription.LastProviderEventOccurredAtUtc;
            response.HasActivePaidProviderSubscription = HasPaidProviderSubscription(latestSubscription);
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
            .ThenByDescending(entitlement => entitlement.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        response.PremiumActive = premiumEntitlement is not null;
        response.PremiumEntitlementExpiresAtUtc = premiumEntitlement?.ExpiresAtUtc;
        response.PaidAccessUntilUtc = premiumEntitlement?.ExpiresAtUtc ?? response.CurrentPeriodEndUtc;
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

        var futurePremiumEntitlement = await dbContext.Entitlements
            .AsNoTracking()
            .Where(entitlement =>
                entitlement.UserId == userId &&
                entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType &&
                entitlement.Status == SubscriptionConstants.Entitlements.StatusActive &&
                entitlement.StartsAtUtc > now &&
                (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc > entitlement.StartsAtUtc))
            .OrderBy(entitlement => entitlement.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        response.HasFuturePremiumEntitlement = futurePremiumEntitlement is not null;
        response.FuturePremiumStartsAtUtc = futurePremiumEntitlement?.StartsAtUtc;
        response.FuturePremiumExpiresAtUtc = futurePremiumEntitlement?.ExpiresAtUtc;

        ApplyCurrentAccessSummary(response, premiumEntitlement, trialGrant is not null || trialEntitlement is not null, trialGrant?.ExpiresAtUtc ?? trialEntitlement?.ExpiresAtUtc, futurePremiumEntitlement);

        response.FreeLessonUsedToday = await dbContext.DailyFreeLessonUsages
            .AsNoTracking()
            .AnyAsync(usage => usage.UserId == userId && usage.UsageDate == todayUtc, cancellationToken);

        response.FreeLessonRemainingToday = response.FreeLessonUsedToday ? 0 : SubscriptionConstants.FreeLessonsPerDay;
        ApplyRenewalSummary(response, latestSubscription is not null);

        return response;
    }

    private static void ApplyCurrentAccessSummary(
        SubscriptionStatusResponse response,
        EntitlementEntity? premiumEntitlement,
        bool trialActive,
        DateTimeOffset? trialEndsAtUtc,
        EntitlementEntity? futurePremiumEntitlement)
    {
        if (premiumEntitlement is null)
        {
            response.CurrentAccessTier = "free";
            response.CurrentAccessSource = "free";
            response.CurrentAccessActive = false;
            response.CurrentAccessDisplayCode = "current_access_free";
            response.DailyFreeLimitApplies = true;
            response.DailyFreeLessonsLabelCode = "daily_free_lessons_remaining";
        }
        else
        {
            response.CurrentAccessActive = true;
            response.CurrentAccessStartsAtUtc = premiumEntitlement.StartsAtUtc;
            response.CurrentAccessEndsAtUtc = premiumEntitlement.ExpiresAtUtc;
            response.DailyFreeLimitApplies = false;
            response.DailyFreeLessonsLabelCode = "daily_free_lessons_unlimited";

            var source = premiumEntitlement.Source ?? string.Empty;
            if (trialActive && string.Equals(source, SubscriptionConstants.Entitlements.SourceTrial, StringComparison.OrdinalIgnoreCase))
            {
                response.CurrentAccessTier = "trial_premium";
                response.CurrentAccessSource = "trial";
                response.CurrentAccessEndsAtUtc = trialEndsAtUtc ?? premiumEntitlement.ExpiresAtUtc;
                response.CurrentAccessDisplayCode = "current_access_trial_premium";
            }
            else if (string.Equals(source, SubscriptionConstants.Entitlements.SourceProviderEvent, StringComparison.OrdinalIgnoreCase))
            {
                response.CurrentAccessTier = "paid_premium";
                response.CurrentAccessSource = "provider_event";
                response.CurrentAccessDisplayCode = "current_access_paid_premium";
            }
            else if (string.Equals(source, SubscriptionConstants.Entitlements.SourceManualAdmin, StringComparison.OrdinalIgnoreCase))
            {
                response.CurrentAccessTier = "admin_premium";
                response.CurrentAccessSource = "admin";
                response.CurrentAccessDisplayCode = "current_access_admin_premium";
            }
            else if (source.Contains("development", StringComparison.OrdinalIgnoreCase))
            {
                response.CurrentAccessTier = "development_premium";
                response.CurrentAccessSource = "development";
                response.CurrentAccessDisplayCode = "current_access_development_premium";
            }
            else
            {
                response.CurrentAccessTier = "unknown_premium";
                response.CurrentAccessSource = string.IsNullOrWhiteSpace(source) ? "unknown" : source;
                response.CurrentAccessDisplayCode = "current_access_premium";
            }
        }

        if (futurePremiumEntitlement is not null
            && string.Equals(futurePremiumEntitlement.Source, SubscriptionConstants.Entitlements.SourceProviderEvent, StringComparison.OrdinalIgnoreCase))
        {
            response.HasScheduledPaidPremium = true;
            response.ScheduledPaidPremiumStartUtc = futurePremiumEntitlement.StartsAtUtc;
            response.ScheduledPaidPremiumEndUtc = futurePremiumEntitlement.ExpiresAtUtc;
            response.ScheduledPaidPremiumSource = "provider_event";
            response.ScheduledPaidPremiumLabelCode = "scheduled_paid_premium";
        }
    }

    private static bool HasPaidProviderSubscription(SubscriptionEntity subscription)
    {
        return !string.IsNullOrWhiteSpace(subscription.Provider)
            && !string.Equals(subscription.Provider, SubscriptionConstants.BillingProviders.None, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(subscription.Provider, SubscriptionConstants.BillingProviders.Manual, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(subscription.Provider, SubscriptionConstants.BillingProviders.InternalTrial, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId)
            && (IsStatus(subscription.Status, SubscriptionConstants.SubscriptionStatuses.Active)
                || IsStatus(subscription.Status, SubscriptionConstants.SubscriptionStatuses.Trialing)
                || IsStatus(subscription.Status, SubscriptionConstants.SubscriptionStatuses.PastDue));
    }

    private static void ApplyRenewalSummary(SubscriptionStatusResponse response, bool hasSubscriptionSnapshot)
    {
        var hasProvider = !string.IsNullOrWhiteSpace(response.BillingProvider) && !IsProvider(response.BillingProvider, SubscriptionConstants.BillingProviders.None);
        var scheduledCancellation = response.CancelAtPeriodEnd || IsScheduledCancellation(response.ScheduledChangeAction);
        var activeOrTrialing = IsStatus(response.SubscriptionStatus, SubscriptionConstants.SubscriptionStatuses.Active) || IsStatus(response.SubscriptionStatus, SubscriptionConstants.SubscriptionStatuses.Trialing);

        if (scheduledCancellation)
        {
            response.RenewalStatus = SubscriptionConstants.RenewalStatuses.CancellationScheduled;
            response.NextRenewalState = SubscriptionConstants.NextRenewalStates.NoRenewalScheduled;
            response.CanRequestCancelRenewal = false;
            response.CancellationExplanationCode = SubscriptionConstants.CancellationExplanationCodes.AlreadyScheduled;
            return;
        }

        if (activeOrTrialing && hasProvider && response.ProviderSubscriptionPresent)
        {
            response.RenewalStatus = SubscriptionConstants.RenewalStatuses.RenewalActive;
            response.NextRenewalState = SubscriptionConstants.NextRenewalStates.RenewalExpected;
            response.CanRequestCancelRenewal = true;
            response.CancellationExplanationCode = SubscriptionConstants.CancellationExplanationCodes.None;
            return;
        }

        if (response.PremiumActive && (!hasSubscriptionSnapshot || !hasProvider || !response.ProviderSubscriptionPresent))
        {
            response.RenewalStatus = SubscriptionConstants.RenewalStatuses.NoPaidSubscription;
            response.NextRenewalState = SubscriptionConstants.NextRenewalStates.NotApplicable;
            response.CanRequestCancelRenewal = false;
            response.CancellationExplanationCode = hasProvider && !response.ProviderSubscriptionPresent
                ? SubscriptionConstants.CancellationExplanationCodes.ProviderSubscriptionMissing
                : SubscriptionConstants.CancellationExplanationCodes.NoPaidProviderSubscription;
            return;
        }

        if (IsStatus(response.SubscriptionStatus, SubscriptionConstants.SubscriptionStatuses.Canceled) || IsStatus(response.SubscriptionStatus, SubscriptionConstants.SubscriptionStatuses.Expired))
        {
            response.RenewalStatus = SubscriptionConstants.RenewalStatuses.SubscriptionCanceled;
            response.NextRenewalState = SubscriptionConstants.NextRenewalStates.NotApplicable;
            response.CanRequestCancelRenewal = false;
            response.CancellationExplanationCode = SubscriptionConstants.CancellationExplanationCodes.Canceled;
            return;
        }

        if (IsStatus(response.SubscriptionStatus, SubscriptionConstants.SubscriptionStatuses.Paused))
        {
            response.RenewalStatus = SubscriptionConstants.RenewalStatuses.SubscriptionPaused;
            response.NextRenewalState = SubscriptionConstants.NextRenewalStates.Unknown;
            response.CanRequestCancelRenewal = false;
            response.CancellationExplanationCode = SubscriptionConstants.CancellationExplanationCodes.Paused;
            return;
        }

        if (IsStatus(response.SubscriptionStatus, SubscriptionConstants.SubscriptionStatuses.PastDue))
        {
            response.RenewalStatus = SubscriptionConstants.RenewalStatuses.PastDue;
            response.NextRenewalState = SubscriptionConstants.NextRenewalStates.Unknown;
            response.CanRequestCancelRenewal = false;
            response.CancellationExplanationCode = SubscriptionConstants.CancellationExplanationCodes.PastDue;
            return;
        }

        if (!response.PremiumActive && (!hasSubscriptionSnapshot || IsStatus(response.SubscriptionStatus, SubscriptionConstants.SubscriptionStatuses.None)))
        {
            response.RenewalStatus = SubscriptionConstants.RenewalStatuses.NoPaidSubscription;
            response.NextRenewalState = SubscriptionConstants.NextRenewalStates.NotApplicable;
            response.CanRequestCancelRenewal = false;
            response.CancellationExplanationCode = SubscriptionConstants.CancellationExplanationCodes.NoPaidProviderSubscription;
            return;
        }

        response.RenewalStatus = SubscriptionConstants.RenewalStatuses.Unknown;
        response.NextRenewalState = SubscriptionConstants.NextRenewalStates.Unknown;
        response.CanRequestCancelRenewal = false;
        response.CancellationExplanationCode = SubscriptionConstants.CancellationExplanationCodes.Unknown;
    }

    private static bool IsScheduledCancellation(string? action) => !string.IsNullOrWhiteSpace(action) && action.Contains("cancel", StringComparison.OrdinalIgnoreCase);
    private static bool IsStatus(string? status, string expected) => string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);
    private static bool IsProvider(string? provider, string expected) => string.Equals(provider, expected, StringComparison.OrdinalIgnoreCase);
}
