using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class PaddleWebhookEventNormalizer : IPaddleWebhookEventNormalizer
{
    public const int DefaultLimit = 25;
    public const int MaxLimit = 100;

    private static readonly JsonSerializerOptions SafeMetadataJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext dbContext;
    private readonly ILogger<PaddleWebhookEventNormalizer> logger;

    public PaddleWebhookEventNormalizer(AppDbContext dbContext, ILogger<PaddleWebhookEventNormalizer> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<PaddleWebhookEventNormalizationResult> NormalizeReceivedEventsAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit, 1, MaxLimit);
        var webhookEvents = await dbContext.PaddleWebhookEvents
            .Where(webhookEvent => webhookEvent.ProcessingStatus == SubscriptionConstants.BillingEventStatuses.Received)
            .OrderBy(webhookEvent => webhookEvent.ReceivedAtUtc)
            .ThenBy(webhookEvent => webhookEvent.Id)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return await NormalizeWebhookEventsAsync(webhookEvents, cancellationToken);
    }

    public Task<PaddleWebhookEventNormalizationResult> NormalizeReceivedEventAsync(
        string paddleEventId,
        CancellationToken cancellationToken)
    {
        return NormalizeEventAsync(paddleEventId, cancellationToken);
    }

    public async Task<PaddleWebhookEventNormalizationResult> NormalizeEventAsync(
        string paddleEventId,
        CancellationToken cancellationToken)
    {
        var webhookEvent = await dbContext.PaddleWebhookEvents
            .SingleOrDefaultAsync(candidate => candidate.PaddleEventId == paddleEventId, cancellationToken);

        return await NormalizeWebhookEventsAsync(
            webhookEvent is null ? [] : [webhookEvent],
            cancellationToken);
    }

    private async Task<PaddleWebhookEventNormalizationResult> NormalizeWebhookEventsAsync(
        IReadOnlyCollection<PaddleWebhookEventEntity> webhookEvents,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var checkedCount = webhookEvents.Count;
        var normalizedCount = 0;
        var alreadyNormalizedCount = 0;
        var failedCount = 0;

        foreach (var webhookEvent in webhookEvents)
        {
            try
            {
                var billingEventExists = await dbContext.BillingEvents.AnyAsync(
                    billingEvent => billingEvent.BillingProvider == SubscriptionConstants.BillingProviders.Paddle
                        && billingEvent.ProviderEventId == webhookEvent.PaddleEventId,
                    cancellationToken);

                if (!billingEventExists)
                {
                    dbContext.BillingEvents.Add(CreateBillingEvent(webhookEvent));
                }

                MarkWebhookEventNormalized(webhookEvent, DateTimeOffset.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);

                if (billingEventExists)
                {
                    alreadyNormalizedCount++;
                }
                else
                {
                    normalizedCount++;
                }
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                dbContext.ChangeTracker.Clear();

                var markedNormalized = await MarkWebhookEventNormalizedAfterRaceAsync(webhookEvent.Id, cancellationToken);
                if (markedNormalized)
                {
                    alreadyNormalizedCount++;
                }
                else
                {
                    failedCount++;
                }

                logger.LogInformation(
                    "Paddle webhook normalization found an existing billing event after a uniqueness race. EventId={PaddleEventId}; EventType={EventType}.",
                    webhookEvent.PaddleEventId,
                    webhookEvent.EventType);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                dbContext.ChangeTracker.Clear();
                failedCount++;
                await TryMarkWebhookEventNormalizationFailedAsync(webhookEvent.Id, cancellationToken);

                logger.LogError(
                    exception,
                    "Paddle webhook normalization failed. EventId={PaddleEventId}; EventType={EventType}; TransactionId={PaddleTransactionId}; SubscriptionId={PaddleSubscriptionId}; InternalUserId={InternalUserId}.",
                    webhookEvent.PaddleEventId,
                    webhookEvent.EventType,
                    webhookEvent.PaddleTransactionId,
                    webhookEvent.PaddleSubscriptionId,
                    webhookEvent.InternalUserId);
            }
        }

        var completedAtUtc = DateTimeOffset.UtcNow;
        return new PaddleWebhookEventNormalizationResult(
            checkedCount,
            normalizedCount,
            alreadyNormalizedCount,
            failedCount,
            startedAtUtc,
            completedAtUtc);
    }

    private static BillingEventEntity CreateBillingEvent(PaddleWebhookEventEntity webhookEvent)
    {
        return new BillingEventEntity
        {
            Id = Guid.NewGuid(),
            BillingProvider = SubscriptionConstants.BillingProviders.Paddle,
            EventType = webhookEvent.EventType,
            ProviderEventId = webhookEvent.PaddleEventId,
            ReceivedAtUtc = webhookEvent.ReceivedAtUtc,
            ProcessedAtUtc = null,
            Status = SubscriptionConstants.BillingEventStatuses.Received,
            SafeMetadataJson = CreateSafeMetadataJson(webhookEvent),
            ErrorMessage = null
        };
    }

    private static string CreateSafeMetadataJson(PaddleWebhookEventEntity webhookEvent)
    {
        var subscriptionSnapshot = ExtractSubscriptionSnapshot(webhookEvent.RawPayload);
        var transactionSnapshot = ExtractTransactionSnapshot(webhookEvent.RawPayload);
        var adjustmentSnapshot = ExtractAdjustmentSnapshot(webhookEvent.RawPayload);
        var safeMetadata = new
        {
            paddleEventId = webhookEvent.PaddleEventId,
            eventType = webhookEvent.EventType,
            paddleTransactionId = webhookEvent.PaddleTransactionId,
            paddleSubscriptionId = webhookEvent.PaddleSubscriptionId,
            paddleCustomerId = webhookEvent.PaddleCustomerId,
            internalUserId = webhookEvent.InternalUserId,
            internalPlanId = webhookEvent.InternalPlanId,
            paddleStatus = FirstNonEmpty(transactionSnapshot.Status, subscriptionSnapshot.Status),
            paddlePriceId = FirstNonEmpty(transactionSnapshot.PriceId, subscriptionSnapshot.PriceId),
            paddleProductId = FirstNonEmpty(transactionSnapshot.ProductId, subscriptionSnapshot.ProductId),
            customDataApp = ExtractCustomDataValue(webhookEvent.RawPayload, "app"),
            customDataProduct = ExtractCustomDataValue(webhookEvent.RawPayload, "product"),
            adjustmentAction = adjustmentSnapshot.Action,
            adjustmentStatus = adjustmentSnapshot.Status,
            adjustmentType = adjustmentSnapshot.Type,
            adjustmentAmountMinor = adjustmentSnapshot.AmountMinor,
            adjustmentCurrency = adjustmentSnapshot.Currency,
            amountMinor = transactionSnapshot.AmountMinor,
            currency = transactionSnapshot.Currency,
            billedAtUtc = transactionSnapshot.BilledAtUtc,
            paidAtUtc = transactionSnapshot.PaidAtUtc,
            completedAtUtc = transactionSnapshot.CompletedAtUtc,
            failedAtUtc = transactionSnapshot.FailedAtUtc,
            billingPeriodStartsAtUtc = subscriptionSnapshot.BillingPeriod.StartsAtUtc,
            billingPeriodEndsAtUtc = subscriptionSnapshot.BillingPeriod.EndsAtUtc,
            cancelAtPeriodEnd = subscriptionSnapshot.CancelAtPeriodEnd,
            scheduledChangeAction = subscriptionSnapshot.ScheduledChangeAction,
            scheduledChangeEffectiveAtUtc = subscriptionSnapshot.ScheduledChangeEffectiveAtUtc,
            effectiveAtUtc = subscriptionSnapshot.EffectiveAtUtc,
            occurredAtUtc = webhookEvent.OccurredAtUtc,
            receivedAtUtc = webhookEvent.ReceivedAtUtc
        };

        return JsonSerializer.Serialize(safeMetadata, SafeMetadataJsonOptions);
    }


    private static AdjustmentSnapshotMetadata ExtractAdjustmentSnapshot(string rawPayload)
    {
        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !TryGetObject(root, "data", out var data))
            {
                return AdjustmentSnapshotMetadata.Empty;
            }

            var amountMinor = FirstLong(
                GetNestedLong(data, "totals", "total"),
                GetNestedLong(data, "payout_totals", "total"));

            return new AdjustmentSnapshotMetadata(
                GetString(data, "action"),
                GetString(data, "status"),
                ExtractAdjustmentType(data),
                amountMinor,
                FirstNonEmpty(GetString(data, "currency_code"), GetString(data, "currency")));
        }
        catch (JsonException)
        {
            return AdjustmentSnapshotMetadata.Empty;
        }
    }

    private static string? ExtractAdjustmentType(JsonElement data)
    {
        if (TryGetArray(data, "items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                var type = GetString(item, "type");
                if (!string.IsNullOrWhiteSpace(type))
                {
                    return type;
                }
            }
        }

        return null;
    }

    private static string? ExtractCustomDataValue(string rawPayload, string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object
                && TryGetObject(root, "data", out var data)
                && TryGetObject(data, "custom_data", out var customData)
                    ? GetString(customData, propertyName)
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static TransactionSnapshotMetadata ExtractTransactionSnapshot(string rawPayload)
    {
        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !TryGetObject(root, "data", out var data))
            {
                return TransactionSnapshotMetadata.Empty;
            }

            var price = ExtractPrice(data);
            var amountMinor = FirstLong(
                GetNestedLong(data, "details", "totals", "total"),
                GetNestedLong(data, "details", "totals", "grand_total"),
                GetLong(data, "amount"));

            return new TransactionSnapshotMetadata(
                GetString(data, "status"),
                price.PriceId,
                price.ProductId,
                amountMinor,
                FirstNonEmpty(
                    GetString(data, "currency_code"),
                    GetString(data, "currency"),
                    GetNestedString(data, "details", "currency_code")),
                TryParseDateTimeOffset(GetString(data, "billed_at")),
                TryParseDateTimeOffset(GetString(data, "paid_at")),
                TryParseDateTimeOffset(FirstNonEmpty(GetString(data, "completed_at"), GetString(data, "updated_at"))),
                TryParseDateTimeOffset(FirstNonEmpty(GetString(data, "failed_at"), GetString(data, "updated_at"))));
        }
        catch (JsonException)
        {
            return TransactionSnapshotMetadata.Empty;
        }
    }

    private static SubscriptionSnapshotMetadata ExtractSubscriptionSnapshot(string rawPayload)
    {
        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !TryGetObject(root, "data", out var data))
            {
                return SubscriptionSnapshotMetadata.Empty;
            }

            var billingPeriod = ExtractBillingPeriod(data);
            var scheduledChange = ExtractScheduledChange(data);
            var price = ExtractPrice(data);

            return new SubscriptionSnapshotMetadata(
                GetString(data, "status"),
                price.PriceId,
                price.ProductId,
                billingPeriod,
                GetBoolean(data, "cancel_at_period_end") ?? string.Equals(scheduledChange.Action, SubscriptionConstants.ScheduledChangeActions.Cancel, StringComparison.OrdinalIgnoreCase),
                scheduledChange.Action,
                scheduledChange.EffectiveAtUtc,
                FirstDateTimeOffset(
                    TryParseDateTimeOffset(GetString(data, "effective_at")),
                    scheduledChange.EffectiveAtUtc));
        }
        catch (JsonException)
        {
            return SubscriptionSnapshotMetadata.Empty;
        }
    }

    private static BillingPeriodMetadata ExtractBillingPeriod(JsonElement data)
    {
        if (!TryGetObject(data, "current_billing_period", out var billingPeriod)
            && !TryGetObject(data, "billing_period", out billingPeriod))
        {
            return BillingPeriodMetadata.Empty;
        }

        return new BillingPeriodMetadata(
            TryParseDateTimeOffset(GetString(billingPeriod, "starts_at")),
            TryParseDateTimeOffset(GetString(billingPeriod, "ends_at")));
    }

    private static ScheduledChangeMetadata ExtractScheduledChange(JsonElement data)
    {
        if (!TryGetObject(data, "scheduled_change", out var scheduledChange))
        {
            return ScheduledChangeMetadata.Empty;
        }

        return new ScheduledChangeMetadata(
            GetString(scheduledChange, "action"),
            TryParseDateTimeOffset(GetString(scheduledChange, "effective_at")));
    }

    private static PriceMetadata ExtractPrice(JsonElement data)
    {
        if (TryGetArray(data, "items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || !TryGetObject(item, "price", out var price))
                {
                    continue;
                }

                var priceId = GetString(price, "id");
                var productId = FirstNonEmpty(GetString(price, "product_id"), GetNestedString(price, "product", "id"));
                if (!string.IsNullOrWhiteSpace(priceId) || !string.IsNullOrWhiteSpace(productId))
                {
                    return new PriceMetadata(priceId, productId);
                }
            }
        }

        return PriceMetadata.Empty;
    }

    private static bool TryGetObject(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object)
        {
            value = property;
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryGetArray(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Array)
        {
            value = property;
            return true;
        }

        value = default;
        return false;
    }

    private static string? GetNestedString(JsonElement element, string objectPropertyName, string stringPropertyName)
    {
        return TryGetObject(element, objectPropertyName, out var nestedObject) ? GetString(nestedObject, stringPropertyName) : null;
    }

    private static long? GetNestedLong(JsonElement element, string objectPropertyName, string numberPropertyName)
    {
        if (!TryGetObject(element, objectPropertyName, out var nestedObject))
        {
            return null;
        }

        return GetLong(nestedObject, numberPropertyName);
    }

    private static long? GetNestedLong(JsonElement element, string firstObjectPropertyName, string secondObjectPropertyName, string numberPropertyName)
    {
        if (!TryGetObject(element, firstObjectPropertyName, out var firstObject)
            || !TryGetObject(firstObject, secondObjectPropertyName, out var secondObject))
        {
            return null;
        }

        return GetLong(secondObject, numberPropertyName);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static long? GetLong(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String && long.TryParse(property.GetString(), out var stringNumber))
        {
            return stringNumber;
        }

        return null;
    }

    private static bool? GetBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static DateTimeOffset? TryParseDateTimeOffset(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed.ToUniversalTime() : null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private static long? FirstLong(params long?[] values)
    {
        return values.FirstOrDefault(value => value.HasValue);
    }

    private static DateTimeOffset? FirstDateTimeOffset(params DateTimeOffset?[] values)
    {
        return values.FirstOrDefault(value => value.HasValue);
    }

    private static void MarkWebhookEventNormalized(PaddleWebhookEventEntity webhookEvent, DateTimeOffset nowUtc)
    {
        webhookEvent.ProcessingStatus = SubscriptionConstants.BillingEventStatuses.Normalized;
        webhookEvent.ProcessedAtUtc = nowUtc;
        webhookEvent.UpdatedAt = nowUtc;
    }

    private async Task<bool> MarkWebhookEventNormalizedAfterRaceAsync(Guid webhookEventId, CancellationToken cancellationToken)
    {
        var webhookEvent = await dbContext.PaddleWebhookEvents.SingleOrDefaultAsync(
            candidate => candidate.Id == webhookEventId,
            cancellationToken);
        if (webhookEvent is null)
        {
            return false;
        }

        if (webhookEvent.ProcessingStatus == SubscriptionConstants.BillingEventStatuses.Normalized)
        {
            return true;
        }

        if (webhookEvent.ProcessingStatus != SubscriptionConstants.BillingEventStatuses.Received)
        {
            return false;
        }

        MarkWebhookEventNormalized(webhookEvent, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task TryMarkWebhookEventNormalizationFailedAsync(Guid webhookEventId, CancellationToken cancellationToken)
    {
        try
        {
            var webhookEvent = await dbContext.PaddleWebhookEvents.SingleOrDefaultAsync(
                candidate => candidate.Id == webhookEventId,
                cancellationToken);
            if (webhookEvent is null)
            {
                return;
            }

            var nowUtc = DateTimeOffset.UtcNow;
            webhookEvent.ProcessingStatus = SubscriptionConstants.BillingEventStatuses.NormalizationFailed;
            webhookEvent.ProcessedAtUtc = nowUtc;
            webhookEvent.UpdatedAt = nowUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Unable to mark Paddle webhook event normalization as failed. WebhookEventId={WebhookEventId}.", webhookEventId);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private sealed record BillingPeriodMetadata(DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc)
    {
        public static BillingPeriodMetadata Empty { get; } = new(null, null);
    }

    private sealed record ScheduledChangeMetadata(string? Action, DateTimeOffset? EffectiveAtUtc)
    {
        public static ScheduledChangeMetadata Empty { get; } = new(null, null);
    }

    private sealed record PriceMetadata(string? PriceId, string? ProductId)
    {
        public static PriceMetadata Empty { get; } = new(null, null);
    }

    private sealed record AdjustmentSnapshotMetadata(
        string? Action,
        string? Status,
        string? Type,
        long? AmountMinor,
        string? Currency)
    {
        public static AdjustmentSnapshotMetadata Empty { get; } = new(null, null, null, null, null);
    }

    private sealed record TransactionSnapshotMetadata(
        string? Status,
        string? PriceId,
        string? ProductId,
        long? AmountMinor,
        string? Currency,
        DateTimeOffset? BilledAtUtc,
        DateTimeOffset? PaidAtUtc,
        DateTimeOffset? CompletedAtUtc,
        DateTimeOffset? FailedAtUtc)
    {
        public static TransactionSnapshotMetadata Empty { get; } = new(null, null, null, null, null, null, null, null, null);
    }

    private sealed record SubscriptionSnapshotMetadata(
        string? Status,
        string? PriceId,
        string? ProductId,
        BillingPeriodMetadata BillingPeriod,
        bool CancelAtPeriodEnd,
        string? ScheduledChangeAction,
        DateTimeOffset? ScheduledChangeEffectiveAtUtc,
        DateTimeOffset? EffectiveAtUtc)
    {
        public static SubscriptionSnapshotMetadata Empty { get; } = new(
            null,
            null,
            null,
            BillingPeriodMetadata.Empty,
            false,
            null,
            null,
            null);
    }
}
