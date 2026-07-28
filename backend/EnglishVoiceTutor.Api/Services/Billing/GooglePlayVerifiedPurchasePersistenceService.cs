using System.Data;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayVerifiedPurchasePersistenceService(AppDbContext dbContext, GooglePlayPurchaseClaimService claimService, ProviderSubscriptionPeriodPersistenceService periodService, IGooglePlayPurchaseTokenFingerprintService fingerprintService, IUtcClock utcClock, ILogger<GooglePlayVerifiedPurchasePersistenceService> logger) : IGooglePlayVerifiedPurchasePersistenceService
{
    public async Task<GooglePlayVerifiedPurchasePersistenceResult> PersistAsync(GooglePlayVerifiedPurchasePersistenceRequest request, CancellationToken cancellationToken)
    {
        if (request.VerifiedPurchase.IsTestPurchase) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.TestPurchaseNotSupported);
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.PurchaseToken) || string.IsNullOrWhiteSpace(request.VerifiedPurchase.ProductId) || request.VerifiedPurchase.StartedAtUtc.Offset != TimeSpan.Zero || request.VerifiedPurchase.ExpiresAtUtc.Offset != TimeSpan.Zero || request.VerifiedPurchase.ExpiresAtUtc <= request.VerifiedPurchase.StartedAtUtc) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.InvalidInput);
        string fingerprint;
        try { fingerprint = fingerprintService.CreateFingerprint(request.PurchaseToken); } catch (ArgumentException) { return Result(GooglePlayVerifiedPurchasePersistenceResultCode.InvalidInput); }
        try
        {
            return await BillingSerializableTransactionRetryPolicy.ExecuteAsync((_, ct) => PersistWithinTransactionAsync(request, fingerprint, ct), dbContext.ChangeTracker.Clear, logger, "Google Play verified purchase persistence", Guid.Empty, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { dbContext.ChangeTracker.Clear(); return Result(GooglePlayVerifiedPurchasePersistenceResultCode.TemporarilyUnavailable); }
    }

    private async Task<GooglePlayVerifiedPurchasePersistenceResult> PersistWithinTransactionAsync(GooglePlayVerifiedPurchasePersistenceRequest request, string fingerprint, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var existingClaim = await dbContext.GooglePlayPurchaseClaims.SingleOrDefaultAsync(item => item.PurchaseTokenFingerprint == fingerprint, cancellationToken);
        if (existingClaim is not null && existingClaim.UserId != request.UserId) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict);
        if (existingClaim is not null && !string.Equals(existingClaim.ProductId, request.VerifiedPurchase.ProductId, StringComparison.Ordinal)) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.ProductMismatch);

        var subscription = await dbContext.Subscriptions.SingleOrDefaultAsync(item => item.Provider == SubscriptionConstants.BillingProviders.GooglePlay && item.ProviderSubscriptionId == fingerprint, cancellationToken);
        if (subscription is not null
            && (subscription.UserId != request.UserId
                || subscription.PlanId != SubscriptionConstants.Plans.PremiumPlanId
                || subscription.Provider != SubscriptionConstants.BillingProviders.GooglePlay
                || subscription.ProviderSubscriptionId != fingerprint
                || (!string.IsNullOrWhiteSpace(subscription.ProviderProductId)
                    && !string.Equals(subscription.ProviderProductId, request.VerifiedPurchase.ProductId, StringComparison.Ordinal))))
        {
            return Result(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict);
        }

        var claim = await claimService.ClaimWithinTransactionAsync(request.UserId, fingerprint, request.VerifiedPurchase.ProductId, cancellationToken);
        if (claim.Code == GooglePlayPurchaseClaimResultCode.OwnershipConflict) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict);
        if (claim.Code is GooglePlayPurchaseClaimResultCode.InvalidInput or GooglePlayPurchaseClaimResultCode.TemporarilyUnavailable) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.TemporarilyUnavailable);

        if (subscription is null)
        {
            var now = utcClock.UtcNow;
            subscription = new SubscriptionEntity { Id = Guid.NewGuid(), UserId = request.UserId, PlanId = SubscriptionConstants.Plans.PremiumPlanId, Status = SubscriptionConstants.SubscriptionStatuses.Active, Provider = SubscriptionConstants.BillingProviders.GooglePlay, ProviderSubscriptionId = fingerprint, ProviderProductId = request.VerifiedPurchase.ProductId, StartedAt = request.VerifiedPurchase.StartedAtUtc, CancelAtPeriodEnd = false, CreatedAt = now, UpdatedAt = now };
            dbContext.Subscriptions.Add(subscription);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (string.IsNullOrWhiteSpace(subscription.ProviderProductId))
        {
            subscription.ProviderProductId = request.VerifiedPurchase.ProductId;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var period = await periodService.ApplyWithinTransactionAsync(new ProviderSubscriptionPeriodPersistenceRequest(request.UserId, subscription.Id, request.VerifiedPurchase.ProductId, request.VerifiedPurchase.StartedAtUtc, request.VerifiedPurchase.ExpiresAtUtc, false), cancellationToken);
        if (period.Code is ProviderSubscriptionPeriodPersistenceResultCode.InvalidInput or ProviderSubscriptionPeriodPersistenceResultCode.SubscriptionNotFound or ProviderSubscriptionPeriodPersistenceResultCode.SubscriptionOwnershipConflict or ProviderSubscriptionPeriodPersistenceResultCode.UnsupportedSubscription or ProviderSubscriptionPeriodPersistenceResultCode.TemporarilyUnavailable) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict);
        await transaction.CommitAsync(cancellationToken);
        return Result(period.Code == ProviderSubscriptionPeriodPersistenceResultCode.Applied ? GooglePlayVerifiedPurchasePersistenceResultCode.Applied : GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent);
    }
    private static GooglePlayVerifiedPurchasePersistenceResult Result(GooglePlayVerifiedPurchasePersistenceResultCode code) => new(code);
}
