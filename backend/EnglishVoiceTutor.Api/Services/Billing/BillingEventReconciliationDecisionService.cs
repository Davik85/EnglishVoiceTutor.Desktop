using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingEventReconciliationDecisionService : IBillingEventReconciliationDecisionService
{
    public const int DefaultProcessLimit = 25;
    public const int MaxProcessLimit = 100;

    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext dbContext;
    private readonly ILogger<BillingEventReconciliationDecisionService> logger;

    public BillingEventReconciliationDecisionService(
        AppDbContext dbContext,
        ILogger<BillingEventReconciliationDecisionService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
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
            checkedCount++;

            try
            {
                var nowUtc = DateTimeOffset.UtcNow;
                if (!string.Equals(
                    billingEvent.EventType,
                    SubscriptionConstants.BillingEventTypes.TransactionCompleted,
                    StringComparison.OrdinalIgnoreCase))
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
                else if (metadata.InternalUserId is null)
                {
                    MarkBlocked(
                        billingEvent,
                        nowUtc,
                        SubscriptionConstants.BillingEventReconciliation.MissingInternalUserIdMessage);
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
                else
                {
                    MarkPending(billingEvent, nowUtc);
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
    }
}
