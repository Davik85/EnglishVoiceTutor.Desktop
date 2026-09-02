using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayReconciliationIterationService(
    AppDbContext dbContext,
    GooglePlayRtdnEventPersistenceService eventPersistence,
    GooglePlayPurchaseTokenSecretPersistenceService secretPersistence,
    IGooglePlayPurchaseTokenProtectionService tokenProtection,
    IGooglePlayPurchaseProcessor purchaseProcessor,
    IUtcClock utcClock,
    IOptions<GooglePlayReconciliationOptions> optionsAccessor,
    ILogger<GooglePlayReconciliationIterationService> logger)
{
    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var options = optionsAccessor.Value;
        var now = utcClock.UtcNow;
        var events = await eventPersistence.GetProcessableBatchAsync(now, now.AddSeconds(-options.ProcessingLeaseSeconds), options.BatchSize, cancellationToken);
        foreach (var item in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessEventAsync(item.Id, now, options, cancellationToken);
        }

        var secrets = await secretPersistence.GetDueReconciliationBatchAsync(now, options.MaximumAttempts, options.BatchSize, cancellationToken);
        foreach (var secret in secrets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessSecretAsync(secret.Id, now, options, cancellationToken);
        }
    }

    private async Task ProcessEventAsync(Guid eventId, DateTimeOffset now, GooglePlayReconciliationOptions options, CancellationToken cancellationToken)
    {
        if (!await eventPersistence.TryMarkProcessingAsync(eventId, now, now.AddSeconds(-options.ProcessingLeaseSeconds), cancellationToken)) return;
        var item = await dbContext.GooglePlayRtdnEvents.SingleAsync(value => value.Id == eventId, cancellationToken);
        if (item.NotificationKind == "test_notification") { await eventPersistence.MarkProcessedAsync(item.Id, cancellationToken); return; }
        if (item.PurchaseTokenFingerprint is null) { await eventPersistence.RecordPermanentFailureAsync(item.Id, GooglePlayRtdnSafeErrorCodes.InvalidNotification, cancellationToken); return; }

        var secret = await secretPersistence.FindByFingerprintAsync(item.PurchaseTokenFingerprint, cancellationToken);
        if (secret is null) { await RetryOrFailEventAsync(item, now, options, GooglePlayRtdnSafeErrorCodes.ProviderUnavailable, cancellationToken); return; }
        if (secret.SupersededAtUtc is not null) { await eventPersistence.MarkProcessedAsync(item.Id, cancellationToken); return; }
        var claim = await dbContext.GooglePlayPurchaseClaims.SingleOrDefaultAsync(value => value.Id == secret.GooglePlayPurchaseClaimId, cancellationToken);
        if (claim is null || !await dbContext.Users.AnyAsync(user => user.Id == claim.UserId, cancellationToken)) { await eventPersistence.RecordPermanentFailureAsync(item.Id, GooglePlayRtdnSafeErrorCodes.ProviderRejected, cancellationToken); return; }

        GooglePlayPurchaseTokenUnprotectResult token;
        try { token = tokenProtection.TryUnprotect(secret.ProtectedPurchaseToken); }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { token = GooglePlayPurchaseTokenUnprotectResult.Failure; }
        if (!token.Succeeded || string.IsNullOrWhiteSpace(token.PurchaseToken)) { await eventPersistence.RecordPermanentFailureAsync(item.Id, GooglePlayRtdnSafeErrorCodes.ProviderRejected, cancellationToken); return; }

        var confirmedRevocation = item.NotificationKind == "subscription_revoked";
        var result = await purchaseProcessor.ProcessAsync(
            claim.UserId,
            token.PurchaseToken,
            new GooglePlayPurchaseProcessingContext(confirmedRevocation),
            cancellationToken);
        if (result.Code == GooglePlayPurchaseProcessingResultCode.Verified) { await eventPersistence.MarkProcessedAsync(item.Id, cancellationToken); return; }
        if (result.Code is GooglePlayPurchaseProcessingResultCode.InvalidPurchase or GooglePlayPurchaseProcessingResultCode.UnsupportedProduct or GooglePlayPurchaseProcessingResultCode.OwnershipConflict or GooglePlayPurchaseProcessingResultCode.AcknowledgementInconsistent or GooglePlayPurchaseProcessingResultCode.TrialDeferralAmbiguous)
        { await eventPersistence.RecordPermanentFailureAsync(item.Id, GooglePlayRtdnSafeErrorCodes.ProviderRejected, cancellationToken); return; }
        await RetryOrFailEventAsync(item, now, options, result.Code == GooglePlayPurchaseProcessingResultCode.AcknowledgementPending ? GooglePlayRtdnSafeErrorCodes.ProviderUnavailable : GooglePlayRtdnSafeErrorCodes.ProviderUnavailable, cancellationToken);
    }

    private async Task RetryOrFailEventAsync(GooglePlayRtdnEventEntity item, DateTimeOffset now, GooglePlayReconciliationOptions options, string safeCode, CancellationToken cancellationToken)
    {
        if (item.AttemptCount >= options.MaximumAttempts) { await eventPersistence.RecordPermanentFailureAsync(item.Id, safeCode, cancellationToken); return; }
        await eventPersistence.RecordRetryableFailureAsync(item.Id, now.AddSeconds(RetryDelaySeconds(item.AttemptCount, options)), safeCode, cancellationToken);
    }

    private async Task ProcessSecretAsync(Guid secretId, DateTimeOffset now, GooglePlayReconciliationOptions options, CancellationToken cancellationToken)
    {
        var secret = await dbContext.GooglePlayPurchaseTokenSecrets.SingleOrDefaultAsync(value => value.Id == secretId, cancellationToken);
        if (secret is null) return;
        if (secret.SupersededAtUtc is not null) return;
        var claim = await dbContext.GooglePlayPurchaseClaims.SingleOrDefaultAsync(value => value.Id == secret.GooglePlayPurchaseClaimId, cancellationToken);
        if (claim is null || !await dbContext.Users.AnyAsync(user => user.Id == claim.UserId, cancellationToken)) { await secretPersistence.UpdateReconciliationMetadataAsync(secret.GooglePlayPurchaseClaimId, now, null, options.MaximumAttempts, GooglePlayRtdnSafeErrorCodes.ProviderRejected, null, false, cancellationToken); return; }
        GooglePlayPurchaseTokenUnprotectResult token;
        try { token = tokenProtection.TryUnprotect(secret.ProtectedPurchaseToken); }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { token = GooglePlayPurchaseTokenUnprotectResult.Failure; }
        if (!token.Succeeded || string.IsNullOrWhiteSpace(token.PurchaseToken)) { await secretPersistence.UpdateReconciliationMetadataAsync(secret.GooglePlayPurchaseClaimId, now, null, options.MaximumAttempts, GooglePlayRtdnSafeErrorCodes.ProviderRejected, null, false, cancellationToken); return; }
        var result = await purchaseProcessor.ProcessAsync(claim.UserId, token.PurchaseToken, cancellationToken);
        if (result.Code == GooglePlayPurchaseProcessingResultCode.Verified) { await secretPersistence.UpdateReconciliationMetadataAsync(secret.GooglePlayPurchaseClaimId, now, null, 0, null, secret.FinalRecheckUntilUtc, false, cancellationToken); return; }
        if (result.Code == GooglePlayPurchaseProcessingResultCode.TrialDeferralPending) return;
        if (result.Code == GooglePlayPurchaseProcessingResultCode.TrialDeferralAmbiguous)
        {
            await secretPersistence.UpdateReconciliationMetadataAsync(secret.GooglePlayPurchaseClaimId, now, null, options.MaximumAttempts, GooglePlayTrialDeferralSafeErrorCodes.ProviderStateDiverged, secret.FinalRecheckUntilUtc, false, cancellationToken);
            return;
        }
        var permanent = result.Code is GooglePlayPurchaseProcessingResultCode.InvalidPurchase or GooglePlayPurchaseProcessingResultCode.UnsupportedProduct or GooglePlayPurchaseProcessingResultCode.OwnershipConflict or GooglePlayPurchaseProcessingResultCode.AcknowledgementInconsistent;
        var attempts = Math.Min(options.MaximumAttempts, secret.ReconciliationAttemptCount + 1);
        var acknowledgementPending = !permanent && result.Code == GooglePlayPurchaseProcessingResultCode.AcknowledgementPending;
        var exhausted = attempts >= options.MaximumAttempts;
        await secretPersistence.UpdateReconciliationMetadataAsync(secret.GooglePlayPurchaseClaimId, now, permanent || exhausted ? null : now.AddSeconds(RetryDelaySeconds(attempts, options)), attempts, permanent ? GooglePlayRtdnSafeErrorCodes.ProviderRejected : GooglePlayRtdnSafeErrorCodes.ProviderUnavailable, secret.FinalRecheckUntilUtc, acknowledgementPending, cancellationToken);
        logger.LogInformation("Google Play reconciliation token result. ResultCode={ResultCode}.", permanent ? GooglePlayRtdnSafeErrorCodes.ProviderRejected : GooglePlayRtdnSafeErrorCodes.ProviderUnavailable);
    }

    public static int RetryDelaySeconds(int attempts, GooglePlayReconciliationOptions options)
    {
        var exponent = Math.Clamp(Math.Max(0, attempts - 1), 0, 20);
        var delay = (long)options.InitialRetrySeconds << exponent;
        return (int)Math.Min(options.MaximumRetrySeconds, delay);
    }
}
