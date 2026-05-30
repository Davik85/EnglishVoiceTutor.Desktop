using System.Data;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingEventSubscriptionSnapshotService : IBillingEventSubscriptionSnapshotService
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext dbContext;
    private readonly ILogger<BillingEventSubscriptionSnapshotService> logger;

    public BillingEventSubscriptionSnapshotService(
        AppDbContext dbContext,
        ILogger<BillingEventSubscriptionSnapshotService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<BillingEventSubscriptionSnapshotResult> ProcessProviderEventAsync(
        string billingProvider,
        string providerEventId,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var checkedCount = 0;
        var upsertedCount = 0;
        var ignoredOlderCount = 0;
        var blockedCount = 0;
        var failedCount = 0;
        var alreadySkippedCount = 0;
        var providerEventEntitlementExpiredCount = 0;
        DateTimeOffset? providerEventEntitlementExpiresAtUtc = null;

        var billingEventId = await dbContext.BillingEvents
            .AsNoTracking()
            .Where(candidate => candidate.BillingProvider == billingProvider
                && candidate.ProviderEventId == providerEventId)
            .Select(candidate => (Guid?)candidate.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (billingEventId is null)
        {
            return CreateResult(startedAtUtc, checkedCount, upsertedCount, ignoredOlderCount, blockedCount, failedCount, alreadySkippedCount, providerEventEntitlementExpiredCount, providerEventEntitlementExpiresAtUtc);
        }

        checkedCount = 1;

        try
        {
            var result = await BillingSerializableTransactionRetryPolicy.ExecuteAsync(
                (_, retryCancellationToken) => ProcessBillingEventAsync(billingEventId.Value, retryCancellationToken),
                dbContext.ChangeTracker.Clear,
                logger,
                "Billing event subscription lifecycle snapshot processing",
                billingEventId.Value,
                cancellationToken);
            providerEventEntitlementExpiredCount += result.ProviderEventEntitlementExpiredCount;
            providerEventEntitlementExpiresAtUtc = result.ProviderEventEntitlementExpiresAtUtc;
            switch (result.Result)
            {
                case SubscriptionSnapshotEventResult.Upserted:
                    upsertedCount++;
                    break;
                case SubscriptionSnapshotEventResult.IgnoredOlder:
                    ignoredOlderCount++;
                    break;
                case SubscriptionSnapshotEventResult.Blocked:
                    blockedCount++;
                    break;
                case SubscriptionSnapshotEventResult.AlreadySkipped:
                    alreadySkippedCount++;
                    break;
                case SubscriptionSnapshotEventResult.Failed:
                    failedCount++;
                    break;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            failedCount++;
            dbContext.ChangeTracker.Clear();
            await TryMarkBillingEventFailedAsync(billingEventId.Value, cancellationToken);

            logger.LogError(
                exception,
                "Billing event subscription lifecycle snapshot processing failed unexpectedly. BillingEventId={BillingEventId}; BillingProvider={BillingProvider}; ProviderEventId={ProviderEventId}.",
                billingEventId.Value,
                billingProvider,
                providerEventId);
        }

        return CreateResult(startedAtUtc, checkedCount, upsertedCount, ignoredOlderCount, blockedCount, failedCount, alreadySkippedCount, providerEventEntitlementExpiredCount, providerEventEntitlementExpiresAtUtc);
    }

    private async Task<SubscriptionSnapshotEventOutcome> ProcessBillingEventAsync(
        Guid billingEventId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var billingEvent = await dbContext.BillingEvents.SingleOrDefaultAsync(
            candidate => candidate.Id == billingEventId,
            cancellationToken);
        if (billingEvent is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return SubscriptionSnapshotEventOutcome.AlreadySkipped();
        }

        if (!IsSupportedSubscriptionLifecycleEvent(billingEvent))
        {
            await transaction.CommitAsync(cancellationToken);
            return SubscriptionSnapshotEventOutcome.AlreadySkipped();
        }

        if (billingEvent.Status != SubscriptionConstants.BillingEventStatuses.Received)
        {
            await transaction.CommitAsync(cancellationToken);
            return SubscriptionSnapshotEventOutcome.AlreadySkipped();
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var validation = await ValidateBillingEventAsync(billingEvent, cancellationToken);
        if (!validation.IsValid)
        {
            MarkBlocked(
                billingEvent,
                nowUtc,
                validation.ErrorMessage ?? SubscriptionConstants.SubscriptionLifecycleSnapshot.InvalidBillingEventMetadataMessage);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return SubscriptionSnapshotEventOutcome.Blocked();
        }

        var existingSubscription = await dbContext.Subscriptions.SingleOrDefaultAsync(
            subscription => subscription.Provider == billingEvent.BillingProvider
                && subscription.ProviderSubscriptionId == validation.ProviderSubscriptionId,
            cancellationToken);

        if (existingSubscription is not null && IsOlderProviderEvent(existingSubscription, validation.EventOccurredAtUtc))
        {
            billingEvent.Status = SubscriptionConstants.BillingEventStatuses.Processed;
            billingEvent.ProcessedAtUtc = nowUtc;
            billingEvent.ErrorMessage = SubscriptionConstants.SubscriptionLifecycleSnapshot.OlderProviderEventIgnoredMessage;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return SubscriptionSnapshotEventOutcome.IgnoredOlder();
        }

        if (existingSubscription is null)
        {
            existingSubscription = new SubscriptionEntity
            {
                Id = Guid.NewGuid(),
                UserId = validation.InternalUserId!.Value,
                PlanId = validation.InternalPlanId,
                Provider = billingEvent.BillingProvider,
                ProviderSubscriptionId = validation.ProviderSubscriptionId,
                StartedAt = validation.BillingPeriodStartsAtUtc ?? validation.EventOccurredAtUtc ?? nowUtc,
                CreatedAt = nowUtc
            };
            dbContext.Subscriptions.Add(existingSubscription);
        }

        ApplySnapshot(existingSubscription, billingEvent, validation, nowUtc);

        var entitlementExpiry = IsEntitlementExpiryLifecycleEvent(billingEvent.EventType)
            ? await ExpireActiveProviderEventPremiumEntitlementsAsync(
                existingSubscription,
                validation.InternalUserId!.Value,
                ResolveEntitlementExpiryUtc(validation, nowUtc),
                nowUtc,
                billingEvent,
                cancellationToken)
            : EntitlementExpiryOutcome.None;

        billingEvent.Status = SubscriptionConstants.BillingEventStatuses.Processed;
        billingEvent.ProcessedAtUtc = nowUtc;
        billingEvent.ErrorMessage = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SubscriptionSnapshotEventOutcome.Upserted(
            entitlementExpiry.ExpiredCount,
            entitlementExpiry.ExpiresAtUtc);
    }

    private async Task<SubscriptionSnapshotValidationResult> ValidateBillingEventAsync(
        BillingEventEntity billingEvent,
        CancellationToken cancellationToken)
    {
        if (!TryReadMetadata(billingEvent.SafeMetadataJson, out var metadata))
        {
            return SubscriptionSnapshotValidationResult.Invalid(SubscriptionConstants.SubscriptionLifecycleSnapshot.InvalidBillingEventMetadataMessage);
        }

        if (metadata.InternalUserId is null)
        {
            return SubscriptionSnapshotValidationResult.Invalid(SubscriptionConstants.SubscriptionLifecycleSnapshot.MissingInternalUserIdMessage);
        }

        if (!Guid.TryParse(metadata.InternalUserId, out var internalUserId))
        {
            return SubscriptionSnapshotValidationResult.Invalid(SubscriptionConstants.SubscriptionLifecycleSnapshot.InvalidInternalUserIdMessage);
        }

        var providerSubscriptionId = FirstNonEmpty(metadata.PaddleSubscriptionId);
        if (providerSubscriptionId is null)
        {
            return SubscriptionSnapshotValidationResult.Invalid(SubscriptionConstants.SubscriptionLifecycleSnapshot.MissingProviderSubscriptionIdMessage);
        }

        var userExists = await dbContext.Users.AnyAsync(user => user.Id == internalUserId, cancellationToken);
        if (!userExists)
        {
            return SubscriptionSnapshotValidationResult.Invalid(SubscriptionConstants.SubscriptionLifecycleSnapshot.UserNotFoundMessage);
        }

        return SubscriptionSnapshotValidationResult.Valid(
            internalUserId,
            string.IsNullOrWhiteSpace(metadata.InternalPlanId) ? SubscriptionConstants.Plans.PremiumPlanId : metadata.InternalPlanId.Trim(),
            providerSubscriptionId,
            FirstNonEmpty(metadata.PaddleCustomerId),
            FirstNonEmpty(metadata.PaddlePriceId),
            FirstNonEmpty(metadata.PaddleProductId),
            MapSubscriptionStatus(metadata.PaddleStatus, billingEvent.EventType),
            metadata.BillingPeriodStartsAtUtc,
            metadata.BillingPeriodEndsAtUtc,
            metadata.CancelAtPeriodEnd,
            FirstNonEmpty(metadata.ScheduledChangeAction),
            metadata.ScheduledChangeEffectiveAtUtc,
            metadata.EffectiveAtUtc,
            metadata.OccurredAtUtc,
            metadata.PaddleEventId,
            metadata.EventType);
    }

    private static void ApplySnapshot(
        SubscriptionEntity subscription,
        BillingEventEntity billingEvent,
        SubscriptionSnapshotValidationResult snapshot,
        DateTimeOffset nowUtc)
    {
        subscription.UserId = snapshot.InternalUserId!.Value;
        subscription.PlanId = snapshot.InternalPlanId;
        subscription.Status = snapshot.Status;
        subscription.Provider = billingEvent.BillingProvider;
        subscription.ProviderSubscriptionId = snapshot.ProviderSubscriptionId;
        subscription.ProviderCustomerId = snapshot.ProviderCustomerId ?? subscription.ProviderCustomerId;
        subscription.ProviderPriceId = snapshot.ProviderPriceId ?? subscription.ProviderPriceId;
        subscription.ProviderProductId = snapshot.ProviderProductId ?? subscription.ProviderProductId;
        subscription.CurrentPeriodStartUtc = snapshot.BillingPeriodStartsAtUtc ?? subscription.CurrentPeriodStartUtc;
        subscription.CurrentPeriodEndUtc = snapshot.BillingPeriodEndsAtUtc ?? subscription.CurrentPeriodEndUtc;
        subscription.ExpiresAt = snapshot.BillingPeriodEndsAtUtc ?? subscription.ExpiresAt;
        subscription.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd || snapshot.CancelAtPeriodEnd || IsScheduledCancellation(snapshot.ScheduledChangeAction);
        subscription.ScheduledChangeAction = snapshot.ScheduledChangeAction ?? subscription.ScheduledChangeAction;
        subscription.ScheduledChangeEffectiveAtUtc = snapshot.ScheduledChangeEffectiveAtUtc ?? subscription.ScheduledChangeEffectiveAtUtc;
        subscription.LastProviderEventId = snapshot.ProviderEventId;
        subscription.LastProviderEventType = snapshot.ProviderEventType;
        subscription.LastProviderEventOccurredAtUtc = snapshot.EventOccurredAtUtc;
        subscription.LastSyncedAtUtc = nowUtc;
        subscription.UpdatedAt = nowUtc;
    }

    private async Task<EntitlementExpiryOutcome> ExpireActiveProviderEventPremiumEntitlementsAsync(
        SubscriptionEntity subscription,
        Guid internalUserId,
        DateTimeOffset requestedExpiresAtUtc,
        DateTimeOffset nowUtc,
        BillingEventEntity billingEvent,
        CancellationToken cancellationToken)
    {
        var activeEntitlements = await dbContext.Entitlements
            .Where(entitlement => entitlement.UserId == internalUserId
                && entitlement.PlanId == SubscriptionConstants.Plans.PremiumPlanId
                && entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType
                && entitlement.Source == SubscriptionConstants.Entitlements.SourceProviderEvent
                && entitlement.Status == SubscriptionConstants.Entitlements.StatusActive
                && entitlement.StartsAtUtc <= nowUtc
                && (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc > nowUtc)
                && (entitlement.SubscriptionId == null || entitlement.SubscriptionId == subscription.Id))
            .ToListAsync(cancellationToken);

        var expiredCount = 0;
        DateTimeOffset? latestExpiresAtUtc = null;

        foreach (var entitlement in activeEntitlements)
        {
            if (entitlement.ExpiresAtUtc.HasValue && entitlement.ExpiresAtUtc.Value <= requestedExpiresAtUtc)
            {
                continue;
            }

            entitlement.SubscriptionId ??= subscription.Id;
            entitlement.ExpiresAtUtc = requestedExpiresAtUtc;
            entitlement.Reason = CreateEntitlementExpiryReason(billingEvent);
            entitlement.UpdatedAt = nowUtc;

            expiredCount++;
            latestExpiresAtUtc = MaxDateTimeOffset(latestExpiresAtUtc, requestedExpiresAtUtc);
        }

        return new EntitlementExpiryOutcome(expiredCount, latestExpiresAtUtc);
    }

    private static DateTimeOffset ResolveEntitlementExpiryUtc(
        SubscriptionSnapshotValidationResult snapshot,
        DateTimeOffset nowUtc)
    {
        return snapshot.EffectiveAtUtc
            ?? snapshot.EventOccurredAtUtc
            ?? nowUtc;
    }

    private static bool IsEntitlementExpiryLifecycleEvent(string eventType)
    {
        return string.Equals(eventType, SubscriptionConstants.BillingEventTypes.SubscriptionCanceled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(eventType, SubscriptionConstants.BillingEventTypes.SubscriptionPaused, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset? MaxDateTimeOffset(DateTimeOffset? current, DateTimeOffset? candidate)
    {
        if (current is null)
        {
            return candidate;
        }

        if (candidate is null)
        {
            return current;
        }

        return candidate.Value > current.Value ? candidate : current;
    }

    private static string CreateEntitlementExpiryReason(BillingEventEntity billingEvent)
    {
        return $"{SubscriptionConstants.SubscriptionLifecycleSnapshot.ExpiredProviderEventEntitlementReason} Provider={billingEvent.BillingProvider}; ProviderEventType={billingEvent.EventType}; ProviderEventId={billingEvent.ProviderEventId}.";
    }

    private static bool IsSupportedSubscriptionLifecycleEvent(BillingEventEntity billingEvent)
    {
        return billingEvent.BillingProvider == SubscriptionConstants.BillingProviders.Paddle
            && (string.Equals(billingEvent.EventType, SubscriptionConstants.BillingEventTypes.SubscriptionCreated, StringComparison.OrdinalIgnoreCase)
                || string.Equals(billingEvent.EventType, SubscriptionConstants.BillingEventTypes.SubscriptionUpdated, StringComparison.OrdinalIgnoreCase)
                || string.Equals(billingEvent.EventType, SubscriptionConstants.BillingEventTypes.SubscriptionPastDue, StringComparison.OrdinalIgnoreCase)
                || string.Equals(billingEvent.EventType, SubscriptionConstants.BillingEventTypes.SubscriptionCanceled, StringComparison.OrdinalIgnoreCase)
                || string.Equals(billingEvent.EventType, SubscriptionConstants.BillingEventTypes.SubscriptionPaused, StringComparison.OrdinalIgnoreCase)
                || string.Equals(billingEvent.EventType, SubscriptionConstants.BillingEventTypes.SubscriptionResumed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(billingEvent.EventType, SubscriptionConstants.BillingEventTypes.SubscriptionActivated, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsOlderProviderEvent(SubscriptionEntity subscription, DateTimeOffset? incomingEventOccurredAtUtc)
    {
        return subscription.LastProviderEventOccurredAtUtc is not null
            && incomingEventOccurredAtUtc is not null
            && incomingEventOccurredAtUtc.Value < subscription.LastProviderEventOccurredAtUtc.Value;
    }

    private static bool IsScheduledCancellation(string? scheduledChangeAction)
    {
        return string.Equals(
            scheduledChangeAction?.Trim(),
            SubscriptionConstants.ScheduledChangeActions.Cancel,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string MapSubscriptionStatus(string? providerStatus, string eventType)
    {
        if (string.Equals(eventType, SubscriptionConstants.BillingEventTypes.SubscriptionPastDue, StringComparison.OrdinalIgnoreCase))
        {
            return SubscriptionConstants.SubscriptionStatuses.PastDue;
        }

        if (string.Equals(eventType, SubscriptionConstants.BillingEventTypes.SubscriptionCanceled, StringComparison.OrdinalIgnoreCase))
        {
            return SubscriptionConstants.SubscriptionStatuses.Canceled;
        }

        if (string.Equals(eventType, SubscriptionConstants.BillingEventTypes.SubscriptionPaused, StringComparison.OrdinalIgnoreCase))
        {
            return SubscriptionConstants.SubscriptionStatuses.Paused;
        }

        if (string.Equals(eventType, SubscriptionConstants.BillingEventTypes.SubscriptionResumed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(eventType, SubscriptionConstants.BillingEventTypes.SubscriptionActivated, StringComparison.OrdinalIgnoreCase))
        {
            return SubscriptionConstants.SubscriptionStatuses.Active;
        }

        return providerStatus?.Trim().ToLowerInvariant() switch
        {
            SubscriptionConstants.SubscriptionStatuses.Active => SubscriptionConstants.SubscriptionStatuses.Active,
            SubscriptionConstants.SubscriptionStatuses.Trialing => SubscriptionConstants.SubscriptionStatuses.Trialing,
            SubscriptionConstants.SubscriptionStatuses.PastDue => SubscriptionConstants.SubscriptionStatuses.PastDue,
            SubscriptionConstants.SubscriptionStatuses.Paused => SubscriptionConstants.SubscriptionStatuses.Paused,
            SubscriptionConstants.SubscriptionStatuses.Canceled => SubscriptionConstants.SubscriptionStatuses.Canceled,
            _ => SubscriptionConstants.SubscriptionStatuses.Unknown
        };
    }

    private async Task TryMarkBillingEventFailedAsync(Guid billingEventId, CancellationToken cancellationToken)
    {
        try
        {
            var billingEvent = await dbContext.BillingEvents.SingleOrDefaultAsync(
                candidate => candidate.Id == billingEventId
                    && candidate.Status == SubscriptionConstants.BillingEventStatuses.Received,
                cancellationToken);
            if (billingEvent is null)
            {
                return;
            }

            var nowUtc = DateTimeOffset.UtcNow;
            billingEvent.Status = SubscriptionConstants.BillingEventStatuses.Failed;
            billingEvent.ProcessedAtUtc = nowUtc;
            billingEvent.ErrorMessage = SubscriptionConstants.SubscriptionLifecycleSnapshot.UnexpectedProcessingErrorMessage;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Unable to mark billing event subscription lifecycle snapshot processing as failed. BillingEventId={BillingEventId}.",
                billingEventId);
        }
    }

    private static void MarkBlocked(BillingEventEntity billingEvent, DateTimeOffset nowUtc, string reason)
    {
        billingEvent.Status = SubscriptionConstants.BillingEventStatuses.ReconciliationBlocked;
        billingEvent.ProcessedAtUtc = nowUtc;
        billingEvent.ErrorMessage = reason;
    }

    private static bool TryReadMetadata(string? safeMetadataJson, out BillingEventSubscriptionSafeMetadata metadata)
    {
        metadata = new BillingEventSubscriptionSafeMetadata();

        if (string.IsNullOrWhiteSpace(safeMetadataJson))
        {
            return false;
        }

        try
        {
            var parsedMetadata = JsonSerializer.Deserialize<BillingEventSubscriptionSafeMetadata>(safeMetadataJson, MetadataJsonOptions);
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

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private static BillingEventSubscriptionSnapshotResult CreateResult(
        DateTimeOffset startedAtUtc,
        int checkedCount,
        int upsertedCount,
        int ignoredOlderCount,
        int blockedCount,
        int failedCount,
        int alreadySkippedCount,
        int providerEventEntitlementExpiredCount,
        DateTimeOffset? providerEventEntitlementExpiresAtUtc)
    {
        return new BillingEventSubscriptionSnapshotResult(
            checkedCount,
            upsertedCount,
            ignoredOlderCount,
            blockedCount,
            failedCount,
            alreadySkippedCount,
            providerEventEntitlementExpiredCount,
            providerEventEntitlementExpiresAtUtc,
            startedAtUtc,
            DateTimeOffset.UtcNow);
    }

    private enum SubscriptionSnapshotEventResult
    {
        Upserted,
        IgnoredOlder,
        Blocked,
        Failed,
        AlreadySkipped
    }

    private sealed record SubscriptionSnapshotEventOutcome(
        SubscriptionSnapshotEventResult Result,
        int ProviderEventEntitlementExpiredCount,
        DateTimeOffset? ProviderEventEntitlementExpiresAtUtc)
    {
        public static SubscriptionSnapshotEventOutcome Upserted(
            int providerEventEntitlementExpiredCount,
            DateTimeOffset? providerEventEntitlementExpiresAtUtc) =>
            new(SubscriptionSnapshotEventResult.Upserted, providerEventEntitlementExpiredCount, providerEventEntitlementExpiresAtUtc);

        public static SubscriptionSnapshotEventOutcome IgnoredOlder() =>
            new(SubscriptionSnapshotEventResult.IgnoredOlder, 0, null);

        public static SubscriptionSnapshotEventOutcome Blocked() =>
            new(SubscriptionSnapshotEventResult.Blocked, 0, null);

        public static SubscriptionSnapshotEventOutcome AlreadySkipped() =>
            new(SubscriptionSnapshotEventResult.AlreadySkipped, 0, null);
    }

    private sealed record EntitlementExpiryOutcome(int ExpiredCount, DateTimeOffset? ExpiresAtUtc)
    {
        public static EntitlementExpiryOutcome None { get; } = new(0, null);
    }

    private sealed class BillingEventSubscriptionSafeMetadata
    {
        public string? PaddleEventId { get; set; }
        public string? EventType { get; set; }
        public string? PaddleSubscriptionId { get; set; }
        public string? PaddleCustomerId { get; set; }
        public string? InternalUserId { get; set; }
        public string? InternalPlanId { get; set; }
        public string? PaddleStatus { get; set; }
        public string? PaddlePriceId { get; set; }
        public string? PaddleProductId { get; set; }
        public DateTimeOffset? BillingPeriodStartsAtUtc { get; set; }
        public DateTimeOffset? BillingPeriodEndsAtUtc { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        public string? ScheduledChangeAction { get; set; }
        public DateTimeOffset? ScheduledChangeEffectiveAtUtc { get; set; }
        public DateTimeOffset? EffectiveAtUtc { get; set; }
        public DateTimeOffset? OccurredAtUtc { get; set; }
    }

    private sealed record SubscriptionSnapshotValidationResult(
        bool IsValid,
        Guid? InternalUserId,
        string InternalPlanId,
        string? ProviderSubscriptionId,
        string? ProviderCustomerId,
        string? ProviderPriceId,
        string? ProviderProductId,
        string Status,
        DateTimeOffset? BillingPeriodStartsAtUtc,
        DateTimeOffset? BillingPeriodEndsAtUtc,
        bool CancelAtPeriodEnd,
        string? ScheduledChangeAction,
        DateTimeOffset? ScheduledChangeEffectiveAtUtc,
        DateTimeOffset? EffectiveAtUtc,
        DateTimeOffset? EventOccurredAtUtc,
        string? ProviderEventId,
        string? ProviderEventType,
        string? ErrorMessage)
    {
        public static SubscriptionSnapshotValidationResult Valid(
            Guid internalUserId,
            string internalPlanId,
            string providerSubscriptionId,
            string? providerCustomerId,
            string? providerPriceId,
            string? providerProductId,
            string status,
            DateTimeOffset? billingPeriodStartsAtUtc,
            DateTimeOffset? billingPeriodEndsAtUtc,
            bool cancelAtPeriodEnd,
            string? scheduledChangeAction,
            DateTimeOffset? scheduledChangeEffectiveAtUtc,
            DateTimeOffset? effectiveAtUtc,
            DateTimeOffset? eventOccurredAtUtc,
            string? providerEventId,
            string? providerEventType) =>
            new(
                true,
                internalUserId,
                internalPlanId,
                providerSubscriptionId,
                providerCustomerId,
                providerPriceId,
                providerProductId,
                status,
                billingPeriodStartsAtUtc,
                billingPeriodEndsAtUtc,
                cancelAtPeriodEnd,
                scheduledChangeAction,
                scheduledChangeEffectiveAtUtc,
                effectiveAtUtc,
                eventOccurredAtUtc,
                providerEventId,
                providerEventType,
                null);

        public static SubscriptionSnapshotValidationResult Invalid(string errorMessage) =>
            new(
                false,
                null,
                SubscriptionConstants.Plans.PremiumPlanId,
                null,
                null,
                null,
                null,
                SubscriptionConstants.SubscriptionStatuses.Unknown,
                null,
                null,
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                errorMessage);
    }
}
