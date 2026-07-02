using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingEventReconciliationDecisionService : IBillingEventReconciliationDecisionService
{
    public const int DefaultProcessLimit = 25;
    public const int MaxProcessLimit = 100;

    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext dbContext;
    private readonly ILogger<BillingEventReconciliationDecisionService> logger;
    private readonly PaddleBillingOptions paddleOptions;

    public BillingEventReconciliationDecisionService(
        AppDbContext dbContext,
        ILogger<BillingEventReconciliationDecisionService> logger,
        IOptions<PaddleBillingOptions> paddleOptions)
    {
        this.dbContext = dbContext;
        this.logger = logger;
        this.paddleOptions = paddleOptions.Value;
    }

    public async Task<BillingEventReconciliationDecisionResult> ProcessReceivedEventsAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var effectiveLimit = NormalizeLimit(limit);
        var checkedCount = 0;
        var markedPendingCount = 0;
        var ignoredCount = 0;
        var blockedCount = 0;
        var failedCount = 0;

        var billingEvents = await dbContext.BillingEvents
            .Where(billingEvent => billingEvent.BillingProvider == SubscriptionConstants.BillingProviders.Paddle
                && billingEvent.Status == SubscriptionConstants.BillingEventStatuses.Received)
            .OrderBy(billingEvent => billingEvent.ReceivedAtUtc)
            .ThenBy(billingEvent => billingEvent.Id)
            .Take(effectiveLimit)
            .ToListAsync(cancellationToken);

        foreach (var billingEvent in billingEvents)
        {
            var result = await ProcessBillingEventAsync(billingEvent, countAlreadyProcessedAsChecked: true, cancellationToken);
            checkedCount += result.CheckedCount;
            markedPendingCount += result.MarkedPendingCount;
            ignoredCount += result.IgnoredCount;
            blockedCount += result.BlockedCount;
            failedCount += result.FailedCount;
        }

        var completedAtUtc = DateTimeOffset.UtcNow;
        return new BillingEventReconciliationDecisionResult(
            checkedCount,
            markedPendingCount,
            ignoredCount,
            blockedCount,
            failedCount,
            startedAtUtc,
            completedAtUtc);
    }

    public async Task<BillingEventReconciliationDecisionResult> ProcessProviderEventAsync(
        string billingProvider,
        string providerEventId,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var billingEvent = await dbContext.BillingEvents.SingleOrDefaultAsync(
            candidate => candidate.BillingProvider == billingProvider
                && candidate.ProviderEventId == providerEventId,
            cancellationToken);

        if (billingEvent is null)
        {
            var completedAtUtc = DateTimeOffset.UtcNow;
            return new BillingEventReconciliationDecisionResult(0, 0, 0, 0, 0, startedAtUtc, completedAtUtc);
        }

        var result = await ProcessBillingEventAsync(billingEvent, countAlreadyProcessedAsChecked: true, cancellationToken);
        return result with { StartedAtUtc = startedAtUtc, CompletedAtUtc = DateTimeOffset.UtcNow };
    }

    private async Task<BillingEventReconciliationDecisionResult> ProcessBillingEventAsync(
        BillingEventEntity billingEvent,
        bool countAlreadyProcessedAsChecked,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var checkedCount = countAlreadyProcessedAsChecked || billingEvent.Status == SubscriptionConstants.BillingEventStatuses.Received ? 1 : 0;
        var markedPendingCount = 0;
        var ignoredCount = 0;
        var blockedCount = 0;
        var failedCount = 0;

        if (billingEvent.Status != SubscriptionConstants.BillingEventStatuses.Received)
        {
            var completedAtUtc = DateTimeOffset.UtcNow;
            return new BillingEventReconciliationDecisionResult(
                checkedCount,
                markedPendingCount,
                ignoredCount,
                blockedCount,
                failedCount,
                startedAtUtc,
                completedAtUtc);
        }

        try
        {
            var nowUtc = DateTimeOffset.UtcNow;
            if (!IsSupportedReconciliationEventType(billingEvent.EventType))
            {
                MarkIgnored(billingEvent, nowUtc);
                ignoredCount++;
            }
            else if (!TryReadMetadata(billingEvent.SafeMetadataJson, out var metadata))
            {
                MarkBlocked(
                    billingEvent,
                    nowUtc,
                    SubscriptionConstants.BillingEventReconciliation.InvalidBillingEventMetadataMessage);
                blockedCount++;
            }
            else if (!await EnsureSafeUserResolutionAsync(billingEvent, metadata, cancellationToken))
            {
                MarkBlocked(
                    billingEvent,
                    nowUtc,
                    SubscriptionConstants.BillingEventReconciliation.MissingInternalUserIdMessage);
                LogReconciliationDiagnostics(billingEvent, metadata, "blocked", SubscriptionConstants.BillingEventReconciliation.MissingInternalUserIdMessage);
                blockedCount++;
            }
            else if (!string.Equals(
                metadata.InternalPlanId,
                SubscriptionConstants.Plans.PremiumPlanId,
                StringComparison.OrdinalIgnoreCase))
            {
                MarkBlocked(
                    billingEvent,
                    nowUtc,
                    SubscriptionConstants.BillingEventReconciliation.UnsupportedPlanIdMessage);
                blockedCount++;
            }
            else if (!MatchesExpectedPrice(metadata.PaddlePriceId))
            {
                MarkBlocked(
                    billingEvent,
                    nowUtc,
                    SubscriptionConstants.BillingEventReconciliation.UnsupportedPriceIdMessage);
                blockedCount++;
            }
            else if (!MatchesExpectedProduct(metadata.PaddleProductId))
            {
                MarkBlocked(
                    billingEvent,
                    nowUtc,
                    SubscriptionConstants.BillingEventReconciliation.UnsupportedProductIdMessage);
                blockedCount++;
            }
            else if (!MatchesExpectedCustomData(metadata.CustomDataApp, metadata.CustomDataProduct))
            {
                MarkBlocked(
                    billingEvent,
                    nowUtc,
                    SubscriptionConstants.BillingEventReconciliation.UnsupportedCustomDataMessage);
                blockedCount++;
            }
            else
            {
                MarkPending(billingEvent, nowUtc);
                LogReconciliationDiagnostics(billingEvent, metadata, "pending", null);
                markedPendingCount++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            failedCount++;
            dbContext.ChangeTracker.Clear();
            logger.LogError(
                exception,
                "Billing event reconciliation decision failed. BillingEventId={BillingEventId}; BillingProvider={BillingProvider}; EventType={EventType}; ProviderEventId={ProviderEventId}.",
                billingEvent.Id,
                billingEvent.BillingProvider,
                billingEvent.EventType,
                billingEvent.ProviderEventId);
        }

        var finishedAtUtc = DateTimeOffset.UtcNow;
        return new BillingEventReconciliationDecisionResult(
            checkedCount,
            markedPendingCount,
            ignoredCount,
            blockedCount,
            failedCount,
            startedAtUtc,
            finishedAtUtc);
    }

    private static bool IsSupportedReconciliationEventType(string eventType)
    {
        return string.Equals(eventType, SubscriptionConstants.BillingEventTypes.TransactionCompleted, StringComparison.OrdinalIgnoreCase)
            || string.Equals(eventType, SubscriptionConstants.BillingEventTypes.AdjustmentCreated, StringComparison.OrdinalIgnoreCase)
            || string.Equals(eventType, SubscriptionConstants.BillingEventTypes.AdjustmentUpdated, StringComparison.OrdinalIgnoreCase);
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

    private async Task<bool> EnsureSafeUserResolutionAsync(
        BillingEventEntity billingEvent,
        BillingEventSafeMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (metadata.InternalUserId is not null)
        {
            metadata.UserResolutionSource = "metadata";
            return true;
        }

        if (!IsAdjustmentEvent(billingEvent.EventType))
        {
            metadata.UserResolutionSource = "none";
            return false;
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
                metadata.InternalUserId = paymentUserId;
                metadata.UserResolutionSource = "payment";
                return true;
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
                metadata.InternalUserId = subscriptionUserId;
                metadata.UserResolutionSource = "subscription";
                return true;
            }
        }

        metadata.UserResolutionSource = "none";
        return false;
    }

    private void LogReconciliationDiagnostics(BillingEventEntity billingEvent, BillingEventSafeMetadata metadata, string decision, string? blockReasonCode)
    {
        logger.LogInformation(
            "Billing event reconciliation diagnostics. EventType={EventType}; ProviderEventId={ProviderEventId}; ProviderTransactionId={ProviderTransactionId}; ProviderSubscriptionId={ProviderSubscriptionId}; InternalUserIdPresent={InternalUserIdPresent}; UserResolutionSource={UserResolutionSource}; ResolvedUserId={ResolvedUserId}; FullRefundDetected={FullRefundDetected}; ChargebackDetected={ChargebackDetected}; ReconciliationDecision={ReconciliationDecision}; BlockReasonCode={BlockReasonCode}.",
            billingEvent.EventType,
            billingEvent.ProviderEventId,
            metadata.PaddleTransactionId,
            metadata.PaddleSubscriptionId,
            metadata.InternalUserId is not null,
            metadata.UserResolutionSource ?? "none",
            metadata.InternalUserId,
            IsFullRefund(metadata),
            IsChargeback(metadata),
            decision,
            blockReasonCode);
    }

    private static bool IsAdjustmentEvent(string eventType)
    {
        return string.Equals(eventType, SubscriptionConstants.BillingEventTypes.AdjustmentCreated, StringComparison.OrdinalIgnoreCase)
            || string.Equals(eventType, SubscriptionConstants.BillingEventTypes.AdjustmentUpdated, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsChargeback(BillingEventSafeMetadata metadata)
    {
        return string.Equals(metadata.AdjustmentAction, "chargeback", StringComparison.OrdinalIgnoreCase)
            || string.Equals(metadata.AdjustmentAction, "chargeback_warning", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFullRefund(BillingEventSafeMetadata metadata)
    {
        var isApprovedRefund = string.Equals(metadata.AdjustmentAction, "refund", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(metadata.AdjustmentStatus, "pending_approval", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(metadata.AdjustmentStatus, "rejected", StringComparison.OrdinalIgnoreCase);
        return isApprovedRefund
            && (string.Equals(metadata.AdjustmentType, "full", StringComparison.OrdinalIgnoreCase)
                || (metadata.AdjustmentAmountMinor.HasValue && metadata.AmountMinor.HasValue
                    && Math.Abs(metadata.AdjustmentAmountMinor.Value) >= Math.Abs(metadata.AmountMinor.Value)));
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return DefaultProcessLimit;
        }

        return Math.Min(limit, MaxProcessLimit);
    }

    private static void MarkPending(BillingEventEntity billingEvent, DateTimeOffset nowUtc)
    {
        billingEvent.Status = SubscriptionConstants.BillingEventStatuses.ReconciliationPending;
        billingEvent.ProcessedAtUtc = nowUtc;
        billingEvent.ErrorMessage = null;
    }

    private static void MarkIgnored(BillingEventEntity billingEvent, DateTimeOffset nowUtc)
    {
        billingEvent.Status = SubscriptionConstants.BillingEventStatuses.Ignored;
        billingEvent.ProcessedAtUtc = nowUtc;
        billingEvent.ErrorMessage = SubscriptionConstants.BillingEventReconciliation.UnsupportedBillingEventTypeMessage;
    }

    private static void MarkBlocked(BillingEventEntity billingEvent, DateTimeOffset nowUtc, string reason)
    {
        billingEvent.Status = SubscriptionConstants.BillingEventStatuses.ReconciliationBlocked;
        billingEvent.ProcessedAtUtc = nowUtc;
        billingEvent.ErrorMessage = reason;
    }

    private static bool TryReadMetadata(string? safeMetadataJson, out BillingEventSafeMetadata metadata)
    {
        metadata = new BillingEventSafeMetadata();

        if (string.IsNullOrWhiteSpace(safeMetadataJson))
        {
            return false;
        }

        try
        {
            var parsedMetadata = JsonSerializer.Deserialize<BillingEventSafeMetadata>(safeMetadataJson, MetadataJsonOptions);
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

    private sealed class BillingEventSafeMetadata
    {
        public Guid? InternalUserId { get; set; }
        public string? InternalPlanId { get; set; }
        public string? PaddlePriceId { get; set; }
        public string? PaddleProductId { get; set; }
        public string? CustomDataApp { get; set; }
        public string? CustomDataProduct { get; set; }
        public string? PaddleTransactionId { get; set; }
        public string? PaddleSubscriptionId { get; set; }
        public string? AdjustmentAction { get; set; }
        public string? AdjustmentStatus { get; set; }
        public string? AdjustmentType { get; set; }
        public long? AdjustmentAmountMinor { get; set; }
        public long? AmountMinor { get; set; }
        public string? UserResolutionSource { get; set; }
    }
}
