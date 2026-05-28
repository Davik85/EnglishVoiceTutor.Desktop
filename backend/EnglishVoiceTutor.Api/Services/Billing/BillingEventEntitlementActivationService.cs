using System.Data;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingEventEntitlementActivationService : IBillingEventEntitlementActivationService
{
    public const int DefaultActivationLimit = 25;
    public const int MaxActivationLimit = 100;

    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext dbContext;
    private readonly ILogger<BillingEventEntitlementActivationService> logger;

    public BillingEventEntitlementActivationService(
        AppDbContext dbContext,
        ILogger<BillingEventEntitlementActivationService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<BillingEventEntitlementActivationResult> ActivatePendingEntitlementsAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var effectiveLimit = NormalizeLimit(limit);
        var checkedCount = 0;
        var activatedCount = 0;
        var blockedCount = 0;
        var failedCount = 0;
        var alreadySkippedCount = 0;

        var billingEventIds = await dbContext.BillingEvents
            .AsNoTracking()
            .Where(billingEvent => billingEvent.BillingProvider == SubscriptionConstants.BillingProviders.Paddle
                && billingEvent.Status == SubscriptionConstants.BillingEventStatuses.ReconciliationPending
                && billingEvent.EventType == SubscriptionConstants.BillingEventTypes.TransactionCompleted)
            .OrderBy(billingEvent => billingEvent.ReceivedAtUtc)
            .ThenBy(billingEvent => billingEvent.Id)
            .Take(effectiveLimit)
            .Select(billingEvent => billingEvent.Id)
            .ToListAsync(cancellationToken);

        checkedCount = billingEventIds.Count;

        foreach (var billingEventId in billingEventIds)
        {
            try
            {
                var result = await ActivateBillingEventAsync(billingEventId, cancellationToken);
                switch (result)
                {
                    case ActivationEventResult.Activated:
                        activatedCount++;
                        break;
                    case ActivationEventResult.Blocked:
                        blockedCount++;
                        break;
                    case ActivationEventResult.AlreadySkipped:
                        alreadySkippedCount++;
                        break;
                    case ActivationEventResult.Failed:
                        failedCount++;
                        break;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failedCount++;
                dbContext.ChangeTracker.Clear();
                await TryMarkBillingEventFailedAsync(billingEventId, cancellationToken);

                logger.LogError(
                    exception,
                    "Billing event entitlement activation failed unexpectedly. BillingEventId={BillingEventId}.",
                    billingEventId);
            }
        }

        var completedAtUtc = DateTimeOffset.UtcNow;
        return new BillingEventEntitlementActivationResult(
            checkedCount,
            activatedCount,
            blockedCount,
            failedCount,
            alreadySkippedCount,
            startedAtUtc,
            completedAtUtc);
    }

    public async Task<BillingEventEntitlementActivationResult> ActivateProviderEventAsync(
        string billingProvider,
        string providerEventId,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var checkedCount = 0;
        var activatedCount = 0;
        var blockedCount = 0;
        var failedCount = 0;
        var alreadySkippedCount = 0;

        var billingEventId = await dbContext.BillingEvents
            .AsNoTracking()
            .Where(candidate => candidate.BillingProvider == billingProvider
                && candidate.ProviderEventId == providerEventId)
            .Select(candidate => (Guid?)candidate.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (billingEventId is not null)
        {
            checkedCount = 1;

            try
            {
                var result = await ActivateBillingEventAsync(billingEventId.Value, cancellationToken);
                switch (result)
                {
                    case ActivationEventResult.Activated:
                        activatedCount++;
                        break;
                    case ActivationEventResult.Blocked:
                        blockedCount++;
                        break;
                    case ActivationEventResult.AlreadySkipped:
                        alreadySkippedCount++;
                        break;
                    case ActivationEventResult.Failed:
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
                    "Billing event entitlement activation failed unexpectedly. BillingEventId={BillingEventId}; BillingProvider={BillingProvider}; ProviderEventId={ProviderEventId}.",
                    billingEventId.Value,
                    billingProvider,
                    providerEventId);
            }
        }

        var completedAtUtc = DateTimeOffset.UtcNow;
        return new BillingEventEntitlementActivationResult(
            checkedCount,
            activatedCount,
            blockedCount,
            failedCount,
            alreadySkippedCount,
            startedAtUtc,
            completedAtUtc);
    }

    private async Task<ActivationEventResult> ActivateBillingEventAsync(
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
            return ActivationEventResult.AlreadySkipped;
        }

        if (billingEvent.BillingProvider != SubscriptionConstants.BillingProviders.Paddle
            || billingEvent.Status != SubscriptionConstants.BillingEventStatuses.ReconciliationPending
            || billingEvent.EventType != SubscriptionConstants.BillingEventTypes.TransactionCompleted)
        {
            await transaction.CommitAsync(cancellationToken);
            return ActivationEventResult.AlreadySkipped;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var validation = await ValidateBillingEventAsync(billingEvent, nowUtc, cancellationToken);
        if (!validation.IsValid)
        {
            MarkBlocked(billingEvent, nowUtc, validation.ErrorMessage ?? SubscriptionConstants.BillingEventActivation.InvalidBillingEventMetadataMessage);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ActivationEventResult.Blocked;
        }

        dbContext.Entitlements.Add(new EntitlementEntity
        {
            Id = Guid.NewGuid(),
            UserId = validation.InternalUserId!.Value,
            PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            SubscriptionId = null,
            EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
            Source = SubscriptionConstants.Entitlements.SourceProviderEvent,
            Status = SubscriptionConstants.Entitlements.StatusActive,
            StartsAtUtc = validation.BillingPeriodStartsAtUtc ?? nowUtc,
            ExpiresAtUtc = validation.BillingPeriodEndsAtUtc,
            Reason = CreateActivationReason(billingEvent),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        });

        billingEvent.Status = SubscriptionConstants.BillingEventStatuses.Processed;
        billingEvent.ProcessedAtUtc = nowUtc;
        billingEvent.ErrorMessage = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ActivationEventResult.Activated;
    }

    private async Task<ActivationValidationResult> ValidateBillingEventAsync(
        BillingEventEntity billingEvent,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (!TryReadMetadata(billingEvent.SafeMetadataJson, out var metadata))
        {
            return ActivationValidationResult.Invalid(SubscriptionConstants.BillingEventActivation.InvalidBillingEventMetadataMessage);
        }

        if (metadata.InternalUserId is null)
        {
            return ActivationValidationResult.Invalid(SubscriptionConstants.BillingEventActivation.MissingInternalUserIdMessage);
        }

        if (!Guid.TryParse(metadata.InternalUserId, out var internalUserId))
        {
            return ActivationValidationResult.Invalid(SubscriptionConstants.BillingEventActivation.InvalidInternalUserIdMessage);
        }

        if (!string.Equals(metadata.InternalPlanId, SubscriptionConstants.Plans.PremiumPlanId, StringComparison.OrdinalIgnoreCase))
        {
            return ActivationValidationResult.Invalid(SubscriptionConstants.BillingEventActivation.UnsupportedPlanIdMessage);
        }

        if (metadata.BillingPeriodEndsAtUtc is null)
        {
            return ActivationValidationResult.Invalid(SubscriptionConstants.BillingEventActivation.MissingBillingPeriodEndMessage);
        }

        if (metadata.BillingPeriodEndsAtUtc.Value <= nowUtc)
        {
            return ActivationValidationResult.Invalid(SubscriptionConstants.BillingEventActivation.BillingPeriodEndNotFutureMessage);
        }

        var userExists = await dbContext.Users.AnyAsync(user => user.Id == internalUserId, cancellationToken);
        if (!userExists)
        {
            return ActivationValidationResult.Invalid(SubscriptionConstants.BillingEventActivation.UserNotFoundMessage);
        }

        return ActivationValidationResult.Valid(
            internalUserId,
            metadata.BillingPeriodStartsAtUtc,
            metadata.BillingPeriodEndsAtUtc.Value);
    }

    private async Task TryMarkBillingEventFailedAsync(Guid billingEventId, CancellationToken cancellationToken)
    {
        try
        {
            var billingEvent = await dbContext.BillingEvents.SingleOrDefaultAsync(
                candidate => candidate.Id == billingEventId
                    && candidate.Status == SubscriptionConstants.BillingEventStatuses.ReconciliationPending,
                cancellationToken);
            if (billingEvent is null)
            {
                return;
            }

            var nowUtc = DateTimeOffset.UtcNow;
            billingEvent.Status = SubscriptionConstants.BillingEventStatuses.Failed;
            billingEvent.ProcessedAtUtc = nowUtc;
            billingEvent.ErrorMessage = SubscriptionConstants.BillingEventActivation.UnexpectedProcessingErrorMessage;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Unable to mark billing event entitlement activation as failed. BillingEventId={BillingEventId}.",
                billingEventId);
        }
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return DefaultActivationLimit;
        }

        return Math.Min(limit, MaxActivationLimit);
    }

    private static void MarkBlocked(BillingEventEntity billingEvent, DateTimeOffset nowUtc, string reason)
    {
        billingEvent.Status = SubscriptionConstants.BillingEventStatuses.ReconciliationBlocked;
        billingEvent.ProcessedAtUtc = nowUtc;
        billingEvent.ErrorMessage = reason;
    }

    private static string CreateActivationReason(BillingEventEntity billingEvent)
    {
        return $"{SubscriptionConstants.BillingEventActivation.ActivatedReason} Provider={billingEvent.BillingProvider}; ProviderEventId={billingEvent.ProviderEventId}.";
    }

    private static bool TryReadMetadata(string? safeMetadataJson, out BillingEventActivationSafeMetadata metadata)
    {
        metadata = new BillingEventActivationSafeMetadata();

        if (string.IsNullOrWhiteSpace(safeMetadataJson))
        {
            return false;
        }

        try
        {
            var parsedMetadata = JsonSerializer.Deserialize<BillingEventActivationSafeMetadata>(safeMetadataJson, MetadataJsonOptions);
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

    private enum ActivationEventResult
    {
        Activated,
        Blocked,
        Failed,
        AlreadySkipped
    }

    private sealed class BillingEventActivationSafeMetadata
    {
        public string? InternalUserId { get; set; }
        public string? InternalPlanId { get; set; }
        public DateTimeOffset? BillingPeriodStartsAtUtc { get; set; }
        public DateTimeOffset? BillingPeriodEndsAtUtc { get; set; }
    }

    private sealed record ActivationValidationResult(
        bool IsValid,
        Guid? InternalUserId,
        DateTimeOffset? BillingPeriodStartsAtUtc,
        DateTimeOffset? BillingPeriodEndsAtUtc,
        string? ErrorMessage)
    {
        public static ActivationValidationResult Valid(
            Guid internalUserId,
            DateTimeOffset? billingPeriodStartsAtUtc,
            DateTimeOffset billingPeriodEndsAtUtc) =>
            new(true, internalUserId, billingPeriodStartsAtUtc, billingPeriodEndsAtUtc, null);

        public static ActivationValidationResult Invalid(string errorMessage) =>
            new(false, null, null, null, errorMessage);
    }
}
