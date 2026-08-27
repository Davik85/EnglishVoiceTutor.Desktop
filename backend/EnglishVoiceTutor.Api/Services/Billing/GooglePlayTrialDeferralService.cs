using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayTrialDeferralService(
    AppDbContext dbContext,
    IGooglePlaySubscriptionsV2Client subscriptionsClient,
    IGooglePlayVerifiedPurchasePersistenceService purchasePersistence,
    GooglePlayPurchaseTokenSecretPersistenceService secretPersistence,
    IGooglePlayPurchaseTokenFingerprintService fingerprintService,
    IUtcClock utcClock,
    IOptions<GooglePlayReconciliationOptions> optionsAccessor,
    ILogger<GooglePlayTrialDeferralService> logger) : IGooglePlayTrialDeferralService
{
    public async Task<GooglePlayTrialDeferralResult> ProcessAsync(
        Guid userId,
        string purchaseToken,
        string protectedPurchaseToken,
        CancellationToken cancellationToken)
    {
        string fingerprint;
        try { fingerprint = fingerprintService.CreateFingerprint(purchaseToken); }
        catch (ArgumentException) { return Result(GooglePlayTrialDeferralResultCode.AmbiguousTerminal); }

        var claim = await dbContext.GooglePlayPurchaseClaims.SingleOrDefaultAsync(
            item => item.PurchaseTokenFingerprint == fingerprint,
            cancellationToken);
        if (claim is null || claim.UserId != userId) return Result(GooglePlayTrialDeferralResultCode.AmbiguousTerminal);

        var plan = await dbContext.GooglePlayInitialPremiumDeferrals.SingleOrDefaultAsync(
            item => item.GooglePlayPurchaseClaimId == claim.Id,
            cancellationToken);
        if (plan is null) return Result(GooglePlayTrialDeferralResultCode.NotRequired);
        if (plan.UserId != userId) return await MarkAmbiguousAsync(plan, GooglePlayTrialDeferralSafeErrorCodes.ProviderStateDiverged, cancellationToken);
        if (plan.Status == GooglePlayTrialDeferralStatuses.Completed) return Result(GooglePlayTrialDeferralResultCode.Completed);
        if (plan.Status == GooglePlayTrialDeferralStatuses.AmbiguousTerminal) return Result(GooglePlayTrialDeferralResultCode.AmbiguousTerminal);

        var now = utcClock.UtcNow;
        if (plan.NextAttemptAtUtc > now) return Result(GooglePlayTrialDeferralResultCode.Pending);
        return plan.Status switch
        {
            GooglePlayTrialDeferralStatuses.Pending => await PrepareAndIssueAsync(plan, purchaseToken, protectedPurchaseToken, cancellationToken),
            GooglePlayTrialDeferralStatuses.ProviderOutcomeUnknown => await ReconcileUnknownOutcomeAsync(plan, purchaseToken, protectedPurchaseToken, cancellationToken),
            GooglePlayTrialDeferralStatuses.ProviderAppliedAwaitingRefresh => await RefreshAuthoritativeStateAsync(plan, purchaseToken, protectedPurchaseToken, cancellationToken),
            _ => await MarkAmbiguousAsync(plan, GooglePlayTrialDeferralSafeErrorCodes.ProviderStateDiverged, cancellationToken)
        };
    }

    private async Task<GooglePlayTrialDeferralResult> PrepareAndIssueAsync(
        GooglePlayInitialPremiumDeferralEntity plan,
        string purchaseToken,
        string protectedPurchaseToken,
        CancellationToken cancellationToken)
    {
        var snapshotResult = await GetProviderSnapshotAsync(plan, purchaseToken, cancellationToken);
        if (snapshotResult.Snapshot is null)
        {
            return snapshotResult.Terminal
                ? await MarkAmbiguousAsync(plan, snapshotResult.SafeErrorCode, cancellationToken)
                : await SchedulePendingAsync(plan, snapshotResult.SafeErrorCode, cancellationToken);
        }

        if (!TryReadProviderState(snapshotResult.Snapshot, plan, out var expiry, out var etag)
            || snapshotResult.Snapshot.AcknowledgementState != GooglePlayPurchaseAcknowledgementState.Acknowledged
            || expiry != plan.BaselineProviderExpiryUtc)
        {
            return await MarkAmbiguousAsync(plan, GooglePlayTrialDeferralSafeErrorCodes.ProviderStateDiverged, cancellationToken);
        }

        if (!await TryClaimAttemptAsync(plan, etag, cancellationToken)) return Result(GooglePlayTrialDeferralResultCode.Pending);
        return await IssueStoredCommandAsync(plan, purchaseToken, protectedPurchaseToken, cancellationToken);
    }

    private async Task<GooglePlayTrialDeferralResult> ReconcileUnknownOutcomeAsync(
        GooglePlayInitialPremiumDeferralEntity plan,
        string purchaseToken,
        string protectedPurchaseToken,
        CancellationToken cancellationToken)
    {
        var snapshotResult = await GetProviderSnapshotAsync(plan, purchaseToken, cancellationToken);
        if (snapshotResult.Snapshot is null)
        {
            return snapshotResult.Terminal
                ? await MarkAmbiguousAsync(plan, snapshotResult.SafeErrorCode, cancellationToken)
                : await SchedulePendingAsync(plan, snapshotResult.SafeErrorCode, cancellationToken);
        }

        if (!TryReadProviderState(snapshotResult.Snapshot, plan, out var expiry, out var currentEtag))
            return await MarkAmbiguousAsync(plan, GooglePlayTrialDeferralSafeErrorCodes.ProviderStateDiverged, cancellationToken);

        if (expiry == plan.TargetProviderExpiryUtc)
        {
            plan.Status = GooglePlayTrialDeferralStatuses.ProviderAppliedAwaitingRefresh;
            plan.NextAttemptAtUtc = null;
            plan.UpdatedAtUtc = utcClock.UtcNow;
            plan.ConcurrencyRevision++;
            await dbContext.SaveChangesAsync(cancellationToken);
            return await PersistAuthoritativeSnapshotAsync(plan, snapshotResult.Snapshot, purchaseToken, protectedPurchaseToken, cancellationToken);
        }

        if (expiry != plan.BaselineProviderExpiryUtc
            || string.IsNullOrWhiteSpace(plan.CommandEtag)
            || !string.Equals(currentEtag, plan.CommandEtag, StringComparison.Ordinal)
            || plan.AttemptCount >= optionsAccessor.Value.MaximumAttempts)
        {
            return await MarkAmbiguousAsync(plan, GooglePlayTrialDeferralSafeErrorCodes.ProviderStateDiverged, cancellationToken);
        }

        if (!await TryClaimAttemptAsync(plan, plan.CommandEtag, cancellationToken)) return Result(GooglePlayTrialDeferralResultCode.Pending);
        return await IssueStoredCommandAsync(plan, purchaseToken, protectedPurchaseToken, cancellationToken);
    }

    private async Task<GooglePlayTrialDeferralResult> IssueStoredCommandAsync(
        GooglePlayInitialPremiumDeferralEntity plan,
        string purchaseToken,
        string protectedPurchaseToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await subscriptionsClient.DeferAsync(
                plan.PackageName,
                purchaseToken,
                plan.CommandEtag!,
                TimeSpan.FromTicks(plan.ApprovedDeferDurationTicks),
                cancellationToken);
            var matchingItem = response.Items.Count == 1
                && string.Equals(response.Items[0].ProductId, SubscriptionConstants.Billing.GooglePlayPremiumProductId, StringComparison.Ordinal)
                ? response.Items[0]
                : null;
            plan.ProviderResponseExpiryUtc = matchingItem?.ExpiryTimeUtc;
            plan.Status = GooglePlayTrialDeferralStatuses.ProviderAppliedAwaitingRefresh;
            plan.LastSafeErrorCode = null;
            plan.NextAttemptAtUtc = null;
            plan.UpdatedAtUtc = utcClock.UtcNow;
            plan.ConcurrencyRevision++;
            await dbContext.SaveChangesAsync(cancellationToken);
            return await RefreshAuthoritativeStateAsync(plan, purchaseToken, protectedPurchaseToken, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (GooglePlaySubscriptionsV2ClientException exception) when (exception.Failure is GooglePlaySubscriptionsV2ClientFailure.PreconditionFailed or GooglePlaySubscriptionsV2ClientFailure.ProviderOutcomeUnknown)
        {
            plan.LastSafeErrorCode = GooglePlayTrialDeferralSafeErrorCodes.ProviderOutcomeUnknown;
            plan.NextAttemptAtUtc = null;
            plan.UpdatedAtUtc = utcClock.UtcNow;
            plan.ConcurrencyRevision++;
            await dbContext.SaveChangesAsync(cancellationToken);
            return await ReconcileUnknownOutcomeAsync(plan, purchaseToken, protectedPurchaseToken, cancellationToken);
        }
        catch (GooglePlaySubscriptionsV2ClientException)
        {
            return await MarkAmbiguousAsync(plan, GooglePlayTrialDeferralSafeErrorCodes.ProviderRejected, cancellationToken);
        }
        catch (Exception)
        {
            plan.LastSafeErrorCode = GooglePlayTrialDeferralSafeErrorCodes.ProviderOutcomeUnknown;
            plan.NextAttemptAtUtc = null;
            plan.UpdatedAtUtc = utcClock.UtcNow;
            plan.ConcurrencyRevision++;
            await dbContext.SaveChangesAsync(cancellationToken);
            return await ReconcileUnknownOutcomeAsync(plan, purchaseToken, protectedPurchaseToken, cancellationToken);
        }
    }

    private async Task<GooglePlayTrialDeferralResult> RefreshAuthoritativeStateAsync(
        GooglePlayInitialPremiumDeferralEntity plan,
        string purchaseToken,
        string protectedPurchaseToken,
        CancellationToken cancellationToken)
    {
        var snapshotResult = await GetProviderSnapshotAsync(plan, purchaseToken, cancellationToken);
        if (snapshotResult.Snapshot is null)
        {
            return snapshotResult.Terminal
                ? await MarkAmbiguousAsync(plan, snapshotResult.SafeErrorCode, cancellationToken)
                : await SchedulePendingAsync(plan, snapshotResult.SafeErrorCode, cancellationToken);
        }

        if (!TryReadProviderState(snapshotResult.Snapshot, plan, out var expiry, out _))
            return await MarkAmbiguousAsync(plan, GooglePlayTrialDeferralSafeErrorCodes.ProviderStateDiverged, cancellationToken);
        if (expiry != plan.TargetProviderExpiryUtc)
        {
            if (expiry == plan.BaselineProviderExpiryUtc)
                return await SchedulePendingAsync(plan, GooglePlayTrialDeferralSafeErrorCodes.ProviderUnavailable, cancellationToken);
            return await MarkAmbiguousAsync(plan, GooglePlayTrialDeferralSafeErrorCodes.ProviderStateDiverged, cancellationToken);
        }

        return await PersistAuthoritativeSnapshotAsync(plan, snapshotResult.Snapshot, purchaseToken, protectedPurchaseToken, cancellationToken);
    }

    private async Task<GooglePlayTrialDeferralResult> PersistAuthoritativeSnapshotAsync(
        GooglePlayInitialPremiumDeferralEntity plan,
        GooglePlaySubscriptionV2Snapshot snapshot,
        string purchaseToken,
        string protectedPurchaseToken,
        CancellationToken cancellationToken)
    {
        var item = snapshot.LineItems[0];
        var refreshedPurchase = new GooglePlayVerifiedPurchase(
            plan.PackageName,
            item.ProductId!,
            snapshot.StartTimeUtc!.Value.ToUniversalTime(),
            item.ExpiryTimeUtc!.Value.ToUniversalTime(),
            GooglePlayPurchaseAcknowledgementState.Acknowledged,
            plan.IsLicenseTestPurchase,
            GooglePlaySubscriptionLifecycleState.Active);
        var tokenSecret = await secretPersistence.FindByClaimIdAsync(plan.GooglePlayPurchaseClaimId, cancellationToken);
        if (tokenSecret is null)
            return await SchedulePendingAsync(plan, GooglePlayTrialDeferralSafeErrorCodes.PersistenceUnavailable, cancellationToken);
        var persistence = await purchasePersistence.PersistAsync(
            new GooglePlayVerifiedPurchasePersistenceRequest(plan.UserId, purchaseToken, refreshedPurchase, protectedPurchaseToken),
            cancellationToken);
        if (persistence.Code is not GooglePlayVerifiedPurchasePersistenceResultCode.Applied and not GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent)
            return await SchedulePendingAsync(plan, GooglePlayTrialDeferralSafeErrorCodes.PersistenceUnavailable, cancellationToken);

        var now = utcClock.UtcNow;
        plan.Status = GooglePlayTrialDeferralStatuses.Completed;
        plan.AuthoritativeProviderExpiryUtc = item.ExpiryTimeUtc;
        plan.LastSafeErrorCode = null;
        plan.NextAttemptAtUtc = null;
        plan.CompletedAtUtc = now;
        plan.UpdatedAtUtc = now;
        plan.ConcurrencyRevision++;
        await dbContext.SaveChangesAsync(cancellationToken);
        await secretPersistence.UpdateReconciliationMetadataAsync(plan.GooglePlayPurchaseClaimId, now, null, 0, null, tokenSecret.FinalRecheckUntilUtc, false, cancellationToken);
        return Result(GooglePlayTrialDeferralResultCode.Completed);
    }

    private async Task<bool> TryClaimAttemptAsync(GooglePlayInitialPremiumDeferralEntity plan, string commandEtag, CancellationToken cancellationToken)
    {
        var now = utcClock.UtcNow;
        if (plan.Status == GooglePlayTrialDeferralStatuses.Pending) plan.CommandEtag = commandEtag;
        else if (!string.Equals(plan.CommandEtag, commandEtag, StringComparison.Ordinal)) return false;
        plan.Status = GooglePlayTrialDeferralStatuses.ProviderOutcomeUnknown;
        plan.AttemptCount++;
        plan.LastAttemptAtUtc = now;
        plan.NextAttemptAtUtc = now.AddSeconds(RetryDelaySeconds(plan.AttemptCount));
        plan.LastSafeErrorCode = GooglePlayTrialDeferralSafeErrorCodes.ProviderOutcomeUnknown;
        plan.UpdatedAtUtc = now;
        plan.ConcurrencyRevision++;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    private async Task<ProviderSnapshotResult> GetProviderSnapshotAsync(GooglePlayInitialPremiumDeferralEntity plan, string purchaseToken, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await subscriptionsClient.GetAsync(plan.PackageName, purchaseToken, cancellationToken);
            return snapshot is null
                ? new ProviderSnapshotResult(null, false, GooglePlayTrialDeferralSafeErrorCodes.ProviderUnavailable)
                : new ProviderSnapshotResult(snapshot, false, string.Empty);
        }
        catch (OperationCanceledException) { throw; }
        catch (GooglePlaySubscriptionsV2ClientException exception) when (exception.Failure == GooglePlaySubscriptionsV2ClientFailure.InvalidPurchase)
        {
            return new ProviderSnapshotResult(null, true, GooglePlayTrialDeferralSafeErrorCodes.ProviderRejected);
        }
        catch (Exception)
        {
            logger.LogWarning("Google Play trial deferral provider refresh requires retry. ResultCode={ResultCode}.", GooglePlayTrialDeferralSafeErrorCodes.ProviderUnavailable);
            return new ProviderSnapshotResult(null, false, GooglePlayTrialDeferralSafeErrorCodes.ProviderUnavailable);
        }
    }

    private static bool TryReadProviderState(
        GooglePlaySubscriptionV2Snapshot snapshot,
        GooglePlayInitialPremiumDeferralEntity plan,
        out DateTimeOffset expiry,
        out string etag)
    {
        expiry = default;
        etag = string.Empty;
        if (snapshot.StartTimeUtc?.ToUniversalTime() != plan.ProviderPurchaseStartedAtUtc
            || snapshot.AcknowledgementState != GooglePlayPurchaseAcknowledgementState.Acknowledged
            || snapshot.LineItems.Count != 1
            || !GooglePlayTrialDeferralEligibility.HasOrdinaryPaidAutoRenewingShape(
                snapshot,
                snapshot.LineItems[0],
                plan.IsLicenseTestPurchase)
            || snapshot.LineItems[0].ExpiryTimeUtc is null)
        {
            return false;
        }
        var expiryTimeUtc = snapshot.LineItems[0].ExpiryTimeUtc;
        if (!expiryTimeUtc.HasValue) return false;
        expiry = expiryTimeUtc.Value.ToUniversalTime();
        etag = snapshot.Etag!;
        return true;
    }

    private async Task<GooglePlayTrialDeferralResult> SchedulePendingAsync(GooglePlayInitialPremiumDeferralEntity plan, string safeCode, CancellationToken cancellationToken)
    {
        var maximumAttempts = optionsAccessor.Value.MaximumAttempts;
        plan.AttemptCount = Math.Min(maximumAttempts, plan.AttemptCount + 1);
        if (plan.AttemptCount >= maximumAttempts)
            return await MarkAmbiguousAsync(plan, safeCode, cancellationToken);
        var now = utcClock.UtcNow;
        plan.NextAttemptAtUtc = now.AddSeconds(RetryDelaySeconds(plan.AttemptCount));
        plan.LastSafeErrorCode = safeCode;
        plan.UpdatedAtUtc = now;
        plan.ConcurrencyRevision++;
        await dbContext.SaveChangesAsync(cancellationToken);
        await secretPersistence.UpdateReconciliationMetadataAsync(plan.GooglePlayPurchaseClaimId, now, plan.NextAttemptAtUtc, plan.AttemptCount, safeCode, null, false, cancellationToken);
        return Result(GooglePlayTrialDeferralResultCode.Pending);
    }

    private async Task<GooglePlayTrialDeferralResult> MarkAmbiguousAsync(GooglePlayInitialPremiumDeferralEntity plan, string safeCode, CancellationToken cancellationToken)
    {
        var now = utcClock.UtcNow;
        plan.Status = GooglePlayTrialDeferralStatuses.AmbiguousTerminal;
        plan.NextAttemptAtUtc = null;
        plan.LastSafeErrorCode = safeCode;
        plan.UpdatedAtUtc = now;
        plan.ConcurrencyRevision++;
        await dbContext.SaveChangesAsync(cancellationToken);
        await secretPersistence.UpdateReconciliationMetadataAsync(plan.GooglePlayPurchaseClaimId, now, null, optionsAccessor.Value.MaximumAttempts, safeCode, null, false, cancellationToken);
        return Result(GooglePlayTrialDeferralResultCode.AmbiguousTerminal);
    }

    private int RetryDelaySeconds(int attempts) => GooglePlayReconciliationIterationService.RetryDelaySeconds(attempts, optionsAccessor.Value);
    private static GooglePlayTrialDeferralResult Result(GooglePlayTrialDeferralResultCode code) => new(code);
    private sealed record ProviderSnapshotResult(GooglePlaySubscriptionV2Snapshot? Snapshot, bool Terminal, string SafeErrorCode);
}
