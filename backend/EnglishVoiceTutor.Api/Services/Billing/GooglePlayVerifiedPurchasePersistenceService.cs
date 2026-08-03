using System.Data;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayVerifiedPurchasePersistenceService(AppDbContext dbContext, GooglePlayPurchaseClaimService claimService, ProviderSubscriptionPeriodPersistenceService periodService, GooglePlayPurchaseTokenSecretPersistenceService tokenSecretService, IGooglePlayPurchaseTokenFingerprintService fingerprintService, IUtcClock utcClock, ILogger<GooglePlayVerifiedPurchasePersistenceService> logger) : IGooglePlayVerifiedPurchasePersistenceService
{
    public async Task<GooglePlayVerifiedPurchasePersistenceResult> PersistAsync(GooglePlayVerifiedPurchasePersistenceRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.PurchaseToken) || string.IsNullOrWhiteSpace(request.ProtectedPurchaseToken) || string.IsNullOrWhiteSpace(request.VerifiedPurchase.ProductId) || request.VerifiedPurchase.StartedAtUtc.Offset != TimeSpan.Zero || request.VerifiedPurchase.ExpiresAtUtc.Offset != TimeSpan.Zero || request.VerifiedPurchase.ExpiresAtUtc <= request.VerifiedPurchase.StartedAtUtc) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.InvalidInput);
        string fingerprint;
        try { fingerprint = fingerprintService.CreateFingerprint(request.PurchaseToken); } catch (ArgumentException) { return Result(GooglePlayVerifiedPurchasePersistenceResultCode.InvalidInput); }
        string? linkedFingerprint = null;
        if (!string.IsNullOrWhiteSpace(request.VerifiedPurchase.LinkedPurchaseToken))
        {
            try { linkedFingerprint = fingerprintService.CreateFingerprint(request.VerifiedPurchase.LinkedPurchaseToken); }
            catch (ArgumentException) { return Result(GooglePlayVerifiedPurchasePersistenceResultCode.InvalidInput); }
            if (string.Equals(fingerprint, linkedFingerprint, StringComparison.Ordinal)) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict);
        }
        try
        {
            return await BillingSerializableTransactionRetryPolicy.ExecuteAsync((_, ct) => PersistWithinTransactionAsync(request, fingerprint, linkedFingerprint, ct), dbContext.ChangeTracker.Clear, logger, "Google Play verified purchase persistence", Guid.Empty, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { dbContext.ChangeTracker.Clear(); return Result(GooglePlayVerifiedPurchasePersistenceResultCode.TemporarilyUnavailable); }
    }

    private async Task<GooglePlayVerifiedPurchasePersistenceResult> PersistWithinTransactionAsync(GooglePlayVerifiedPurchasePersistenceRequest request, string fingerprint, string? linkedFingerprint, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var existingClaim = await dbContext.GooglePlayPurchaseClaims.SingleOrDefaultAsync(item => item.PurchaseTokenFingerprint == fingerprint, cancellationToken);
        if (existingClaim is not null && existingClaim.UserId != request.UserId) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict);
        if (existingClaim is not null)
        {
            var existingSecret = await tokenSecretService.FindByClaimIdAsync(existingClaim.Id, cancellationToken);
            if (existingSecret?.SupersededAtUtc is not null) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict);
        }
        GooglePlayPurchaseClaimEntity? linkedClaim = null;
        var linkedSecretSuperseded = false;
        if (linkedFingerprint is not null)
        {
            linkedClaim = await dbContext.GooglePlayPurchaseClaims.SingleOrDefaultAsync(item => item.PurchaseTokenFingerprint == linkedFingerprint, cancellationToken);
            if (linkedClaim is not null && linkedClaim.UserId != request.UserId) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict);
            if (linkedClaim is not null)
            {
                var linkedSecret = await tokenSecretService.FindByClaimIdAsync(linkedClaim.Id, cancellationToken);
                if (linkedSecret is null) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict);
                linkedSecretSuperseded = linkedSecret.SupersededAtUtc is not null;
                if (linkedSecret.SupersededAtUtc is not null && existingClaim is null) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict);
            }
        }

        var allowsDeferredTransition = existingClaim is not null
            && linkedClaim is not null
            && linkedClaim.UserId == request.UserId
            && linkedSecretSuperseded
            && !string.Equals(existingClaim.ProductId, request.VerifiedPurchase.ProductId, StringComparison.Ordinal);
        if (existingClaim is not null && !string.Equals(existingClaim.ProductId, request.VerifiedPurchase.ProductId, StringComparison.Ordinal) && !allowsDeferredTransition) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.ProductMismatch);

        var subscription = await dbContext.Subscriptions.SingleOrDefaultAsync(item => item.Provider == SubscriptionConstants.BillingProviders.GooglePlay && item.ProviderSubscriptionId == fingerprint, cancellationToken);
        if (subscription is not null
            && (subscription.UserId != request.UserId
                || subscription.PlanId != SubscriptionConstants.Plans.PremiumPlanId
                || subscription.Provider != SubscriptionConstants.BillingProviders.GooglePlay
                || subscription.ProviderSubscriptionId != fingerprint
                || (!string.IsNullOrWhiteSpace(subscription.ProviderProductId)
                    && !string.Equals(subscription.ProviderProductId, request.VerifiedPurchase.ProductId, StringComparison.Ordinal)
                    && !allowsDeferredTransition)))
        {
            return Result(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict);
        }

        var claim = await claimService.ClaimWithinTransactionAsync(request.UserId, fingerprint, request.VerifiedPurchase.ProductId, cancellationToken);
        if (claim.Code == GooglePlayPurchaseClaimResultCode.OwnershipConflict) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict);
        if (claim.Code is GooglePlayPurchaseClaimResultCode.InvalidInput or GooglePlayPurchaseClaimResultCode.TemporarilyUnavailable) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.TemporarilyUnavailable);
        if (allowsDeferredTransition)
        {
            existingClaim!.ProductId = request.VerifiedPurchase.ProductId;
            if (subscription is not null) subscription.ProviderProductId = request.VerifiedPurchase.ProductId;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

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
        var secret = await tokenSecretService.CreateOrUpdateAsync(new GooglePlayPurchaseTokenSecretWriteRequest(existingClaim?.Id ?? (await dbContext.GooglePlayPurchaseClaims.SingleAsync(item => item.PurchaseTokenFingerprint == fingerprint, cancellationToken)).Id, fingerprint, request.ProtectedPurchaseToken, GooglePlayPurchaseTokenProtectionService.ProtectionFormatVersion, request.VerifiedPurchase.AcknowledgementState == GooglePlayPurchaseAcknowledgementState.Pending), cancellationToken);
        if (secret.Code != GooglePlayPurchaseTokenSecretPersistenceResultCode.Stored) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.TemporarilyUnavailable);
        if (linkedClaim is not null)
        {
            var now = utcClock.UtcNow;
            var oldSubscriptions = await dbContext.Subscriptions.Where(item => item.UserId == request.UserId && item.Provider == SubscriptionConstants.BillingProviders.GooglePlay && item.ProviderSubscriptionId == linkedFingerprint).ToListAsync(cancellationToken);
            foreach (var oldSubscription in oldSubscriptions)
            {
                oldSubscription.Status = SubscriptionConstants.SubscriptionStatuses.Expired;
                oldSubscription.UpdatedAt = now;
            }
            var oldSubscriptionIds = oldSubscriptions.Select(item => item.Id).ToArray();
            var oldEntitlements = await dbContext.Entitlements.Where(item => item.UserId == request.UserId && item.SubscriptionId != null && oldSubscriptionIds.Contains(item.SubscriptionId.Value) && item.Source == SubscriptionConstants.Entitlements.SourceProviderEvent && item.Status == SubscriptionConstants.Entitlements.StatusActive).ToListAsync(cancellationToken);
            foreach (var entitlement in oldEntitlements) { entitlement.Status = SubscriptionConstants.Entitlements.StatusInactive; entitlement.UpdatedAt = now; }
            var oldSecret = await tokenSecretService.FindByClaimIdAsync(linkedClaim.Id, cancellationToken);
            if (oldSecret is not null)
            {
                oldSecret.SupersededAtUtc = now;
                oldSecret.NextProviderCheckAtUtc = null;
                oldSecret.AcknowledgementPending = false;
                oldSecret.UpdatedAtUtc = now;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return Result(period.Code == ProviderSubscriptionPeriodPersistenceResultCode.Applied ? GooglePlayVerifiedPurchasePersistenceResultCode.Applied : GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent);
    }

    public async Task UpdateAcknowledgementStateAsync(string purchaseToken, bool acknowledgementPending, string? safeResultCode, CancellationToken cancellationToken)
    {
        string fingerprint;
        try { fingerprint = fingerprintService.CreateFingerprint(purchaseToken); }
        catch (ArgumentException) { return; }
        var secret = await tokenSecretService.FindByFingerprintAsync(fingerprint, cancellationToken);
        if (secret is null) return;
        await tokenSecretService.UpdateReconciliationMetadataAsync(secret.GooglePlayPurchaseClaimId, secret.LastProviderCheckAtUtc, secret.NextProviderCheckAtUtc, secret.ReconciliationAttemptCount, safeResultCode, secret.FinalRecheckUntilUtc, acknowledgementPending, cancellationToken);
    }
    private static GooglePlayVerifiedPurchasePersistenceResult Result(GooglePlayVerifiedPurchasePersistenceResultCode code) => new(code);
}
