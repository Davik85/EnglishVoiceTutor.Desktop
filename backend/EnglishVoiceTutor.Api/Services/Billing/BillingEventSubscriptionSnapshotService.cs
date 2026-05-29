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

        var billingEventId = await dbContext.BillingEvents
            .AsNoTracking()
            .Where(candidate => candidate.BillingProvider == billingProvider
                && candidate.ProviderEventId == providerEventId)
            .Select(candidate => (Guid?)candidate.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (billingEventId is null)
        {
            return CreateResult(startedAtUtc, checkedCount, upsertedCount, ignoredOlderCount, blockedCount, failedCount, alreadySkippedCount);
        }

        checkedCount = 1;

        try
        {
            var result = await ProcessBillingEventAsync(billingEventId.Value, cancellationToken);
            switch (result)
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

        return CreateResult(startedAtUtc, checkedCount, upsertedCount, ignoredOlderCount, blockedCount, failedCount, alreadySkippedCount);
    }

    private async Task<SubscriptionSnapshotEventResult> ProcessBillingEventAsync(
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
            return SubscriptionSnapshotEventResult.AlreadySkipped;
        }

        if (!IsSupportedSubscriptionLifecycleEvent(billingEvent))
        {
            await transaction.CommitAsync(cancellationToken);
            return SubscriptionSnapshotEventResult.AlreadySkipped;
        }

        if (billingEvent.Status != SubscriptionConstants.BillingEventStatuses.Received)
        {
            await transaction.CommitAsync(cancellationToken);
            return SubscriptionSnapshotEventResult.AlreadySkipped;
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
            return SubscriptionSnapshotEventResult.Blocked;
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
            return SubscriptionSnapshotEventResult.IgnoredOlder;
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

        billingEvent.Status = SubscriptionConstants.BillingEventStatuses.Processed;
        billingEvent.ProcessedAtUtc = nowUtc;
        billingEvent.ErrorMessage = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SubscriptionSnapshotEventResult.Upserted;
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

    private static bool IsSupportedSubscriptionLifecycleEvent(BillingEventEntity billingEvent)
    {
        return billingEvent.BillingProvider == SubscriptionConstants.BillingProviders.Paddle
            && (string.Equals(billingEvent.EventType, SubscriptionConstants.BillingEventTypes.SubscriptionCreated, StringComparison.OrdinalIgnoreCase)
                || string.Equals(billingEvent.EventType, SubscriptionConstants.BillingEventTypes.SubscriptionUpdated, StringComparison.OrdinalIgnoreCase)
                || string.Equals(billingEvent.EventType, SubscriptionConstants.BillingEventTypes.SubscriptionPastDue, StringComparison.OrdinalIgnoreCase));
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
        int alreadySkippedCount)
    {
        return new BillingEventSubscriptionSnapshotResult(
            checkedCount,
            upsertedCount,
            ignoredOlderCount,
            blockedCount,
            failedCount,
            alreadySkippedCount,
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
                errorMessage);
    }
}
