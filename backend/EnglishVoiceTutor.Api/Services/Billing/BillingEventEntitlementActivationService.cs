using System.Data;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingEventEntitlementActivationService : IBillingEventEntitlementActivationService
{
    public const int DefaultActivationLimit = 25;
    public const int MaxActivationLimit = 100;

    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext dbContext;
    private readonly ILogger<BillingEventEntitlementActivationService> logger;
    private readonly PaddleBillingOptions paddleOptions;

    public BillingEventEntitlementActivationService(
        AppDbContext dbContext,
        ILogger<BillingEventEntitlementActivationService> logger,
        IOptions<PaddleBillingOptions> paddleOptions)
    {
        this.dbContext = dbContext;
        this.logger = logger;
        this.paddleOptions = paddleOptions.Value;
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
        DateTimeOffset? latestEntitlementExpiresAtUtc = null;

        var billingEventIds = await dbContext.BillingEvents
            .AsNoTracking()
            .Where(billingEvent => billingEvent.Status == SubscriptionConstants.BillingEventStatuses.ReconciliationPending
                && (billingEvent.EventType == SubscriptionConstants.BillingEventTypes.TransactionCompleted
                    || billingEvent.EventType == SubscriptionConstants.BillingEventTypes.AdjustmentCreated
                    || billingEvent.EventType == SubscriptionConstants.BillingEventTypes.AdjustmentUpdated))
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
                var activationOutcome = await BillingSerializableTransactionRetryPolicy.ExecuteAsync(
                    (_, retryCancellationToken) => ActivateBillingEventAsync(billingEventId, retryCancellationToken),
                    dbContext.ChangeTracker.Clear,
                    logger,
                    "Billing event entitlement activation",
                    billingEventId,
                    cancellationToken);
                latestEntitlementExpiresAtUtc = MaxDateTimeOffset(latestEntitlementExpiresAtUtc, activationOutcome.EntitlementExpiresAtUtc);

                switch (activationOutcome.Result)
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
            completedAtUtc,
            latestEntitlementExpiresAtUtc);
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
        DateTimeOffset? entitlementExpiresAtUtc = null;

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
                var activationOutcome = await BillingSerializableTransactionRetryPolicy.ExecuteAsync(
                    (_, retryCancellationToken) => ActivateBillingEventAsync(billingEventId.Value, retryCancellationToken),
                    dbContext.ChangeTracker.Clear,
                    logger,
                    "Billing event entitlement activation",
                    billingEventId.Value,
                    cancellationToken);
                entitlementExpiresAtUtc = activationOutcome.EntitlementExpiresAtUtc;

                switch (activationOutcome.Result)
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
            completedAtUtc,
            entitlementExpiresAtUtc);
    }


    public async Task<BillingEventEntitlementActivationResult> RevokeAdjustmentProviderEventAsync(
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
        DateTimeOffset? entitlementExpiresAtUtc = null;

        var billingEventId = await dbContext.BillingEvents
            .AsNoTracking()
            .Where(candidate => candidate.BillingProvider == billingProvider
                && candidate.ProviderEventId == providerEventId
                && (candidate.EventType == SubscriptionConstants.BillingEventTypes.AdjustmentCreated
                    || candidate.EventType == SubscriptionConstants.BillingEventTypes.AdjustmentUpdated))
            .Select(candidate => (Guid?)candidate.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (billingEventId is not null)
        {
            checkedCount = 1;

            try
            {
                var activationOutcome = await BillingSerializableTransactionRetryPolicy.ExecuteAsync(
                    (_, retryCancellationToken) => RevokeAdjustmentBillingEventAsync(billingEventId.Value, retryCancellationToken),
                    dbContext.ChangeTracker.Clear,
                    logger,
                    "Paddle adjustment operator reprocess revocation",
                    billingEventId.Value,
                    cancellationToken);
                entitlementExpiresAtUtc = activationOutcome.EntitlementExpiresAtUtc;

                switch (activationOutcome.Result)
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
                logger.LogError(
                    exception,
                    "Paddle adjustment operator reprocess revocation failed unexpectedly. BillingEventId={BillingEventId}; BillingProvider={BillingProvider}; ProviderEventId={ProviderEventId}.",
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
            completedAtUtc,
            entitlementExpiresAtUtc);
    }

    private async Task<ActivationEventOutcome> ActivateBillingEventAsync(
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
            return ActivationEventOutcome.AlreadySkipped();
        }

        if (billingEvent.Status != SubscriptionConstants.BillingEventStatuses.ReconciliationPending)
        {
            await transaction.CommitAsync(cancellationToken);
            return ActivationEventOutcome.AlreadySkipped();
        }

        var nowUtc = DateTimeOffset.UtcNow;
        if (IsAdjustmentEvent(billingEvent.EventType))
        {
            var refundOutcome = await ProcessAdjustmentBillingEventAsync(billingEvent, nowUtc, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return refundOutcome;
        }

        if (billingEvent.EventType != SubscriptionConstants.BillingEventTypes.TransactionCompleted)
        {
            await transaction.CommitAsync(cancellationToken);
            return ActivationEventOutcome.AlreadySkipped();
        }

        var validation = await ValidateBillingEventAsync(billingEvent, nowUtc, cancellationToken);
        if (!validation.IsValid)
        {
            MarkBlocked(billingEvent, nowUtc, validation.ErrorMessage ?? SubscriptionConstants.BillingEventActivation.InvalidBillingEventMetadataMessage);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ActivationEventOutcome.Blocked();
        }

        var entitlement = await FindCurrentOrScheduledProviderEventEntitlementAsync(
            validation.InternalUserId!.Value,
            nowUtc,
            cancellationToken);

        var schedule = await CalculateStackedProviderEntitlementScheduleAsync(
            validation.InternalUserId.Value,
            validation.BillingPeriodStartsAtUtc,
            validation.BillingPeriodEndsAtUtc!.Value,
            entitlement,
            nowUtc,
            cancellationToken);

        var entitlementChanged = false;
        DateTimeOffset? effectiveExpiresAtUtc = schedule.ExpiresAtUtc;

        if (entitlement is null)
        {
            dbContext.Entitlements.Add(new EntitlementEntity
            {
                Id = Guid.NewGuid(),
                UserId = validation.InternalUserId.Value,
                PlanId = SubscriptionConstants.Plans.PremiumPlanId,
                SubscriptionId = null,
                EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
                Source = SubscriptionConstants.Entitlements.SourceProviderEvent,
                Status = SubscriptionConstants.Entitlements.StatusActive,
                StartsAtUtc = schedule.StartsAtUtc,
                ExpiresAtUtc = schedule.ExpiresAtUtc,
                Reason = CreateActivationReason(billingEvent),
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            });

            entitlementChanged = true;
        }
        else if (entitlement.ExpiresAtUtc is null || schedule.ExpiresAtUtc > entitlement.ExpiresAtUtc.Value)
        {
            entitlement.ExpiresAtUtc = schedule.ExpiresAtUtc;
            entitlement.Reason = CreateActivationReason(billingEvent);
            entitlement.UpdatedAt = nowUtc;
            effectiveExpiresAtUtc = entitlement.ExpiresAtUtc;
            entitlementChanged = true;
        }
        else
        {
            effectiveExpiresAtUtc = entitlement.ExpiresAtUtc;
        }

        billingEvent.Status = SubscriptionConstants.BillingEventStatuses.Processed;
        billingEvent.ProcessedAtUtc = nowUtc;
        billingEvent.ErrorMessage = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return entitlementChanged
            ? ActivationEventOutcome.Activated(effectiveExpiresAtUtc)
            : ActivationEventOutcome.AlreadyCurrent(effectiveExpiresAtUtc);
    }


    private async Task<ActivationEventOutcome> RevokeAdjustmentBillingEventAsync(
        Guid billingEventId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var billingEvent = await dbContext.BillingEvents.SingleOrDefaultAsync(
            candidate => candidate.Id == billingEventId,
            cancellationToken);
        if (billingEvent is null || !IsAdjustmentEvent(billingEvent.EventType))
        {
            await transaction.CommitAsync(cancellationToken);
            return ActivationEventOutcome.AlreadySkipped();
        }

        var outcome = await ProcessAdjustmentBillingEventAsync(billingEvent, DateTimeOffset.UtcNow, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return outcome;
    }

    private async Task<ActivationEventOutcome> ProcessAdjustmentBillingEventAsync(
        BillingEventEntity billingEvent,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (!TryReadMetadata(billingEvent.SafeMetadataJson, out var metadata))
        {
            MarkBlocked(billingEvent, nowUtc, SubscriptionConstants.BillingEventActivation.InvalidBillingEventMetadataMessage);
            await dbContext.SaveChangesAsync(cancellationToken);
            return ActivationEventOutcome.Blocked();
        }

        var isChargeback = string.Equals(metadata.AdjustmentAction, "chargeback", StringComparison.OrdinalIgnoreCase)
            || string.Equals(metadata.AdjustmentAction, "chargeback_warning", StringComparison.OrdinalIgnoreCase);
        var isApprovedRefund = string.Equals(metadata.AdjustmentAction, "refund", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(metadata.AdjustmentStatus, "pending_approval", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(metadata.AdjustmentStatus, "rejected", StringComparison.OrdinalIgnoreCase);
        var isFullRefund = isApprovedRefund
            && (string.Equals(metadata.AdjustmentType, "full", StringComparison.OrdinalIgnoreCase)
                || (metadata.AdjustmentAmountMinor.HasValue && metadata.AmountMinor.HasValue
                    && Math.Abs(metadata.AdjustmentAmountMinor.Value) >= Math.Abs(metadata.AmountMinor.Value)));

        if (!isChargeback && !isFullRefund)
        {
            billingEvent.Status = SubscriptionConstants.BillingEventStatuses.Processed;
            billingEvent.ProcessedAtUtc = nowUtc;
            billingEvent.ErrorMessage = SubscriptionConstants.BillingEventActivation.PartialRefundManualReviewMessage;
            await dbContext.SaveChangesAsync(cancellationToken);
            LogAdjustmentDiagnostics(billingEvent, metadata, metadata.UserResolutionSource ?? "none", null, isFullRefund, isChargeback, "skipped", null, 0, 0);
            return ActivationEventOutcome.AlreadySkipped();
        }

        var resolution = await ResolveAdjustmentUserIdAsync(metadata, cancellationToken);
        var userId = resolution.UserId;
        if (userId is null)
        {
            MarkBlocked(billingEvent, nowUtc, SubscriptionConstants.BillingEventReconciliation.MissingInternalUserIdMessage);
            await dbContext.SaveChangesAsync(cancellationToken);
            LogAdjustmentDiagnostics(billingEvent, metadata, resolution.Source, null, isFullRefund, isChargeback, "blocked", SubscriptionConstants.BillingEventReconciliation.MissingInternalUserIdMessage, 0, 0);
            return ActivationEventOutcome.Blocked();
        }

        var entitlements = await dbContext.Entitlements
            .Where(entitlement => entitlement.UserId == userId.Value
                && entitlement.PlanId == SubscriptionConstants.Plans.PremiumPlanId
                && entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType
                && entitlement.Source == SubscriptionConstants.Entitlements.SourceProviderEvent
                && entitlement.Status == SubscriptionConstants.Entitlements.StatusActive
                && entitlement.StartsAtUtc <= nowUtc
                && (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc > nowUtc))
            .ToListAsync(cancellationToken);

        var reason = isChargeback
            ? SubscriptionConstants.BillingEventActivation.ChargebackRevokedReason
            : SubscriptionConstants.BillingEventActivation.FullRefundRevokedReason;

        foreach (var entitlement in entitlements)
        {
            entitlement.Status = SubscriptionConstants.Entitlements.StatusExpired;
            entitlement.ExpiresAtUtc = nowUtc;
            entitlement.Reason = $"{reason}; Provider={billingEvent.BillingProvider}; ProviderEventId={billingEvent.ProviderEventId}.";
            entitlement.UpdatedAt = nowUtc;
        }

        dbContext.AdminActions.Add(new AdminActionEntity
        {
            Id = Guid.NewGuid(),
            AdminUserId = null,
            TargetUserId = userId.Value,
            ActionType = isChargeback
                ? SubscriptionConstants.AdminActionTypes.PaddleChargebackPremiumRevoke
                : SubscriptionConstants.AdminActionTypes.PaddleFullRefundPremiumRevoke,
            Reason = reason,
            CreatedAtUtc = nowUtc,
            SafeMetadataJson = JsonSerializer.Serialize(new
            {
                billingProvider = billingEvent.BillingProvider,
                providerEventId = billingEvent.ProviderEventId,
                eventType = billingEvent.EventType,
                paddleTransactionId = metadata.PaddleTransactionId,
                paddleSubscriptionId = metadata.PaddleSubscriptionId,
                adjustmentAction = metadata.AdjustmentAction,
                adjustmentStatus = metadata.AdjustmentStatus,
                revokedEntitlementCount = entitlements.Count
            }, MetadataJsonOptions)
        });

        billingEvent.Status = SubscriptionConstants.BillingEventStatuses.Processed;
        billingEvent.ProcessedAtUtc = nowUtc;
        billingEvent.ErrorMessage = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        LogAdjustmentDiagnostics(billingEvent, metadata, resolution.Source, userId, isFullRefund, isChargeback, "processed", null, entitlements.Count, entitlements.Count);
        return entitlements.Count > 0 ? ActivationEventOutcome.Activated(nowUtc) : ActivationEventOutcome.AlreadyCurrent(nowUtc);
    }

    private async Task<AdjustmentUserResolution> ResolveAdjustmentUserIdAsync(BillingEventActivationSafeMetadata metadata, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(metadata.InternalUserId, out var parsed))
        {
            metadata.UserResolutionSource = "metadata";
            return new AdjustmentUserResolution(parsed, "metadata");
        }

        if (!string.IsNullOrWhiteSpace(metadata.PaddleTransactionId))
        {
            var paymentUserId = await dbContext.Payments
                .AsNoTracking()
                .Where(payment => payment.Provider == SubscriptionConstants.BillingProviders.Paddle
                    && payment.ProviderPaymentId == metadata.PaddleTransactionId)
                .Select(payment => (Guid?)payment.UserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (paymentUserId is not null)
            {
                metadata.UserResolutionSource = "payment";
                return new AdjustmentUserResolution(paymentUserId, "payment");
            }
        }

        if (!string.IsNullOrWhiteSpace(metadata.PaddleSubscriptionId))
        {
            var subscriptionUserId = await dbContext.Subscriptions
                .AsNoTracking()
                .Where(subscription => subscription.Provider == SubscriptionConstants.BillingProviders.Paddle
                    && subscription.ProviderSubscriptionId == metadata.PaddleSubscriptionId)
                .Select(subscription => (Guid?)subscription.UserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (subscriptionUserId is not null)
            {
                metadata.UserResolutionSource = "subscription";
                return new AdjustmentUserResolution(subscriptionUserId, "subscription");
            }

            var entitlementUserId = await dbContext.Entitlements
                .AsNoTracking()
                .Where(entitlement => entitlement.Subscription != null
                    && entitlement.Subscription.Provider == SubscriptionConstants.BillingProviders.Paddle
                    && entitlement.Subscription.ProviderSubscriptionId == metadata.PaddleSubscriptionId
                    && entitlement.PlanId == SubscriptionConstants.Plans.PremiumPlanId
                    && entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType
                    && entitlement.Source == SubscriptionConstants.Entitlements.SourceProviderEvent
                    && entitlement.Status == SubscriptionConstants.Entitlements.StatusActive)
                .Select(entitlement => (Guid?)entitlement.UserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (entitlementUserId is not null)
            {
                metadata.UserResolutionSource = "entitlement";
                return new AdjustmentUserResolution(entitlementUserId, "entitlement");
            }
        }

        metadata.UserResolutionSource = "none";
        return new AdjustmentUserResolution(null, "none");
    }

    private void LogAdjustmentDiagnostics(
        BillingEventEntity billingEvent,
        BillingEventActivationSafeMetadata metadata,
        string userResolutionSource,
        Guid? resolvedUserId,
        bool fullRefundDetected,
        bool chargebackDetected,
        string decision,
        string? blockReasonCode,
        int entitlementCandidatesCount,
        int revokedCount)
    {
        logger.LogInformation(
            "Billing adjustment entitlement diagnostics. EventType={EventType}; ProviderEventId={ProviderEventId}; ProviderTransactionId={ProviderTransactionId}; ProviderSubscriptionId={ProviderSubscriptionId}; InternalUserIdPresent={InternalUserIdPresent}; UserResolutionSource={UserResolutionSource}; ResolvedUserId={ResolvedUserId}; FullRefundDetected={FullRefundDetected}; ChargebackDetected={ChargebackDetected}; ReconciliationDecision={ReconciliationDecision}; BlockReasonCode={BlockReasonCode}; EntitlementCandidatesCount={EntitlementCandidatesCount}; RevokedCount={RevokedCount}.",
            billingEvent.EventType,
            billingEvent.ProviderEventId,
            metadata.PaddleTransactionId,
            metadata.PaddleSubscriptionId,
            !string.IsNullOrWhiteSpace(metadata.InternalUserId),
            userResolutionSource,
            resolvedUserId,
            fullRefundDetected,
            chargebackDetected,
            decision,
            blockReasonCode,
            entitlementCandidatesCount,
            revokedCount);
    }

    private static bool IsAdjustmentEvent(string eventType)
    {
        return string.Equals(eventType, SubscriptionConstants.BillingEventTypes.AdjustmentCreated, StringComparison.OrdinalIgnoreCase)
            || string.Equals(eventType, SubscriptionConstants.BillingEventTypes.AdjustmentUpdated, StringComparison.OrdinalIgnoreCase);
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

        if (!MatchesExpectedPrice(metadata.PaddlePriceId))
        {
            return ActivationValidationResult.Invalid(SubscriptionConstants.BillingEventReconciliation.UnsupportedPriceIdMessage);
        }

        if (!MatchesExpectedProduct(metadata.PaddleProductId))
        {
            return ActivationValidationResult.Invalid(SubscriptionConstants.BillingEventReconciliation.UnsupportedProductIdMessage);
        }

        if (!MatchesExpectedCustomData(metadata.CustomDataApp, metadata.CustomDataProduct))
        {
            return ActivationValidationResult.Invalid(SubscriptionConstants.BillingEventReconciliation.UnsupportedCustomDataMessage);
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

    private bool MatchesExpectedPrice(string? priceId)
    {
        var expected = GetExpectedPremiumPriceId();
        return !string.IsNullOrWhiteSpace(expected)
            && string.Equals(priceId, expected, StringComparison.Ordinal);
    }

    private bool MatchesExpectedProduct(string? productId)
    {
        var expected = GetExpectedPremiumProductId();
        return string.IsNullOrWhiteSpace(expected)
            || string.Equals(productId, expected, StringComparison.Ordinal);
    }

    private bool MatchesExpectedCustomData(string? app, string? product)
    {
        return string.Equals(app, ExpectedCustomDataApp(), StringComparison.Ordinal)
            && string.Equals(product, ExpectedCustomDataProduct(), StringComparison.Ordinal);
    }

    private string GetExpectedPremiumPriceId()
    {
        var livePriceId = string.IsNullOrWhiteSpace(paddleOptions.PremiumLivePriceId) ? paddleOptions.PremiumPriceId : paddleOptions.PremiumLivePriceId;
        return string.Equals(paddleOptions.Environment, SubscriptionConstants.Billing.LivePaddleEnvironment, StringComparison.OrdinalIgnoreCase)
            ? livePriceId.Trim()
            : paddleOptions.PremiumPriceId.Trim();
    }

    private string GetExpectedPremiumProductId()
    {
        var liveProductId = string.IsNullOrWhiteSpace(paddleOptions.PremiumLiveProductId) ? paddleOptions.PremiumProductId : paddleOptions.PremiumLiveProductId;
        return string.Equals(paddleOptions.Environment, SubscriptionConstants.Billing.LivePaddleEnvironment, StringComparison.OrdinalIgnoreCase)
            ? liveProductId.Trim()
            : paddleOptions.PremiumProductId.Trim();
    }

    private string ExpectedCustomDataApp() => string.IsNullOrWhiteSpace(paddleOptions.ExpectedCustomDataApp) ? "language_voice_tutor" : paddleOptions.ExpectedCustomDataApp.Trim();

    private string ExpectedCustomDataProduct() => string.IsNullOrWhiteSpace(paddleOptions.ExpectedCustomDataProduct) ? "language_voice_tutor_pro" : paddleOptions.ExpectedCustomDataProduct.Trim();

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


    private async Task<ProviderEntitlementSchedule> CalculateStackedProviderEntitlementScheduleAsync(
        Guid userId,
        DateTimeOffset? billingPeriodStartsAtUtc,
        DateTimeOffset billingPeriodEndsAtUtc,
        EntitlementEntity? existingProviderEventEntitlement,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var providerPaidPeriodStartsAtUtc = billingPeriodStartsAtUtc ?? nowUtc;
        var providerPaidDuration = billingPeriodEndsAtUtc - providerPaidPeriodStartsAtUtc;
        var latestActiveAccessEndUtc = await FindLatestActiveAccessEndAsync(userId, nowUtc, cancellationToken);
        var stackStartsAtUtc = latestActiveAccessEndUtc is null
            ? MaxDateTimeOffset(providerPaidPeriodStartsAtUtc, nowUtc)!.Value
            : MaxDateTimeOffset(nowUtc, latestActiveAccessEndUtc)!.Value;

        if (existingProviderEventEntitlement?.ExpiresAtUtc is not null)
        {
            stackStartsAtUtc = MaxDateTimeOffset(stackStartsAtUtc, existingProviderEventEntitlement.ExpiresAtUtc.Value)!.Value;
        }

        return new ProviderEntitlementSchedule(
            stackStartsAtUtc,
            stackStartsAtUtc.Add(providerPaidDuration));
    }

    private async Task<DateTimeOffset?> FindLatestActiveAccessEndAsync(
        Guid userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var latestTrialGrantEndUtc = await dbContext.TrialGrants
            .AsNoTracking()
            .Where(trial => trial.UserId == userId
                && trial.Status == SubscriptionConstants.Entitlements.StatusActive
                && trial.GrantedAtUtc <= nowUtc
                && trial.ExpiresAtUtc > nowUtc)
            .MaxAsync(trial => (DateTimeOffset?)trial.ExpiresAtUtc, cancellationToken);

        var latestPremiumEntitlementEndUtc = await dbContext.Entitlements
            .AsNoTracking()
            .Where(entitlement => entitlement.UserId == userId
                && entitlement.PlanId == SubscriptionConstants.Plans.PremiumPlanId
                && entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType
                && entitlement.Status == SubscriptionConstants.Entitlements.StatusActive
                && entitlement.StartsAtUtc <= nowUtc
                && entitlement.ExpiresAtUtc.HasValue
                && entitlement.ExpiresAtUtc.Value > nowUtc)
            .MaxAsync(entitlement => entitlement.ExpiresAtUtc, cancellationToken);

        return MaxDateTimeOffset(latestTrialGrantEndUtc, latestPremiumEntitlementEndUtc);
    }

    private Task<EntitlementEntity?> FindCurrentOrScheduledProviderEventEntitlementAsync(
        Guid userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        return dbContext.Entitlements
            .Where(entitlement => entitlement.UserId == userId
                && entitlement.PlanId == SubscriptionConstants.Plans.PremiumPlanId
                && entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType
                && entitlement.Source == SubscriptionConstants.Entitlements.SourceProviderEvent
                && entitlement.Status == SubscriptionConstants.Entitlements.StatusActive
                && (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc > nowUtc))
            .OrderByDescending(entitlement => entitlement.ExpiresAtUtc == null)
            .ThenByDescending(entitlement => entitlement.ExpiresAtUtc)
            .ThenBy(entitlement => entitlement.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
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


    private sealed record ProviderEntitlementSchedule(DateTimeOffset StartsAtUtc, DateTimeOffset ExpiresAtUtc);

    private sealed record ActivationEventOutcome(ActivationEventResult Result, DateTimeOffset? EntitlementExpiresAtUtc)
    {
        public static ActivationEventOutcome Activated(DateTimeOffset? entitlementExpiresAtUtc) =>
            new(ActivationEventResult.Activated, entitlementExpiresAtUtc);

        public static ActivationEventOutcome AlreadyCurrent(DateTimeOffset? entitlementExpiresAtUtc) =>
            new(ActivationEventResult.AlreadySkipped, entitlementExpiresAtUtc);

        public static ActivationEventOutcome AlreadySkipped() =>
            new(ActivationEventResult.AlreadySkipped, null);

        public static ActivationEventOutcome Blocked() =>
            new(ActivationEventResult.Blocked, null);
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
        public string? PaddlePriceId { get; set; }
        public string? PaddleProductId { get; set; }
        public string? CustomDataApp { get; set; }
        public string? CustomDataProduct { get; set; }
        public string? PaddleTransactionId { get; set; }
        public string? PaddleSubscriptionId { get; set; }
        public long? AmountMinor { get; set; }
        public string? AdjustmentAction { get; set; }
        public string? AdjustmentStatus { get; set; }
        public string? AdjustmentType { get; set; }
        public long? AdjustmentAmountMinor { get; set; }
        public string? UserResolutionSource { get; set; }
    }

    private sealed record AdjustmentUserResolution(Guid? UserId, string Source);

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
