using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class PaddleAdjustmentReprocessService : IPaddleAdjustmentReprocessService
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext dbContext;
    private readonly IBillingEventEntitlementActivationService entitlementActivationService;
    private readonly ILogger<PaddleAdjustmentReprocessService> logger;

    public PaddleAdjustmentReprocessService(
        AppDbContext dbContext,
        IBillingEventEntitlementActivationService entitlementActivationService,
        ILogger<PaddleAdjustmentReprocessService> logger)
    {
        this.dbContext = dbContext;
        this.entitlementActivationService = entitlementActivationService;
        this.logger = logger;
    }

    public async Task<PaddleAdjustmentReprocessResult> ReprocessProviderEventAsync(string providerEventId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerEventId))
        {
            throw new ArgumentException("A Paddle provider event id is required.", nameof(providerEventId));
        }

        var normalizedProviderEventId = providerEventId.Trim();
        var billingEvent = await dbContext.BillingEvents.SingleOrDefaultAsync(
            candidate => candidate.BillingProvider == SubscriptionConstants.BillingProviders.Paddle
                && candidate.ProviderEventId == normalizedProviderEventId,
            cancellationToken);

        if (billingEvent is null)
        {
            var result = CreateResult(PaddleAdjustmentReprocessResults.NotFound, "provider_event_not_found", null, normalizedProviderEventId, new(), "none", null, 0, 0);
            LogResult(result);
            return result;
        }

        var metadata = ReadMetadata(billingEvent.SafeMetadataJson);
        if (!IsAdjustmentEvent(billingEvent.EventType))
        {
            var result = CreateResult(PaddleAdjustmentReprocessResults.RefusedEventType, "event_type_not_adjustment", billingEvent.EventType, normalizedProviderEventId, metadata, "none", null, 0, 0);
            LogResult(result);
            return result;
        }

        var fullRefundDetected = IsFullRefund(metadata);
        var chargebackDetected = IsChargeback(metadata);
        var resolution = await ResolveUserAsync(metadata, cancellationToken);
        var entitlementCandidatesCount = resolution.UserId is null ? 0 : await CountActiveProviderPremiumAsync(resolution.UserId.Value, cancellationToken);

        if (!fullRefundDetected && !chargebackDetected)
        {
            var result = CreateResult(PaddleAdjustmentReprocessResults.PartialRefundSkipped, null, billingEvent.EventType, normalizedProviderEventId, metadata, resolution.Source, resolution.UserId, entitlementCandidatesCount, 0);
            LogResult(result);
            return result;
        }

        if (resolution.UserId is null)
        {
            var result = CreateResult(PaddleAdjustmentReprocessResults.Blocked, "user_not_safely_resolved", billingEvent.EventType, normalizedProviderEventId, metadata, resolution.Source, resolution.UserId, entitlementCandidatesCount, 0);
            LogResult(result);
            return result;
        }

        if (entitlementCandidatesCount == 0)
        {
            var result = CreateResult(PaddleAdjustmentReprocessResults.AlreadyRevoked, null, billingEvent.EventType, normalizedProviderEventId, metadata, resolution.Source, resolution.UserId, entitlementCandidatesCount, 0);
            LogResult(result);
            return result;
        }

        var originalPaymentCount = await dbContext.Payments.CountAsync(cancellationToken);
        var originalSubscriptionCount = await dbContext.Subscriptions.CountAsync(cancellationToken);

        var activation = await entitlementActivationService.RevokeAdjustmentProviderEventAsync(SubscriptionConstants.BillingProviders.Paddle, normalizedProviderEventId, cancellationToken);
        var currentPaymentCount = await dbContext.Payments.CountAsync(cancellationToken);
        var currentSubscriptionCount = await dbContext.Subscriptions.CountAsync(cancellationToken);
        if (currentPaymentCount != originalPaymentCount || currentSubscriptionCount != originalSubscriptionCount)
        {
            var result = CreateResult(PaddleAdjustmentReprocessResults.Failed, "payment_or_subscription_history_changed", billingEvent.EventType, normalizedProviderEventId, metadata, resolution.Source, resolution.UserId, entitlementCandidatesCount, activation.ActivatedCount);
            LogResult(result);
            return result;
        }

        if (activation.FailedCount > 0)
        {
            var result = CreateResult(PaddleAdjustmentReprocessResults.Failed, "adjustment_revoke_failed", billingEvent.EventType, normalizedProviderEventId, metadata, resolution.Source, resolution.UserId, entitlementCandidatesCount, activation.ActivatedCount);
            LogResult(result);
            return result;
        }

        if (activation.BlockedCount > 0)
        {
            var result = CreateResult(PaddleAdjustmentReprocessResults.Blocked, "adjustment_revoke_blocked", billingEvent.EventType, normalizedProviderEventId, metadata, resolution.Source, resolution.UserId, entitlementCandidatesCount, activation.ActivatedCount);
            LogResult(result);
            return result;
        }

        var finalResult = activation.ActivatedCount > 0
            ? PaddleAdjustmentReprocessResults.Revoked
            : PaddleAdjustmentReprocessResults.AlreadyRevoked;
        var final = CreateResult(finalResult, null, billingEvent.EventType, normalizedProviderEventId, metadata, resolution.Source, resolution.UserId, entitlementCandidatesCount, activation.ActivatedCount);
        LogResult(final);
        return final;
    }

    private async Task<int> CountActiveProviderPremiumAsync(Guid userId, CancellationToken cancellationToken)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        return await dbContext.Entitlements.CountAsync(entitlement => entitlement.UserId == userId
            && entitlement.PlanId == SubscriptionConstants.Plans.PremiumPlanId
            && entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType
            && entitlement.Source == SubscriptionConstants.Entitlements.SourceProviderEvent
            && entitlement.Status == SubscriptionConstants.Entitlements.StatusActive
            && entitlement.StartsAtUtc <= nowUtc
            && (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc > nowUtc), cancellationToken);
    }

    private async Task<UserResolution> ResolveUserAsync(SafeMetadata metadata, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(metadata.InternalUserId, out var parsed)) return new(parsed, "metadata");
        if (!string.IsNullOrWhiteSpace(metadata.PaddleTransactionId))
        {
            var id = await dbContext.Payments.AsNoTracking().Where(p => p.Provider == SubscriptionConstants.BillingProviders.Paddle && p.ProviderPaymentId == metadata.PaddleTransactionId).Select(p => (Guid?)p.UserId).FirstOrDefaultAsync(cancellationToken);
            if (id is not null) return new(id, "payment");
        }
        if (!string.IsNullOrWhiteSpace(metadata.PaddleSubscriptionId))
        {
            var id = await dbContext.Subscriptions.AsNoTracking().Where(s => s.Provider == SubscriptionConstants.BillingProviders.Paddle && s.ProviderSubscriptionId == metadata.PaddleSubscriptionId).Select(s => (Guid?)s.UserId).FirstOrDefaultAsync(cancellationToken);
            if (id is not null) return new(id, "subscription");
            id = await dbContext.Entitlements.AsNoTracking().Where(e => e.Subscription != null && e.Subscription.Provider == SubscriptionConstants.BillingProviders.Paddle && e.Subscription.ProviderSubscriptionId == metadata.PaddleSubscriptionId && e.Source == SubscriptionConstants.Entitlements.SourceProviderEvent && e.Status == SubscriptionConstants.Entitlements.StatusActive).Select(e => (Guid?)e.UserId).FirstOrDefaultAsync(cancellationToken);
            if (id is not null) return new(id, "entitlement");
        }
        return new(null, "none");
    }

    private void LogResult(PaddleAdjustmentReprocessResult result)
    {
        logger.LogInformation("Paddle adjustment reprocess result. ProviderEventId={ProviderEventId}; EventType={EventType}; ProviderTransactionId={ProviderTransactionId}; ProviderSubscriptionId={ProviderSubscriptionId}; UserResolutionSource={UserResolutionSource}; ResolvedUserId={ResolvedUserId}; FullRefundDetected={FullRefundDetected}; ChargebackDetected={ChargebackDetected}; EntitlementCandidatesCount={EntitlementCandidatesCount}; RevokedCount={RevokedCount}; Result={Result}; BlockReason={BlockReason}.", result.ProviderEventId, result.EventType, result.ProviderTransactionId, result.ProviderSubscriptionId, result.UserResolutionSource, result.ResolvedUserId, result.FullRefundDetected, result.ChargebackDetected, result.EntitlementCandidatesCount, result.RevokedCount, result.Result, result.BlockReason);
    }

    private static PaddleAdjustmentReprocessResult CreateResult(string result, string? blockReason, string? eventType, string providerEventId, SafeMetadata metadata, string source, Guid? userId, int candidates, int revoked) => new(result, blockReason, eventType, providerEventId, metadata.PaddleTransactionId, metadata.PaddleSubscriptionId, source, userId, IsFullRefund(metadata), IsChargeback(metadata), candidates, revoked);
    private static bool IsAdjustmentEvent(string eventType) => string.Equals(eventType, SubscriptionConstants.BillingEventTypes.AdjustmentCreated, StringComparison.OrdinalIgnoreCase) || string.Equals(eventType, SubscriptionConstants.BillingEventTypes.AdjustmentUpdated, StringComparison.OrdinalIgnoreCase);
    private static bool IsChargeback(SafeMetadata metadata) => string.Equals(metadata.AdjustmentAction, "chargeback", StringComparison.OrdinalIgnoreCase) || string.Equals(metadata.AdjustmentAction, "chargeback_warning", StringComparison.OrdinalIgnoreCase);
    private static bool IsFullRefund(SafeMetadata metadata) => string.Equals(metadata.AdjustmentAction, "refund", StringComparison.OrdinalIgnoreCase) && !string.Equals(metadata.AdjustmentStatus, "pending_approval", StringComparison.OrdinalIgnoreCase) && !string.Equals(metadata.AdjustmentStatus, "rejected", StringComparison.OrdinalIgnoreCase) && (string.Equals(metadata.AdjustmentType, "full", StringComparison.OrdinalIgnoreCase) || (metadata.AdjustmentAmountMinor.HasValue && metadata.AmountMinor.HasValue && Math.Abs(metadata.AdjustmentAmountMinor.Value) >= Math.Abs(metadata.AmountMinor.Value)));
    private static SafeMetadata ReadMetadata(string? json) { try { return string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<SafeMetadata>(json, MetadataJsonOptions) ?? new(); } catch (JsonException) { return new(); } }

    private sealed record UserResolution(Guid? UserId, string Source);
    private sealed class SafeMetadata
    {
        public string? InternalUserId { get; set; }
        public string? PaddleTransactionId { get; set; }
        public string? PaddleSubscriptionId { get; set; }
        public string? AdjustmentAction { get; set; }
        public string? AdjustmentStatus { get; set; }
        public string? AdjustmentType { get; set; }
        public long? AdjustmentAmountMinor { get; set; }
        public long? AmountMinor { get; set; }
    }
}
