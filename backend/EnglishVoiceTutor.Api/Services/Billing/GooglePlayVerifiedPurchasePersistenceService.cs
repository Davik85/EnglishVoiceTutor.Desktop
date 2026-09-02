using System.Data;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayVerifiedPurchasePersistenceService(AppDbContext dbContext, GooglePlayPurchaseClaimService claimService, GooglePlayPurchaseTokenSecretPersistenceService tokenSecretService, IGooglePlayPurchaseTokenFingerprintService fingerprintService, IUtcClock utcClock, ILogger<GooglePlayVerifiedPurchasePersistenceService> logger) : IGooglePlayVerifiedPurchasePersistenceService
{
    private const string VerifiedPeriodReason = "provider_scoped_verified_period";
    private const string OnHoldReason = "google_play_on_hold";
    private const string PausedReason = "google_play_paused";
    private const string ExpiredReason = "google_play_expired";
    private const string RevokedReason = "google_play_revoked";

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
            if (!request.IsAuthoritativeTrialDeferralPersistence
                && IsEntitlementRetainingLifecycle(request.VerifiedPurchase, utcClock.UtcNow)
                && await dbContext.GooglePlayInitialPremiumDeferrals.AnyAsync(
                    item => item.GooglePlayPurchaseClaimId == existingClaim.Id
                        && item.Status != GooglePlayTrialDeferralStatuses.Completed
                        && item.Status != GooglePlayTrialDeferralStatuses.AmbiguousTerminal,
                    cancellationToken))
            {
                return Result(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent);
            }
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
        if (subscription is not null
            && IsEntitlementRetainingLifecycle(request.VerifiedPurchase, utcClock.UtcNow)
            && await ExactEntitlements(subscription).AnyAsync(item => item.Status == SubscriptionConstants.Entitlements.StatusRevoked, cancellationToken))
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

        var persistedClaim = existingClaim ?? await dbContext.GooglePlayPurchaseClaims.SingleAsync(item => item.PurchaseTokenFingerprint == fingerprint, cancellationToken);
        PremiumCoverageWindow? existingCoverage = null;
        if (request.VerifiedPurchase.InitialPremiumDeferralEvidence is not null
            && !await dbContext.GooglePlayInitialPremiumDeferrals.AnyAsync(
                item => item.GooglePlayPurchaseClaimId == persistedClaim.Id,
                cancellationToken))
        {
            existingCoverage = await PremiumCoverageTimeline.CalculateAsync(
                dbContext,
                request.UserId,
                request.VerifiedPurchase.StartedAtUtc,
                cancellationToken);
        }

        var lifecycleChanged = await ApplyLifecycleWithinTransactionAsync(subscription, request.VerifiedPurchase, cancellationToken);
        var secret = await tokenSecretService.CreateOrUpdateAsync(new GooglePlayPurchaseTokenSecretWriteRequest(persistedClaim.Id, fingerprint, request.ProtectedPurchaseToken, GooglePlayPurchaseTokenProtectionService.ProtectionFormatVersion, request.VerifiedPurchase.AcknowledgementState == GooglePlayPurchaseAcknowledgementState.Pending), cancellationToken);
        if (secret.Code != GooglePlayPurchaseTokenSecretPersistenceResultCode.Stored) return Result(GooglePlayVerifiedPurchasePersistenceResultCode.TemporarilyUnavailable);
        await CaptureInitialPremiumDeferralPlanWithinTransactionAsync(
            request.UserId,
            persistedClaim.Id,
            request.VerifiedPurchase,
            existingCoverage,
            cancellationToken);
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
        return Result(lifecycleChanged ? GooglePlayVerifiedPurchasePersistenceResultCode.Applied : GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent);
    }

    private async Task CaptureInitialPremiumDeferralPlanWithinTransactionAsync(
        Guid userId,
        Guid claimId,
        GooglePlayVerifiedPurchase purchase,
        PremiumCoverageWindow? existingCoverage,
        CancellationToken cancellationToken)
    {
        if (purchase.InitialPremiumDeferralEvidence is null
            || existingCoverage is not { HasCoverage: true, StartsAtUtc: not null, EndsAtUtc: not null }
            || await dbContext.GooglePlayInitialPremiumDeferrals.AnyAsync(
                item => item.GooglePlayPurchaseClaimId == claimId,
                cancellationToken))
        {
            return;
        }

        var requiredDuration = existingCoverage.Value.EndsAtUtc.Value - purchase.StartedAtUtc;
        if (requiredDuration <= TimeSpan.Zero) return;
        var approvedDuration = requiredDuration < TimeSpan.FromDays(1) ? TimeSpan.FromDays(1) : requiredDuration;
        if (approvedDuration > TimeSpan.FromDays(365)) return;
        var now = utcClock.UtcNow;
        dbContext.GooglePlayInitialPremiumDeferrals.Add(new GooglePlayInitialPremiumDeferralEntity
        {
            Id = Guid.NewGuid(),
            GooglePlayPurchaseClaimId = claimId,
            UserId = userId,
            PackageName = purchase.PackageName,
            ProductId = purchase.ProductId,
            ProviderPurchaseStartedAtUtc = purchase.StartedAtUtc,
            BaselineProviderExpiryUtc = purchase.ExpiresAtUtc,
            ExistingCoverageStartsAtUtc = existingCoverage.Value.StartsAtUtc.Value,
            ExistingCoverageTailUtc = existingCoverage.Value.EndsAtUtc.Value,
            IsLicenseTestPurchase = purchase.InitialPremiumDeferralEvidence.IsLicenseTestPurchase,
            ApprovedDeferDurationTicks = approvedDuration.Ticks,
            TargetProviderExpiryUtc = purchase.ExpiresAtUtc.Add(approvedDuration),
            Status = GooglePlayTrialDeferralStatuses.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> ApplyLifecycleWithinTransactionAsync(
        SubscriptionEntity subscription,
        GooglePlayVerifiedPurchase purchase,
        CancellationToken cancellationToken)
    {
        var now = utcClock.UtcNow;
        return purchase.LifecycleState switch
        {
            GooglePlaySubscriptionLifecycleState.Active => await RetainExactEntitlementAsync(subscription, purchase, SubscriptionConstants.SubscriptionStatuses.Active, false, null, cancellationToken),
            GooglePlaySubscriptionLifecycleState.InGracePeriod => await RetainExactEntitlementAsync(subscription, purchase, SubscriptionConstants.SubscriptionStatuses.PastDue, false, null, cancellationToken),
            GooglePlaySubscriptionLifecycleState.Canceled when purchase.ExpiresAtUtc > now => await RetainExactEntitlementAsync(subscription, purchase, SubscriptionConstants.SubscriptionStatuses.Canceled, true, purchase.ExpiresAtUtc, cancellationToken, useExactExpiry: true),
            GooglePlaySubscriptionLifecycleState.Canceled => await SuspendExactEntitlementsAsync(subscription, SubscriptionConstants.SubscriptionStatuses.Expired, SubscriptionConstants.Entitlements.StatusExpired, ExpiredReason, purchase.ExpiresAtUtc, cancellationToken),
            GooglePlaySubscriptionLifecycleState.OnHold => await SuspendExactEntitlementsAsync(subscription, SubscriptionConstants.SubscriptionStatuses.PastDue, SubscriptionConstants.Entitlements.StatusInactive, OnHoldReason, null, cancellationToken),
            GooglePlaySubscriptionLifecycleState.Paused => await SuspendExactEntitlementsAsync(subscription, SubscriptionConstants.SubscriptionStatuses.Paused, SubscriptionConstants.Entitlements.StatusInactive, PausedReason, null, cancellationToken),
            GooglePlaySubscriptionLifecycleState.Expired => await SuspendExactEntitlementsAsync(subscription, SubscriptionConstants.SubscriptionStatuses.Expired, SubscriptionConstants.Entitlements.StatusExpired, ExpiredReason, purchase.ExpiresAtUtc, cancellationToken),
            GooglePlaySubscriptionLifecycleState.Revoked => await SuspendExactEntitlementsAsync(subscription, SubscriptionConstants.SubscriptionStatuses.Expired, SubscriptionConstants.Entitlements.StatusRevoked, RevokedReason, now, cancellationToken),
            _ => false
        };
    }

    private async Task<bool> RetainExactEntitlementAsync(
        SubscriptionEntity subscription,
        GooglePlayVerifiedPurchase purchase,
        string subscriptionStatus,
        bool cancelAtPeriodEnd,
        DateTimeOffset? scheduledChangeEffectiveAtUtc,
        CancellationToken cancellationToken,
        bool useExactExpiry = false)
    {
        var now = utcClock.UtcNow;
        var changed = false;
        changed |= SetIfDifferent(subscription.Status, subscriptionStatus, value => subscription.Status = value);
        changed |= SetIfDifferent(subscription.CancelAtPeriodEnd, cancelAtPeriodEnd, value => subscription.CancelAtPeriodEnd = value);
        var scheduledAction = cancelAtPeriodEnd ? SubscriptionConstants.ScheduledChangeActions.Cancel : null;
        changed |= SetIfDifferent(subscription.ScheduledChangeAction, scheduledAction, value => subscription.ScheduledChangeAction = value);
        changed |= SetIfDifferent(subscription.ScheduledChangeEffectiveAtUtc, scheduledChangeEffectiveAtUtc, value => subscription.ScheduledChangeEffectiveAtUtc = value);

        if (useExactExpiry || subscription.CurrentPeriodEndUtc is null || purchase.ExpiresAtUtc > subscription.CurrentPeriodEndUtc.Value)
        {
            changed |= SetIfDifferent(subscription.CurrentPeriodStartUtc, purchase.StartedAtUtc, value => subscription.CurrentPeriodStartUtc = value);
            changed |= SetIfDifferent(subscription.CurrentPeriodEndUtc, purchase.ExpiresAtUtc, value => subscription.CurrentPeriodEndUtc = value);
            if (useExactExpiry || subscription.ExpiresAt is null || purchase.ExpiresAtUtc > subscription.ExpiresAt.Value)
                changed |= SetIfDifferent(subscription.ExpiresAt, purchase.ExpiresAtUtc, value => subscription.ExpiresAt = value);
        }

        var entitlements = await ExactEntitlements(subscription).ToListAsync(cancellationToken);
        var entitlement = entitlements
            .Where(item => item.Status != SubscriptionConstants.Entitlements.StatusRevoked)
            .OrderByDescending(item => item.Status == SubscriptionConstants.Entitlements.StatusActive)
            .ThenByDescending(item => item.UpdatedAt)
            .FirstOrDefault();
        if (entitlement is null && entitlements.Any(item => item.Status == SubscriptionConstants.Entitlements.StatusRevoked))
        {
            // A revoked token cannot restore itself. A future valid purchase must arrive
            // under its own current/linked token and pass the normal ownership flow.
        }
        else if (entitlement is null)
        {
            dbContext.Entitlements.Add(new EntitlementEntity
            {
                Id = Guid.NewGuid(),
                UserId = subscription.UserId,
                SubscriptionId = subscription.Id,
                PlanId = SubscriptionConstants.Plans.PremiumPlanId,
                EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
                Source = SubscriptionConstants.Entitlements.SourceProviderEvent,
                Status = SubscriptionConstants.Entitlements.StatusActive,
                StartsAtUtc = purchase.StartedAtUtc,
                ExpiresAtUtc = purchase.ExpiresAtUtc,
                Reason = VerifiedPeriodReason,
                CreatedAt = now,
                UpdatedAt = now
            });
            changed = true;
        }
        else
        {
            changed |= SetIfDifferent(entitlement.Status, SubscriptionConstants.Entitlements.StatusActive, value => entitlement.Status = value);
            if (purchase.StartedAtUtc < entitlement.StartsAtUtc)
                changed |= SetIfDifferent(entitlement.StartsAtUtc, purchase.StartedAtUtc, value => entitlement.StartsAtUtc = value);
            if (useExactExpiry || (entitlement.ExpiresAtUtc is not null && purchase.ExpiresAtUtc > entitlement.ExpiresAtUtc.Value))
                changed |= SetIfDifferent(entitlement.ExpiresAtUtc, purchase.ExpiresAtUtc, value => entitlement.ExpiresAtUtc = value);
            changed |= SetIfDifferent(entitlement.Reason, VerifiedPeriodReason, value => entitlement.Reason = value);
            if (changed) entitlement.UpdatedAt = now;
        }

        if (changed)
        {
            subscription.ProviderProductId = purchase.ProductId;
            subscription.UpdatedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return changed;
    }

    private async Task<bool> SuspendExactEntitlementsAsync(
        SubscriptionEntity subscription,
        string subscriptionStatus,
        string entitlementStatus,
        string reason,
        DateTimeOffset? entitlementExpiresAtUtc,
        CancellationToken cancellationToken)
    {
        var now = utcClock.UtcNow;
        var changed = false;
        changed |= SetIfDifferent(subscription.Status, subscriptionStatus, value => subscription.Status = value);
        if (subscriptionStatus == SubscriptionConstants.SubscriptionStatuses.Expired)
        {
            changed |= SetIfDifferent(subscription.CancelAtPeriodEnd, false, value => subscription.CancelAtPeriodEnd = value);
            changed |= SetIfDifferent(subscription.ScheduledChangeAction, null, value => subscription.ScheduledChangeAction = value);
            changed |= SetIfDifferent(subscription.ScheduledChangeEffectiveAtUtc, null, value => subscription.ScheduledChangeEffectiveAtUtc = value);
        }

        var entitlements = await ExactEntitlements(subscription).ToListAsync(cancellationToken);
        var candidates = entitlementStatus switch
        {
            SubscriptionConstants.Entitlements.StatusRevoked => entitlements.Where(item => item.Status != SubscriptionConstants.Entitlements.StatusRevoked),
            SubscriptionConstants.Entitlements.StatusExpired => entitlements.Where(item => item.Status is SubscriptionConstants.Entitlements.StatusActive or SubscriptionConstants.Entitlements.StatusInactive),
            _ => entitlements.Where(item => item.Status == SubscriptionConstants.Entitlements.StatusActive)
        };
        foreach (var entitlement in candidates)
        {
            entitlement.Status = entitlementStatus;
            entitlement.Reason = reason;
            if (entitlementExpiresAtUtc.HasValue) entitlement.ExpiresAtUtc = entitlementExpiresAtUtc;
            entitlement.UpdatedAt = now;
            changed = true;
        }

        if (changed)
        {
            subscription.UpdatedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return changed;
    }

    private IQueryable<EntitlementEntity> ExactEntitlements(SubscriptionEntity subscription) => dbContext.Entitlements.Where(item =>
        item.UserId == subscription.UserId
        && item.SubscriptionId == subscription.Id
        && item.PlanId == SubscriptionConstants.Plans.PremiumPlanId
        && item.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType
        && item.Source == SubscriptionConstants.Entitlements.SourceProviderEvent);

    private static bool IsEntitlementRetainingLifecycle(GooglePlayVerifiedPurchase purchase, DateTimeOffset now) =>
        (purchase.LifecycleState is GooglePlaySubscriptionLifecycleState.Active or GooglePlaySubscriptionLifecycleState.InGracePeriod)
        || purchase.LifecycleState == GooglePlaySubscriptionLifecycleState.Canceled && purchase.ExpiresAtUtc > now;

    private static bool SetIfDifferent<T>(T current, T next, Action<T> assign)
    {
        if (EqualityComparer<T>.Default.Equals(current, next)) return false;
        assign(next);
        return true;
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
