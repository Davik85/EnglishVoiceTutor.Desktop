using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class BillingEventPaymentPersistenceService : IBillingEventPaymentPersistenceService
{
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

            if (payment is null)
            {
                payment = new PaymentEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = metadata.InternalUserId!.Value,
                    CreatedAt = nowUtc
                };
                dbContext.Payments.Add(payment);
            }

            var changed = ApplySnapshot(
                payment,
                billingEvent,
                metadata,
                status,
                internalSubscriptionId,
                nowUtc);

            if (!changed && dbContext.Entry(payment).State != EntityState.Added)
            {
                alreadyCurrentCount++;
                return CreateResult();
            }

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

    private static bool ApplySnapshot(
        PaymentEntity payment,
        BillingEventEntity billingEvent,
        PaymentSafeMetadata metadata,
        string status,
        Guid? internalSubscriptionId,
        DateTimeOffset nowUtc)
    {
        var changed = false;

        changed |= SetIfDifferent(payment.UserId, metadata.InternalUserId!.Value, value => payment.UserId = value);
        changed |= SetIfDifferent(payment.SubscriptionId, internalSubscriptionId, value => payment.SubscriptionId = value);
        changed |= SetIfDifferent(payment.InternalPlanId, metadata.InternalPlanId ?? string.Empty, value => payment.InternalPlanId = value);
        changed |= SetIfDifferent(payment.Provider, billingEvent.BillingProvider, value => payment.Provider = value);
        changed |= SetIfDifferent(payment.ProviderPaymentId, metadata.PaddleTransactionId, value => payment.ProviderPaymentId = value);
        changed |= SetIfDifferent(payment.ProviderCustomerId, metadata.PaddleCustomerId, value => payment.ProviderCustomerId = value);
        changed |= SetIfDifferent(payment.ProviderSubscriptionId, metadata.PaddleSubscriptionId, value => payment.ProviderSubscriptionId = value);
        changed |= SetIfDifferent(payment.ProviderPriceId, metadata.PaddlePriceId, value => payment.ProviderPriceId = value);
        changed |= SetIfDifferent(payment.ProviderProductId, metadata.PaddleProductId, value => payment.ProviderProductId = value);
        changed |= SetIfDifferent(payment.AmountMinor, metadata.AmountMinor, value => payment.AmountMinor = value);
        changed |= SetIfDifferent(payment.Amount, ConvertAmount(metadata.AmountMinor), value => payment.Amount = value);
        changed |= SetIfDifferent(payment.Currency, NormalizeCurrency(metadata.Currency), value => payment.Currency = value);
        changed |= SetIfDifferent(payment.Status, status, value => payment.Status = value);
        changed |= SetIfDifferent(payment.ProviderEventId, billingEvent.ProviderEventId, value => payment.ProviderEventId = value);
        changed |= SetIfDifferent(payment.ProviderEventType, billingEvent.EventType, value => payment.ProviderEventType = value);
        changed |= SetIfDifferent(payment.ProviderEventOccurredAtUtc, metadata.OccurredAtUtc, value => payment.ProviderEventOccurredAtUtc = value);
        changed |= SetIfDifferent(payment.SafeMetadataJson, billingEvent.SafeMetadataJson, value => payment.SafeMetadataJson = value);
        changed |= SetIfDifferent(payment.BilledAt, metadata.BilledAtUtc, value => payment.BilledAt = value);
        changed |= SetIfDifferent(payment.PaidAt, metadata.PaidAtUtc, value => payment.PaidAt = value);

        if (status == SubscriptionConstants.PaymentStatuses.Completed)
        {
            changed |= SetIfDifferent(payment.CompletedAt, metadata.CompletedAtUtc ?? metadata.OccurredAtUtc, value => payment.CompletedAt = value);
        }
        else if (status == SubscriptionConstants.PaymentStatuses.Failed)
        {
            changed |= SetIfDifferent(payment.FailedAt, metadata.FailedAtUtc ?? metadata.OccurredAtUtc, value => payment.FailedAt = value);
        }

        if (payment.CreatedAt == default)
        {
            payment.CreatedAt = nowUtc;
            changed = true;
        }

        if (changed || payment.UpdatedAt == default)
        {
            payment.UpdatedAt = nowUtc;
            changed = true;
        }

        return changed;
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

    private static bool SetIfDifferent<T>(T currentValue, T newValue, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            return false;
        }

        setter(newValue);
        return true;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }

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
