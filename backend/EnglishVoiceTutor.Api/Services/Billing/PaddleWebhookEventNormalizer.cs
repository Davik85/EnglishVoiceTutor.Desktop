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
        var billingPeriod = ExtractBillingPeriod(webhookEvent.RawPayload);
        var safeMetadata = new
        {
            paddleEventId = webhookEvent.PaddleEventId,
            eventType = webhookEvent.EventType,
            paddleTransactionId = webhookEvent.PaddleTransactionId,
            paddleSubscriptionId = webhookEvent.PaddleSubscriptionId,
            paddleCustomerId = webhookEvent.PaddleCustomerId,
            internalUserId = webhookEvent.InternalUserId,
            internalPlanId = webhookEvent.InternalPlanId,
            billingPeriodStartsAtUtc = billingPeriod.StartsAtUtc,
            billingPeriodEndsAtUtc = billingPeriod.EndsAtUtc,
            occurredAtUtc = webhookEvent.OccurredAtUtc,
            receivedAtUtc = webhookEvent.ReceivedAtUtc
        };

        return JsonSerializer.Serialize(safeMetadata, SafeMetadataJsonOptions);
    }

    private static BillingPeriodMetadata ExtractBillingPeriod(string rawPayload)
    {
        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !TryGetObject(root, "data", out var data))
            {
                return BillingPeriodMetadata.Empty;
            }

            if (!TryGetObject(data, "billing_period", out var billingPeriod))
            {
                return BillingPeriodMetadata.Empty;
            }

            return new BillingPeriodMetadata(
                TryParseDateTimeOffset(GetString(billingPeriod, "starts_at")),
                TryParseDateTimeOffset(GetString(billingPeriod, "ends_at")));
        }
        catch (JsonException)
        {
            return BillingPeriodMetadata.Empty;
        }
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

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static DateTimeOffset? TryParseDateTimeOffset(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed.ToUniversalTime() : null;
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
}
