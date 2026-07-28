using System.Data;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class ProviderSubscriptionPeriodPersistenceService(
    AppDbContext dbContext,
    ILogger<ProviderSubscriptionPeriodPersistenceService> logger) : IProviderSubscriptionPeriodPersistenceService
{
    private const string VerifiedPeriodReason = "provider_scoped_verified_period";

    public async Task<ProviderSubscriptionPeriodPersistenceResult> ApplyAsync(
        ProviderSubscriptionPeriodPersistenceRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.IsTestPurchase) return Result(ProviderSubscriptionPeriodPersistenceResultCode.TestPurchaseNotSupported);
        if (!HasValidInput(request)) return Result(ProviderSubscriptionPeriodPersistenceResultCode.InvalidInput);

        try
        {
            return await BillingSerializableTransactionRetryPolicy.ExecuteAsync(
                (_, retryCancellationToken) => ApplyWithOwnedTransactionAsync(request, retryCancellationToken),
                dbContext.ChangeTracker.Clear,
                logger,
                "Provider-scoped subscription period persistence",
                request.SubscriptionId,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            dbContext.ChangeTracker.Clear();
            logger.LogWarning("Provider-scoped subscription period persistence completed with safe result {ResultCode}.", ProviderSubscriptionPeriodPersistenceResultCode.TemporarilyUnavailable);
            return Result(ProviderSubscriptionPeriodPersistenceResultCode.TemporarilyUnavailable);
        }
    }

    private async Task<ProviderSubscriptionPeriodPersistenceResult> ApplyWithOwnedTransactionAsync(
        ProviderSubscriptionPeriodPersistenceRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var result = await ApplyWithinTransactionAsync(request, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    internal async Task<ProviderSubscriptionPeriodPersistenceResult> ApplyWithinTransactionAsync(
        ProviderSubscriptionPeriodPersistenceRequest request,
        CancellationToken cancellationToken)
    {
        var subscription = await dbContext.Subscriptions.SingleOrDefaultAsync(candidate => candidate.Id == request.SubscriptionId, cancellationToken);
        if (subscription is null) return Result(ProviderSubscriptionPeriodPersistenceResultCode.SubscriptionNotFound);
        if (subscription.UserId != request.UserId) return Result(ProviderSubscriptionPeriodPersistenceResultCode.SubscriptionOwnershipConflict);
        if (!IsSupportedSubscription(subscription)) return Result(ProviderSubscriptionPeriodPersistenceResultCode.UnsupportedSubscription);

        var matchingEntitlement = await dbContext.Entitlements.SingleOrDefaultAsync(candidate =>
            candidate.UserId == request.UserId
            && candidate.SubscriptionId == subscription.Id
            && candidate.PlanId == SubscriptionConstants.Plans.PremiumPlanId
            && candidate.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType
            && candidate.Source == SubscriptionConstants.Entitlements.SourceProviderEvent
            && candidate.Status == SubscriptionConstants.Entitlements.StatusActive,
            cancellationToken);

        var nowUtc = DateTimeOffset.UtcNow;
        var changed = false;
        if (subscription.CurrentPeriodEndUtc is null || request.PeriodExpiresAtUtc > subscription.CurrentPeriodEndUtc.Value)
        {
            subscription.ProviderProductId = string.IsNullOrWhiteSpace(request.ProviderProductId) ? subscription.ProviderProductId : request.ProviderProductId;
            subscription.CurrentPeriodStartUtc = request.PeriodStartsAtUtc;
            subscription.CurrentPeriodEndUtc = request.PeriodExpiresAtUtc;
            if (subscription.ExpiresAt is null || request.PeriodExpiresAtUtc > subscription.ExpiresAt.Value)
            {
                subscription.ExpiresAt = request.PeriodExpiresAtUtc;
            }
            subscription.Status = SubscriptionConstants.SubscriptionStatuses.Active;
            subscription.UpdatedAt = nowUtc;
            changed = true;
        }

        if (matchingEntitlement is null)
        {
            dbContext.Entitlements.Add(new EntitlementEntity
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                SubscriptionId = subscription.Id,
                PlanId = SubscriptionConstants.Plans.PremiumPlanId,
                EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
                Source = SubscriptionConstants.Entitlements.SourceProviderEvent,
                Status = SubscriptionConstants.Entitlements.StatusActive,
                StartsAtUtc = request.PeriodStartsAtUtc,
                ExpiresAtUtc = request.PeriodExpiresAtUtc,
                Reason = VerifiedPeriodReason,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            });
            changed = true;
        }
        else if (matchingEntitlement.ExpiresAtUtc is not null && request.PeriodExpiresAtUtc > matchingEntitlement.ExpiresAtUtc.Value)
        {
            matchingEntitlement.ExpiresAtUtc = request.PeriodExpiresAtUtc;
            matchingEntitlement.UpdatedAt = nowUtc;
            changed = true;
        }

        if (changed) await dbContext.SaveChangesAsync(cancellationToken);
        return Result(changed ? ProviderSubscriptionPeriodPersistenceResultCode.Applied : ProviderSubscriptionPeriodPersistenceResultCode.AlreadyCurrent);
    }

    private static bool HasValidInput(ProviderSubscriptionPeriodPersistenceRequest request) =>
        request.UserId != Guid.Empty
        && request.SubscriptionId != Guid.Empty
        && request.PeriodStartsAtUtc.Offset == TimeSpan.Zero
        && request.PeriodExpiresAtUtc.Offset == TimeSpan.Zero
        && request.PeriodExpiresAtUtc > request.PeriodStartsAtUtc;

    private static bool IsSupportedSubscription(SubscriptionEntity subscription) =>
        string.Equals(subscription.PlanId, SubscriptionConstants.Plans.PremiumPlanId, StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId)
        && !string.IsNullOrWhiteSpace(subscription.Provider)
        && !string.Equals(subscription.Provider, SubscriptionConstants.BillingProviders.None, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(subscription.Provider, SubscriptionConstants.BillingProviders.Manual, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(subscription.Provider, SubscriptionConstants.BillingProviders.InternalTrial, StringComparison.OrdinalIgnoreCase);

    private static ProviderSubscriptionPeriodPersistenceResult Result(ProviderSubscriptionPeriodPersistenceResultCode code) => new(code);
}
