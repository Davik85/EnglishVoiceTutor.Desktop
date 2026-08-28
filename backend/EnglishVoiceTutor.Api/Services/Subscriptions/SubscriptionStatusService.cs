using System.Text.Json;
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
    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

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

        var activePremiumEntitlements = await dbContext.Entitlements
            .AsNoTracking()
            .Where(entitlement =>
                entitlement.UserId == userId &&
                entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType &&
                entitlement.Status == SubscriptionConstants.Entitlements.StatusActive &&
                entitlement.StartsAtUtc <= now &&
                (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc > now))
            .ToListAsync(cancellationToken);

        var premiumEntitlement = SelectEffectivePremiumEntitlement(activePremiumEntitlements);
        var effectiveSubscription = await FindEffectiveSubscriptionAsync(userId, premiumEntitlement, cancellationToken);

        if (effectiveSubscription is not null)
        {
            ApplySubscriptionMetadata(response, effectiveSubscription);
        }

        response.PremiumActive = premiumEntitlement is not null;
        response.PremiumEntitlementExpiresAtUtc = premiumEntitlement?.ExpiresAtUtc;
        response.PaidAccessUntilUtc = premiumEntitlement is null
            ? response.CurrentPeriodEndUtc
            : premiumEntitlement.ExpiresAtUtc;
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

        var futurePremiumEntitlements = await dbContext.Entitlements
            .AsNoTracking()
            .Where(entitlement =>
                entitlement.UserId == userId &&
                entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType &&
                entitlement.Status == SubscriptionConstants.Entitlements.StatusActive &&
                entitlement.StartsAtUtc > now &&
                (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc > entitlement.StartsAtUtc))
            .OrderBy(entitlement => entitlement.StartsAtUtc)
            .ThenBy(entitlement => entitlement.ExpiresAtUtc)
            .ToListAsync(cancellationToken);

        var futurePremiumEntitlement = futurePremiumEntitlements.FirstOrDefault();
        response.HasFuturePremiumEntitlement = futurePremiumEntitlement is not null;
        response.FuturePremiumStartsAtUtc = futurePremiumEntitlement?.StartsAtUtc;
        response.FuturePremiumExpiresAtUtc = futurePremiumEntitlement?.ExpiresAtUtc;

        ApplyCurrentAccessSummary(response, premiumEntitlement, trialGrant is not null || trialEntitlement is not null, trialGrant?.ExpiresAtUtc ?? trialEntitlement?.ExpiresAtUtc, futurePremiumEntitlement);

        response.FreeLessonUsedToday = await dbContext.DailyFreeLessonUsages
            .AsNoTracking()
            .AnyAsync(usage => usage.UserId == userId && usage.UsageDate == todayUtc, cancellationToken);

        response.FreeLessonRemainingToday = response.FreeLessonUsedToday ? 0 : SubscriptionConstants.FreeLessonsPerDay;
        ApplyRenewalSummary(response, effectiveSubscription is not null);
        await ApplyGooglePlayPurchaseGateAsync(response, userId, cancellationToken);
        ApplyLearnerSubscriptionSummary(response, premiumEntitlement, response.TrialActive, response.TrialEndsAtUtc, futurePremiumEntitlements, now);

        return response;
    }

    private async Task ApplyGooglePlayPurchaseGateAsync(
        SubscriptionStatusResponse response,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var subscriptions = await dbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription =>
                subscription.UserId == userId
                && subscription.PlanId == SubscriptionConstants.Plans.PremiumPlanId)
            .ToListAsync(cancellationToken);

        var paddleProviderEventIds = subscriptions
            .Where(subscription =>
                IsProvider(subscription.Provider, SubscriptionConstants.BillingProviders.Paddle)
                && !string.IsNullOrWhiteSpace(subscription.LastProviderEventId))
            .Select(subscription => subscription.LastProviderEventId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var paddleBillingEvents = paddleProviderEventIds.Count == 0
            ? []
            : await dbContext.BillingEvents
                .AsNoTracking()
                .Where(billingEvent =>
                    billingEvent.BillingProvider == SubscriptionConstants.BillingProviders.Paddle
                    && paddleProviderEventIds.Contains(billingEvent.ProviderEventId))
                .ToListAsync(cancellationToken);
        var paddleBillingEventsByProviderEventId = paddleBillingEvents
            .ToDictionary(billingEvent => billingEvent.ProviderEventId, StringComparer.OrdinalIgnoreCase);

        var renewalOwners = new List<SubscriptionEntity>();
        var ownershipAmbiguous = false;

        foreach (var subscription in subscriptions)
        {
            if (IsProvider(subscription.Provider, SubscriptionConstants.BillingProviders.None)
                || IsProvider(subscription.Provider, SubscriptionConstants.BillingProviders.Manual)
                || IsProvider(subscription.Provider, SubscriptionConstants.BillingProviders.InternalTrial))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(subscription.Provider)
                || string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId))
            {
                ownershipAmbiguous = true;
                continue;
            }

            if (IsTerminalNonRenewingStatus(subscription.Status))
            {
                continue;
            }

            if (IsStatus(subscription.Status, SubscriptionConstants.SubscriptionStatuses.PastDue)
                || IsStatus(subscription.Status, SubscriptionConstants.SubscriptionStatuses.Paused))
            {
                renewalOwners.Add(subscription);
                continue;
            }

            var scheduledCancellation = subscription.CancelAtPeriodEnd
                || IsScheduledCancellation(subscription.ScheduledChangeAction);
            if (HasConflictingCancellationState(subscription, scheduledCancellation))
            {
                ownershipAmbiguous = true;
                continue;
            }

            if (scheduledCancellation)
            {
                if (IsProvider(subscription.Provider, SubscriptionConstants.BillingProviders.Paddle)
                    && (IsStatus(subscription.Status, SubscriptionConstants.SubscriptionStatuses.Active)
                        || IsStatus(subscription.Status, SubscriptionConstants.SubscriptionStatuses.Trialing))
                    && !string.IsNullOrWhiteSpace(subscription.LastProviderEventId)
                    && paddleBillingEventsByProviderEventId.TryGetValue(subscription.LastProviderEventId, out var paddleBillingEvent)
                    && HasAuthoritativeFuturePaddleScheduledCancellation(
                        subscription,
                        paddleBillingEvent,
                        userId,
                        response.CheckedAtUtc))
                {
                    continue;
                }

                ownershipAmbiguous = true;
                continue;
            }

            if (IsRecoverableRenewalStatus(subscription.Status))
            {
                renewalOwners.Add(subscription);
                continue;
            }

            ownershipAmbiguous = true;
        }

        if (ownershipAmbiguous)
        {
            response.GooglePlayPurchaseAllowed = false;
            response.GooglePlayPurchaseBlockReasonCode = SubscriptionConstants.GooglePlayPurchaseGate.RenewalOwnershipAmbiguous;
            response.GooglePlayPurchaseBlockingProvider = null;
            return;
        }

        if (renewalOwners.Count > 1)
        {
            response.GooglePlayPurchaseAllowed = false;
            response.GooglePlayPurchaseBlockReasonCode = SubscriptionConstants.GooglePlayPurchaseGate.MultipleExternalAutoRenewOwners;
            response.GooglePlayPurchaseBlockingProvider = null;
            return;
        }

        if (renewalOwners.Count == 1)
        {
            response.GooglePlayPurchaseAllowed = false;
            response.GooglePlayPurchaseBlockReasonCode = SubscriptionConstants.GooglePlayPurchaseGate.ExternalAutoRenewActive;
            response.GooglePlayPurchaseBlockingProvider = renewalOwners[0].Provider;
            return;
        }

        response.GooglePlayPurchaseAllowed = true;
        response.GooglePlayPurchaseBlockReasonCode = SubscriptionConstants.GooglePlayPurchaseGate.None;
        response.GooglePlayPurchaseBlockingProvider = null;
    }

    private static bool IsRecoverableRenewalStatus(string? status) =>
        IsStatus(status, SubscriptionConstants.SubscriptionStatuses.Active)
        || IsStatus(status, SubscriptionConstants.SubscriptionStatuses.Trialing)
        || IsStatus(status, SubscriptionConstants.SubscriptionStatuses.PastDue)
        || IsStatus(status, SubscriptionConstants.SubscriptionStatuses.Paused);

    private static bool IsTerminalNonRenewingStatus(string? status) =>
        IsStatus(status, SubscriptionConstants.SubscriptionStatuses.Canceled)
        || IsStatus(status, SubscriptionConstants.SubscriptionStatuses.Expired)
        || IsStatus(status, SubscriptionConstants.SubscriptionStatuses.Chargeback);

    private static bool HasConflictingCancellationState(
        SubscriptionEntity subscription,
        bool scheduledCancellation)
    {
        if (subscription.CancelAtPeriodEnd
            && !string.IsNullOrWhiteSpace(subscription.ScheduledChangeAction)
            && !IsScheduledCancellation(subscription.ScheduledChangeAction))
        {
            return true;
        }

        if (!scheduledCancellation)
        {
            return false;
        }

        return IsStatus(subscription.Status, SubscriptionConstants.SubscriptionStatuses.Paused)
            || string.Equals(subscription.LastProviderEventType, SubscriptionConstants.BillingEventTypes.SubscriptionResumed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(subscription.LastProviderEventType, SubscriptionConstants.BillingEventTypes.SubscriptionActivated, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAuthoritativeFuturePaddleScheduledCancellation(
        SubscriptionEntity subscription,
        BillingEventEntity billingEvent,
        Guid userId,
        DateTimeOffset checkedAtUtc)
    {
        if (!IsProvider(billingEvent.BillingProvider, SubscriptionConstants.BillingProviders.Paddle)
            || !string.Equals(billingEvent.Status, SubscriptionConstants.BillingEventStatuses.Processed, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(billingEvent.ProviderEventId, subscription.LastProviderEventId, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(billingEvent.EventType, subscription.LastProviderEventType, StringComparison.OrdinalIgnoreCase)
            || subscription.ScheduledChangeEffectiveAtUtc is null
            || subscription.ScheduledChangeEffectiveAtUtc <= checkedAtUtc
            || !TryReadPaddleScheduledChangeMetadata(billingEvent.SafeMetadataJson, out var metadata))
        {
            return false;
        }

        return string.Equals(metadata.PaddleEventId, billingEvent.ProviderEventId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(metadata.EventType, billingEvent.EventType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(metadata.PaddleSubscriptionId, subscription.ProviderSubscriptionId, StringComparison.Ordinal)
            && Guid.TryParse(metadata.InternalUserId, out var metadataUserId)
            && metadataUserId == userId
            && metadata.ScheduledChangeSnapshotComplete == true
            && IsScheduledCancellation(metadata.ScheduledChangeAction)
            && metadata.ScheduledChangeEffectiveAtUtc == subscription.ScheduledChangeEffectiveAtUtc;
    }

    private static bool TryReadPaddleScheduledChangeMetadata(
        string? safeMetadataJson,
        out PaddleScheduledChangeSafeMetadata metadata)
    {
        metadata = new PaddleScheduledChangeSafeMetadata();
        if (string.IsNullOrWhiteSpace(safeMetadataJson))
        {
            return false;
        }

        try
        {
            var parsedMetadata = JsonSerializer.Deserialize<PaddleScheduledChangeSafeMetadata>(safeMetadataJson, MetadataJsonOptions);
            if (parsedMetadata is null)
            {
                return false;
            }

            metadata = parsedMetadata;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed class PaddleScheduledChangeSafeMetadata
    {
        public string? PaddleEventId { get; set; }
        public string? EventType { get; set; }
        public string? PaddleSubscriptionId { get; set; }
        public string? InternalUserId { get; set; }
        public bool? ScheduledChangeSnapshotComplete { get; set; }
        public string? ScheduledChangeAction { get; set; }
        public DateTimeOffset? ScheduledChangeEffectiveAtUtc { get; set; }
    }

    private async Task<SubscriptionEntity?> FindEffectiveSubscriptionAsync(
        Guid userId,
        EntitlementEntity? effectiveEntitlement,
        CancellationToken cancellationToken)
    {
        if (effectiveEntitlement is null)
        {
            return null;
        }

        if (effectiveEntitlement.SubscriptionId.HasValue)
        {
            return await dbContext.Subscriptions
                .AsNoTracking()
                .SingleOrDefaultAsync(subscription =>
                    subscription.Id == effectiveEntitlement.SubscriptionId.Value &&
                    subscription.UserId == userId &&
                    subscription.PlanId == SubscriptionConstants.Plans.PremiumPlanId,
                    cancellationToken);
        }

        if (!string.Equals(effectiveEntitlement.Source, SubscriptionConstants.Entitlements.SourceProviderEvent, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var candidates = await dbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.UserId == userId && subscription.PlanId == SubscriptionConstants.Plans.PremiumPlanId)
            .ToListAsync(cancellationToken);

        return SelectLegacyProviderSubscription(candidates, effectiveEntitlement);
    }

    private static EntitlementEntity? SelectEffectivePremiumEntitlement(IEnumerable<EntitlementEntity> entitlements) =>
        entitlements
            .OrderByDescending(entitlement => !entitlement.ExpiresAtUtc.HasValue)
            .ThenByDescending(entitlement => entitlement.ExpiresAtUtc)
            .ThenBy(entitlement => entitlement.StartsAtUtc)
            .ThenBy(entitlement => entitlement.CreatedAt)
            .ThenBy(entitlement => entitlement.Id)
            .FirstOrDefault();

    private static SubscriptionEntity? SelectLegacyProviderSubscription(
        IReadOnlyList<SubscriptionEntity> candidates,
        EntitlementEntity entitlement)
    {
        var paidProviderCandidates = candidates.Where(IsExternalPaidProviderSubscription).ToList();
        var exactMatches = paidProviderCandidates.Where(candidate => HasExactCoverage(candidate, entitlement)).ToList();
        if (exactMatches.Count > 0)
        {
            return OrderLegacyCandidates(exactMatches).First();
        }

        var coveringMatches = paidProviderCandidates.Where(candidate => CoversEffectiveEntitlement(candidate, entitlement)).ToList();
        if (coveringMatches.Count > 0)
        {
            return OrderLegacyCandidates(coveringMatches).First();
        }

        return paidProviderCandidates
            .Where(candidate => candidate.CreatedAt <= entitlement.CreatedAt)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .ThenByDescending(candidate => candidate.StartedAt)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefault();
    }

    private static IOrderedEnumerable<SubscriptionEntity> OrderLegacyCandidates(IEnumerable<SubscriptionEntity> candidates) =>
        candidates
            .OrderBy(candidate => candidate.StartedAt)
            .ThenBy(candidate => candidate.CreatedAt)
            .ThenBy(candidate => candidate.Id);

    private static bool HasExactCoverage(SubscriptionEntity subscription, EntitlementEntity entitlement) =>
        entitlement.ExpiresAtUtc.HasValue
            ? subscription.CurrentPeriodEndUtc == entitlement.ExpiresAtUtc || subscription.ExpiresAt == entitlement.ExpiresAtUtc
            : !subscription.CurrentPeriodEndUtc.HasValue && !subscription.ExpiresAt.HasValue;

    private static bool CoversEffectiveEntitlement(SubscriptionEntity subscription, EntitlementEntity entitlement)
    {
        if (!entitlement.ExpiresAtUtc.HasValue)
        {
            return !subscription.CurrentPeriodEndUtc.HasValue && !subscription.ExpiresAt.HasValue;
        }

        var subscriptionEndsAtUtc = subscription.CurrentPeriodEndUtc ?? subscription.ExpiresAt;
        return subscriptionEndsAtUtc.HasValue && subscriptionEndsAtUtc >= entitlement.ExpiresAtUtc;
    }

    private static bool IsExternalPaidProviderSubscription(SubscriptionEntity subscription) =>
        !string.IsNullOrWhiteSpace(subscription.Provider)
        && !IsProvider(subscription.Provider, SubscriptionConstants.BillingProviders.None)
        && !IsProvider(subscription.Provider, SubscriptionConstants.BillingProviders.Manual)
        && !IsProvider(subscription.Provider, SubscriptionConstants.BillingProviders.InternalTrial);

    private static void ApplySubscriptionMetadata(SubscriptionStatusResponse response, SubscriptionEntity subscription)
    {
        response.SubscriptionStatus = subscription.Status;
        response.BillingProvider = subscription.Provider;
        response.CurrentPeriodEndUtc = subscription.CurrentPeriodEndUtc;
        response.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd;
        response.ScheduledChangeAction = subscription.ScheduledChangeAction;
        response.ScheduledChangeEffectiveAtUtc = subscription.ScheduledChangeEffectiveAtUtc;
        response.ProviderSubscriptionPresent = !string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId);
        response.LastProviderEventId = subscription.LastProviderEventId;
        response.LastProviderEventType = subscription.LastProviderEventType;
        response.LastProviderEventOccurredAtUtc = subscription.LastProviderEventOccurredAtUtc;
        response.HasActivePaidProviderSubscription = HasPaidProviderSubscription(subscription);
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


    private static void ApplyLearnerSubscriptionSummary(
        SubscriptionStatusResponse response,
        EntitlementEntity? premiumEntitlement,
        bool trialActive,
        DateTimeOffset? trialEndsAtUtc,
        IReadOnlyList<EntitlementEntity> futurePremiumEntitlements,
        DateTimeOffset now)
    {
        response.LearnerSubscriptionSummaryUpdatedAtUtc = now;
        response.AutoRenewalStatusCode = response.RenewalStatus == SubscriptionConstants.RenewalStatuses.RenewalActive
            ? "active"
            : "inactive";

        if (trialActive)
        {
            response.CurrentTariffId = SubscriptionConstants.Plans.TrialPlanId;
            response.CurrentTariffName = SubscriptionConstants.Plans.TrialPlanName;
            response.CurrentTariffDisplayCode = SubscriptionConstants.Plans.TrialPlanId;
            response.FreeLessonsRemainingDisplayCode = "unlimited";
            response.FreeLessonsRemainingToday = null;
            var coverage = CalculateContinuousPremiumCoverage(premiumEntitlement, trialActive, trialEndsAtUtc, futurePremiumEntitlements, now);
            response.PremiumDisplayStatusCode = coverage.EndsAtUtc.HasValue ? "active_until" : "active";
            response.PremiumStartsAtUtc = coverage.StartsAtUtc;
            response.PremiumEndsAtUtc = coverage.EndsAtUtc;
            response.PremiumCoverageStartsAtUtc = coverage.StartsAtUtc;
            response.PremiumCoverageEndsAtUtc = coverage.EndsAtUtc;
            response.PremiumCoverageDisplayStatusCode = response.PremiumDisplayStatusCode;
            return;
        }

        if (premiumEntitlement is not null)
        {
            response.CurrentTariffId = SubscriptionConstants.Plans.PremiumPlanId;
            response.CurrentTariffName = SubscriptionConstants.Plans.PremiumPlanName;
            response.CurrentTariffDisplayCode = SubscriptionConstants.Plans.PremiumPlanId;
            response.FreeLessonsRemainingDisplayCode = "unlimited";
            response.FreeLessonsRemainingToday = null;
            var coverage = CalculateContinuousPremiumCoverage(premiumEntitlement, trialActive, trialEndsAtUtc, futurePremiumEntitlements, now);
            response.PremiumStartsAtUtc = coverage.StartsAtUtc;
            response.PremiumEndsAtUtc = coverage.EndsAtUtc;
            response.PremiumCoverageStartsAtUtc = coverage.StartsAtUtc;
            response.PremiumCoverageEndsAtUtc = coverage.EndsAtUtc;
            response.PremiumDisplayStatusCode = coverage.EndsAtUtc.HasValue ? "active_until" : "active";
            response.PremiumCoverageDisplayStatusCode = response.PremiumDisplayStatusCode;
            return;
        }

        response.CurrentTariffId = SubscriptionConstants.Plans.FreePlanId;
        response.CurrentTariffName = SubscriptionConstants.Plans.FreePlanName;
        response.CurrentTariffDisplayCode = SubscriptionConstants.Plans.FreePlanId;
        response.FreeLessonsRemainingDisplayCode = "numeric";
        response.FreeLessonsRemainingToday = Math.Max(response.FreeLessonRemainingToday, 0);
        response.PremiumDisplayStatusCode = "inactive";
        response.PremiumStartsAtUtc = null;
        response.PremiumEndsAtUtc = null;
        response.PremiumCoverageStartsAtUtc = null;
        response.PremiumCoverageEndsAtUtc = null;
        response.PremiumCoverageDisplayStatusCode = "inactive";
    }

    private static PremiumCoverageWindow CalculateContinuousPremiumCoverage(
        EntitlementEntity? activePremiumEntitlement,
        bool trialActive,
        DateTimeOffset? trialEndsAtUtc,
        IReadOnlyList<EntitlementEntity> futurePremiumEntitlements,
        DateTimeOffset now)
    {
        var coverageStartsAtUtc = activePremiumEntitlement?.StartsAtUtc;
        var intervals = new List<PremiumCoverageInterval>();
        if (activePremiumEntitlement is not null)
        {
            intervals.Add(new PremiumCoverageInterval(activePremiumEntitlement.StartsAtUtc, activePremiumEntitlement.ExpiresAtUtc));
        }
        if (trialActive && trialEndsAtUtc.HasValue)
        {
            intervals.Add(new PremiumCoverageInterval(now, trialEndsAtUtc));
        }
        intervals.AddRange(futurePremiumEntitlements
            .Where(entitlement =>
                string.Equals(entitlement.Source, SubscriptionConstants.Entitlements.SourceProviderEvent, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entitlement.Source, SubscriptionConstants.Entitlements.SourceManualAdmin, StringComparison.OrdinalIgnoreCase))
            .Select(entitlement => new PremiumCoverageInterval(entitlement.StartsAtUtc, entitlement.ExpiresAtUtc)));

        var timeline = PremiumCoverageTimeline.Calculate(now, intervals);
        return new PremiumCoverageWindow(coverageStartsAtUtc, timeline.HasCoverage ? timeline.EndsAtUtc : null);
    }

    private sealed record PremiumCoverageWindow(DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc);

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
