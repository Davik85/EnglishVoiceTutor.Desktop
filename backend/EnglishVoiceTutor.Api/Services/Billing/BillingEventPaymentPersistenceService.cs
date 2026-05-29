using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingEventPaymentPersistenceService : IBillingEventPaymentPersistenceService
{
    private const int TimestampComparisonToleranceSeconds = 1;

    private static readonly TimeSpan TimestampComparisonTolerance = TimeSpan.FromSeconds(TimestampComparisonToleranceSeconds);
    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext dbContext;
    private readonly ILogger<BillingEventPaymentPersistenceService> logger;

    public BillingEventPaymentPersistenceService(
        AppDbContext dbContext,
        ILogger<BillingEventPaymentPersistenceService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<BillingEventPaymentPersistenceResult> ProcessProviderEventAsync(
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
            return new BillingEventPaymentPersistenceResult(0, 0, 0, 0, 0, null, startedAtUtc, completedAtUtc);
        }

        var result = await ProcessBillingEventAsync(billingEvent, cancellationToken);
        return result with { StartedAtUtc = startedAtUtc, CompletedAtUtc = DateTimeOffset.UtcNow };
    }

    private async Task<BillingEventPaymentPersistenceResult> ProcessBillingEventAsync(
        BillingEventEntity billingEvent,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var checkedCount = 1;
        var persistedOrUpdatedCount = 0;
        var alreadyCurrentCount = 0;
        var blockedCount = 0;
        var failedCount = 0;
        string? paymentStatus = null;

        try
        {
            if (!IsSupportedPaymentEventType(billingEvent.EventType))
            {
                blockedCount++;
                return CreateResult();
            }

            if (!TryReadMetadata(billingEvent.SafeMetadataJson, out var metadata))
            {
                blockedCount++;
                return CreateResult();
            }

            var validation = await ValidateMetadataAsync(metadata, cancellationToken);
            if (!validation.IsValid)
            {
                blockedCount++;
                return CreateResult();
            }

            var nowUtc = DateTimeOffset.UtcNow;
            var status = MapPaymentStatus(billingEvent.EventType);
            paymentStatus = status;
            var payment = await dbContext.Payments.SingleOrDefaultAsync(
                candidate => candidate.Provider == billingEvent.BillingProvider
                    && candidate.ProviderPaymentId == metadata.PaddleTransactionId,
                cancellationToken);

            var internalSubscriptionId = await ResolveInternalSubscriptionIdAsync(
                billingEvent.BillingProvider,
                metadata.PaddleSubscriptionId,
                cancellationToken);
            var snapshot = CreatePaymentSnapshot(
                billingEvent,
                metadata,
                status,
                internalSubscriptionId);

            if (payment is not null && HasCurrentIncomingSnapshot(payment, snapshot))
            {
                alreadyCurrentCount++;
                return CreateResult();
            }

            if (payment is null)
            {
                payment = new PaymentEntity
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = nowUtc
                };
                dbContext.Payments.Add(payment);
            }

            ApplySnapshot(
                payment,
                snapshot,
                nowUtc);

            await dbContext.SaveChangesAsync(cancellationToken);
            persistedOrUpdatedCount++;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            alreadyCurrentCount++;
            logger.LogInformation(
                exception,
                "Payment persistence found an existing payment after a uniqueness race. BillingProvider={BillingProvider}; ProviderEventId={ProviderEventId}.",
                billingEvent.BillingProvider,
                billingEvent.ProviderEventId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            dbContext.ChangeTracker.Clear();
            failedCount++;
            logger.LogError(
                exception,
                "Payment persistence failed. BillingEventId={BillingEventId}; BillingProvider={BillingProvider}; EventType={EventType}; ProviderEventId={ProviderEventId}.",
                billingEvent.Id,
                billingEvent.BillingProvider,
                billingEvent.EventType,
                billingEvent.ProviderEventId);
        }

        return CreateResult();

        BillingEventPaymentPersistenceResult CreateResult()
        {
            return new BillingEventPaymentPersistenceResult(
                checkedCount,
                persistedOrUpdatedCount,
                alreadyCurrentCount,
                blockedCount,
                failedCount,
                paymentStatus,
                startedAtUtc,
                DateTimeOffset.UtcNow);
        }
    }

    private async Task<PaymentValidationResult> ValidateMetadataAsync(
        PaymentSafeMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (metadata.InternalUserId is null)
        {
            return PaymentValidationResult.Invalid(SubscriptionConstants.PaymentPersistence.MissingInternalUserIdMessage);
        }

        if (!string.Equals(metadata.InternalPlanId, SubscriptionConstants.Plans.PremiumPlanId, StringComparison.OrdinalIgnoreCase))
        {
            return PaymentValidationResult.Invalid(SubscriptionConstants.PaymentPersistence.UnsupportedPlanIdMessage);
        }

        if (string.IsNullOrWhiteSpace(metadata.PaddleTransactionId))
        {
            return PaymentValidationResult.Invalid(SubscriptionConstants.PaymentPersistence.MissingProviderTransactionIdMessage);
        }

        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == metadata.InternalUserId.Value, cancellationToken);
        if (!userExists)
        {
            return PaymentValidationResult.Invalid(SubscriptionConstants.PaymentPersistence.UserNotFoundMessage);
        }

        return PaymentValidationResult.Valid();
    }

    private async Task<Guid?> ResolveInternalSubscriptionIdAsync(
        string billingProvider,
        string? providerSubscriptionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerSubscriptionId))
        {
            return null;
        }

        return await dbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.Provider == billingProvider
                && subscription.ProviderSubscriptionId == providerSubscriptionId)
            .Select(subscription => (Guid?)subscription.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static PaymentSnapshot CreatePaymentSnapshot(
        BillingEventEntity billingEvent,
        PaymentSafeMetadata metadata,
        string status,
        Guid? internalSubscriptionId)
    {
        var completedAtUtc = status == SubscriptionConstants.PaymentStatuses.Completed
            ? metadata.CompletedAtUtc ?? metadata.OccurredAtUtc
            : null;
        var failedAtUtc = status == SubscriptionConstants.PaymentStatuses.Failed
            ? metadata.FailedAtUtc ?? metadata.OccurredAtUtc
            : null;

        return new PaymentSnapshot(
            metadata.InternalUserId!.Value,
            internalSubscriptionId,
            metadata.InternalPlanId ?? string.Empty,
            billingEvent.BillingProvider,
            metadata.PaddleTransactionId,
            metadata.PaddleCustomerId,
            metadata.PaddleSubscriptionId,
            metadata.PaddlePriceId,
            metadata.PaddleProductId,
            metadata.AmountMinor,
            ConvertAmount(metadata.AmountMinor),
            NormalizeCurrency(metadata.Currency),
            status,
            billingEvent.ProviderEventId,
            billingEvent.EventType,
            metadata.OccurredAtUtc,
            billingEvent.SafeMetadataJson,
            metadata.BilledAtUtc,
            metadata.PaidAtUtc,
            completedAtUtc,
            failedAtUtc);
    }

    private static bool HasCurrentIncomingSnapshot(PaymentEntity payment, PaymentSnapshot snapshot)
    {
        // SubscriptionId is resolved from local subscription state, not from the incoming provider snapshot.
        // Excluding it here keeps duplicate provider events idempotent after subscription snapshot processing
        // creates the local subscription.
        return payment.UserId == snapshot.UserId
            && string.Equals(payment.InternalPlanId, snapshot.InternalPlanId, StringComparison.Ordinal)
            && string.Equals(payment.Provider, snapshot.Provider, StringComparison.Ordinal)
            && string.Equals(payment.ProviderPaymentId, snapshot.ProviderPaymentId, StringComparison.Ordinal)
            && string.Equals(payment.ProviderCustomerId, snapshot.ProviderCustomerId, StringComparison.Ordinal)
            && string.Equals(payment.ProviderSubscriptionId, snapshot.ProviderSubscriptionId, StringComparison.Ordinal)
            && string.Equals(payment.ProviderPriceId, snapshot.ProviderPriceId, StringComparison.Ordinal)
            && string.Equals(payment.ProviderProductId, snapshot.ProviderProductId, StringComparison.Ordinal)
            && payment.AmountMinor == snapshot.AmountMinor
            && payment.Amount == snapshot.Amount
            && string.Equals(payment.Currency, snapshot.Currency, StringComparison.Ordinal)
            && string.Equals(payment.Status, snapshot.Status, StringComparison.Ordinal)
            && string.Equals(payment.ProviderEventId, snapshot.ProviderEventId, StringComparison.Ordinal)
            && string.Equals(payment.ProviderEventType, snapshot.ProviderEventType, StringComparison.Ordinal)
            && AreNullableTimestampsEquivalent(payment.ProviderEventOccurredAtUtc, snapshot.ProviderEventOccurredAtUtc)
            && string.Equals(payment.SafeMetadataJson, snapshot.SafeMetadataJson, StringComparison.Ordinal)
            && AreNullableTimestampsEquivalent(payment.BilledAt, snapshot.BilledAt)
            && AreNullableTimestampsEquivalent(payment.PaidAt, snapshot.PaidAt)
            && AreNullableTimestampsEquivalent(payment.CompletedAt, snapshot.CompletedAt)
            && AreNullableTimestampsEquivalent(payment.FailedAt, snapshot.FailedAt);
    }

    private static bool AreNullableTimestampsEquivalent(DateTimeOffset? persistedValue, DateTimeOffset? incomingValue)
    {
        if (persistedValue is null || incomingValue is null)
        {
            return persistedValue is null && incomingValue is null;
        }

        var persistedUtc = persistedValue.Value.ToUniversalTime();
        var incomingUtc = incomingValue.Value.ToUniversalTime();
        return (persistedUtc - incomingUtc).Duration() <= TimestampComparisonTolerance;
    }

    private static void ApplySnapshot(
        PaymentEntity payment,
        PaymentSnapshot snapshot,
        DateTimeOffset nowUtc)
    {
        payment.UserId = snapshot.UserId;
        payment.SubscriptionId = snapshot.SubscriptionId;
        payment.InternalPlanId = snapshot.InternalPlanId;
        payment.Provider = snapshot.Provider;
        payment.ProviderPaymentId = snapshot.ProviderPaymentId;
        payment.ProviderCustomerId = snapshot.ProviderCustomerId;
        payment.ProviderSubscriptionId = snapshot.ProviderSubscriptionId;
        payment.ProviderPriceId = snapshot.ProviderPriceId;
        payment.ProviderProductId = snapshot.ProviderProductId;
        payment.AmountMinor = snapshot.AmountMinor;
        payment.Amount = snapshot.Amount;
        payment.Currency = snapshot.Currency;
        payment.Status = snapshot.Status;
        payment.ProviderEventId = snapshot.ProviderEventId;
        payment.ProviderEventType = snapshot.ProviderEventType;
        payment.ProviderEventOccurredAtUtc = snapshot.ProviderEventOccurredAtUtc;
        payment.SafeMetadataJson = snapshot.SafeMetadataJson;
        payment.BilledAt = snapshot.BilledAt;
        payment.PaidAt = snapshot.PaidAt;
        payment.CompletedAt = snapshot.CompletedAt;
        payment.FailedAt = snapshot.FailedAt;

        if (payment.CreatedAt == default)
        {
            payment.CreatedAt = nowUtc;
        }

        payment.UpdatedAt = nowUtc;
    }

    private static bool IsSupportedPaymentEventType(string eventType)
    {
        return string.Equals(eventType, SubscriptionConstants.BillingEventTypes.TransactionCompleted, StringComparison.OrdinalIgnoreCase)
            || string.Equals(eventType, SubscriptionConstants.BillingEventTypes.TransactionPaymentFailed, StringComparison.OrdinalIgnoreCase);
    }

    private static string MapPaymentStatus(string eventType)
    {
        return string.Equals(eventType, SubscriptionConstants.BillingEventTypes.TransactionPaymentFailed, StringComparison.OrdinalIgnoreCase)
            ? SubscriptionConstants.PaymentStatuses.Failed
            : SubscriptionConstants.PaymentStatuses.Completed;
    }

    private static decimal ConvertAmount(long? amountMinor)
    {
        return amountMinor.HasValue ? amountMinor.Value / 100m : 0m;
    }

    private static string NormalizeCurrency(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency) ? string.Empty : currency.Trim().ToUpperInvariant();
    }

    private static bool TryReadMetadata(string? safeMetadataJson, out PaymentSafeMetadata metadata)
    {
        metadata = new PaymentSafeMetadata();

        if (string.IsNullOrWhiteSpace(safeMetadataJson))
        {
            return false;
        }

        try
        {
            var parsedMetadata = JsonSerializer.Deserialize<PaymentSafeMetadata>(safeMetadataJson, MetadataJsonOptions);
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

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private sealed record PaymentSnapshot(
        Guid UserId,
        Guid? SubscriptionId,
        string InternalPlanId,
        string Provider,
        string? ProviderPaymentId,
        string? ProviderCustomerId,
        string? ProviderSubscriptionId,
        string? ProviderPriceId,
        string? ProviderProductId,
        long? AmountMinor,
        decimal Amount,
        string Currency,
        string Status,
        string ProviderEventId,
        string ProviderEventType,
        DateTimeOffset? ProviderEventOccurredAtUtc,
        string? SafeMetadataJson,
        DateTimeOffset? BilledAt,
        DateTimeOffset? PaidAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset? FailedAt);

    private sealed class PaymentSafeMetadata
    {
        public string? PaddleTransactionId { get; set; }
        public string? PaddleSubscriptionId { get; set; }
        public string? PaddleCustomerId { get; set; }
        public Guid? InternalUserId { get; set; }
        public string? InternalPlanId { get; set; }
        public string? PaddlePriceId { get; set; }
        public string? PaddleProductId { get; set; }
        public long? AmountMinor { get; set; }
        public string? Currency { get; set; }
        public DateTimeOffset? BilledAtUtc { get; set; }
        public DateTimeOffset? PaidAtUtc { get; set; }
        public DateTimeOffset? CompletedAtUtc { get; set; }
        public DateTimeOffset? FailedAtUtc { get; set; }
        public DateTimeOffset? OccurredAtUtc { get; set; }
    }

    private sealed record PaymentValidationResult(bool IsValid, string? ErrorMessage)
    {
        public static PaymentValidationResult Valid() => new(true, null);

        public static PaymentValidationResult Invalid(string errorMessage) => new(false, errorMessage);
    }
}
