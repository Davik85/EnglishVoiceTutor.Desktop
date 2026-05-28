using System.Text.Json;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IPaddleWebhookIngestionService
{
    Task<PaddleWebhookIngestionResult> IngestAsync(
        string rawBody,
        string? signatureHeader,
        DateTimeOffset receivedAtUtc,
        CancellationToken cancellationToken);
}

public sealed class PaddleWebhookIngestionService : IPaddleWebhookIngestionService
{
    private const string ReceivedStatus = "received";
    private readonly AppDbContext dbContext;
    private readonly ILogger<PaddleWebhookIngestionService> logger;

    public PaddleWebhookIngestionService(AppDbContext dbContext, ILogger<PaddleWebhookIngestionService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<PaddleWebhookIngestionResult> IngestAsync(
        string rawBody,
        string? signatureHeader,
        DateTimeOffset receivedAtUtc,
        CancellationToken cancellationToken)
    {
        PaddleWebhookMetadata metadata;
        try
        {
            metadata = ExtractMetadata(rawBody);
        }
        catch (JsonException)
        {
            return PaddleWebhookIngestionResult.InvalidJson("Paddle webhook payload is not valid JSON.");
        }
        catch (PaddleWebhookIngestionValidationException exception)
        {
            return PaddleWebhookIngestionResult.InvalidJson(exception.Message);
        }

        var duplicateExists = await dbContext.PaddleWebhookEvents
            .AsNoTracking()
            .AnyAsync(webhookEvent => webhookEvent.PaddleEventId == metadata.EventId, cancellationToken);
        if (duplicateExists)
        {
            logger.LogInformation(
                "Duplicate Paddle webhook event received. EventId={PaddleEventId}; EventType={EventType}; TransactionId={PaddleTransactionId}; SubscriptionId={PaddleSubscriptionId}; InternalUserId={InternalUserId}.",
                metadata.EventId,
                metadata.EventType,
                metadata.TransactionId,
                metadata.SubscriptionId,
                metadata.InternalUserId);

            return PaddleWebhookIngestionResult.Duplicate(metadata.EventId);
        }

        var entity = new PaddleWebhookEventEntity
        {
            Id = Guid.NewGuid(),
            PaddleEventId = metadata.EventId,
            EventType = metadata.EventType,
            OccurredAtUtc = metadata.OccurredAtUtc,
            ReceivedAtUtc = receivedAtUtc,
            ProcessedAtUtc = null,
            ProcessingStatus = ReceivedStatus,
            PaddleNotificationId = metadata.NotificationId,
            PaddleTransactionId = metadata.TransactionId,
            PaddleSubscriptionId = metadata.SubscriptionId,
            PaddleCustomerId = metadata.CustomerId,
            InternalUserId = metadata.InternalUserId,
            InternalPlanId = metadata.InternalPlanId,
            RawPayload = rawBody,
            SignatureHeader = signatureHeader,
            CreatedAt = receivedAtUtc,
            UpdatedAt = receivedAtUtc
        };

        dbContext.PaddleWebhookEvents.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            logger.LogInformation(
                "Duplicate Paddle webhook event raced with another insert. EventId={PaddleEventId}; EventType={EventType}; TransactionId={PaddleTransactionId}; SubscriptionId={PaddleSubscriptionId}; InternalUserId={InternalUserId}.",
                metadata.EventId,
                metadata.EventType,
                metadata.TransactionId,
                metadata.SubscriptionId,
                metadata.InternalUserId);

            return PaddleWebhookIngestionResult.Duplicate(metadata.EventId);
        }

        logger.LogInformation(
            "Paddle webhook event stored. EventId={PaddleEventId}; EventType={EventType}; TransactionId={PaddleTransactionId}; SubscriptionId={PaddleSubscriptionId}; InternalUserId={InternalUserId}.",
            metadata.EventId,
            metadata.EventType,
            metadata.TransactionId,
            metadata.SubscriptionId,
            metadata.InternalUserId);

        return PaddleWebhookIngestionResult.Accepted(metadata.EventId);
    }

    private static PaddleWebhookMetadata ExtractMetadata(string rawBody)
    {
        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new PaddleWebhookIngestionValidationException("Paddle webhook payload must be a JSON object.");
        }

        var eventId = GetString(root, "event_id");
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new PaddleWebhookIngestionValidationException("Paddle webhook event_id is required.");
        }

        var eventType = GetString(root, "event_type");
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new PaddleWebhookIngestionValidationException("Paddle webhook event_type is required.");
        }

        var data = GetObject(root, "data");
        var customData = data.HasValue ? GetObject(data.Value, "custom_data") : null;

        var occurredAtUtc = TryParseDateTimeOffset(GetString(root, "occurred_at"));
        var dataId = data.HasValue ? GetString(data.Value, "id") : null;
        var transactionId = FirstNonEmpty(
            data.HasValue ? GetString(data.Value, "transaction_id") : null,
            data.HasValue ? GetNestedString(data.Value, "transaction", "id") : null,
            eventType.StartsWith("transaction.", StringComparison.OrdinalIgnoreCase) ? dataId : null);
        var subscriptionId = FirstNonEmpty(
            data.HasValue ? GetString(data.Value, "subscription_id") : null,
            data.HasValue ? GetNestedString(data.Value, "subscription", "id") : null,
            eventType.StartsWith("subscription.", StringComparison.OrdinalIgnoreCase) ? dataId : null);
        var customerId = FirstNonEmpty(
            data.HasValue ? GetString(data.Value, "customer_id") : null,
            data.HasValue ? GetNestedString(data.Value, "customer", "id") : null,
            eventType.StartsWith("customer.", StringComparison.OrdinalIgnoreCase) ? dataId : null);

        Guid? internalUserId = null;
        var internalUserIdValue = customData.HasValue ? GetString(customData.Value, "evt_user_id") : null;
        if (Guid.TryParse(internalUserIdValue, out var parsedInternalUserId))
        {
            internalUserId = parsedInternalUserId;
        }

        return new PaddleWebhookMetadata(
            eventId.Trim(),
            eventType.Trim(),
            occurredAtUtc,
            GetString(root, "notification_id"),
            transactionId,
            subscriptionId,
            customerId,
            internalUserId,
            customData.HasValue ? GetString(customData.Value, "evt_plan_id") : null);
    }

    private static JsonElement? GetObject(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object)
        {
            return property;
        }

        return null;
    }

    private static string? GetNestedString(JsonElement element, string objectPropertyName, string stringPropertyName)
    {
        var nestedObject = GetObject(element, objectPropertyName);
        return nestedObject.HasValue ? GetString(nestedObject.Value, stringPropertyName) : null;
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

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}

public sealed record PaddleWebhookIngestionResult(
    bool IsSuccess,
    bool IsDuplicate,
    string? EventId,
    string Message,
    string? ErrorCode)
{
    public static PaddleWebhookIngestionResult Accepted(string eventId) =>
        new(true, false, eventId, "Paddle webhook event received.", null);

    public static PaddleWebhookIngestionResult Duplicate(string eventId) =>
        new(true, true, eventId, "Paddle webhook event was already received.", null);

    public static PaddleWebhookIngestionResult InvalidJson(string message) =>
        new(false, false, null, message, "invalid_json");
}

public sealed record PaddleWebhookMetadata(
    string EventId,
    string EventType,
    DateTimeOffset? OccurredAtUtc,
    string? NotificationId,
    string? TransactionId,
    string? SubscriptionId,
    string? CustomerId,
    Guid? InternalUserId,
    string? InternalPlanId);

public sealed class PaddleWebhookIngestionValidationException(string message) : Exception(message);
