using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public enum GooglePlayPurchaseTokenSecretPersistenceResultCode { Stored, ClaimNotFound, InvalidInput, TemporarilyUnavailable }
public sealed record GooglePlayPurchaseTokenSecretPersistenceResult(GooglePlayPurchaseTokenSecretPersistenceResultCode Code);
public sealed record GooglePlayPurchaseTokenSecretWriteRequest(Guid ClaimId, string PurchaseTokenFingerprint, string ProtectedPurchaseToken, string ProtectionFormatVersion, bool AcknowledgementPending, DateTimeOffset? NextProviderCheckAtUtc = null);

public sealed class GooglePlayPurchaseTokenSecretPersistenceService(AppDbContext dbContext, IUtcClock utcClock)
{
    public Task<List<GooglePlayPurchaseTokenSecretEntity>> GetDueReconciliationBatchAsync(DateTimeOffset now, int maximumAttempts, int maximumCount, CancellationToken cancellationToken) =>
        dbContext.GooglePlayPurchaseTokenSecrets.Where(item =>
                item.SupersededAtUtc == null &&
                item.ReconciliationAttemptCount < maximumAttempts &&
                ((item.AcknowledgementPending && (item.NextProviderCheckAtUtc == null || item.NextProviderCheckAtUtc <= now)) ||
                 (!item.AcknowledgementPending && item.NextProviderCheckAtUtc <= now && (item.FinalRecheckUntilUtc == null || item.FinalRecheckUntilUtc >= now))))
            .OrderBy(item => item.AcknowledgementPending && item.NextProviderCheckAtUtc == null ? 0 : 1)
            .ThenBy(item => item.NextProviderCheckAtUtc)
            .ThenBy(item => item.Id)
            .Take(Math.Clamp(maximumCount, 1, 100)).ToListAsync(cancellationToken);
    public async Task<GooglePlayPurchaseTokenSecretPersistenceResult> CreateOrUpdateAsync(GooglePlayPurchaseTokenSecretWriteRequest request, CancellationToken cancellationToken)
    {
        if (request.ClaimId == Guid.Empty || !IsFingerprint(request.PurchaseTokenFingerprint) || string.IsNullOrWhiteSpace(request.ProtectedPurchaseToken) || string.IsNullOrWhiteSpace(request.ProtectionFormatVersion)) return Result(GooglePlayPurchaseTokenSecretPersistenceResultCode.InvalidInput);
        try
        {
            var claim = await dbContext.GooglePlayPurchaseClaims.SingleOrDefaultAsync(item => item.Id == request.ClaimId && item.PurchaseTokenFingerprint == request.PurchaseTokenFingerprint, cancellationToken);
            if (claim is null) return Result(GooglePlayPurchaseTokenSecretPersistenceResultCode.ClaimNotFound);
            var now = utcClock.UtcNow;
            var secret = await dbContext.GooglePlayPurchaseTokenSecrets.SingleOrDefaultAsync(item => item.GooglePlayPurchaseClaimId == request.ClaimId, cancellationToken);
            if (secret is null)
            {
                dbContext.GooglePlayPurchaseTokenSecrets.Add(new GooglePlayPurchaseTokenSecretEntity { Id = Guid.NewGuid(), GooglePlayPurchaseClaimId = claim.Id, PurchaseTokenFingerprint = request.PurchaseTokenFingerprint, ProtectedPurchaseToken = request.ProtectedPurchaseToken, ProtectionFormatVersion = request.ProtectionFormatVersion, CreatedAtUtc = now, UpdatedAtUtc = now, NextProviderCheckAtUtc = request.NextProviderCheckAtUtc, AcknowledgementPending = request.AcknowledgementPending });
            }
            else
            {
                secret.ProtectedPurchaseToken = request.ProtectedPurchaseToken;
                secret.ProtectionFormatVersion = request.ProtectionFormatVersion;
                secret.NextProviderCheckAtUtc = request.NextProviderCheckAtUtc;
                secret.AcknowledgementPending = request.AcknowledgementPending;
                secret.UpdatedAtUtc = now;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result(GooglePlayPurchaseTokenSecretPersistenceResultCode.Stored);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return Result(GooglePlayPurchaseTokenSecretPersistenceResultCode.TemporarilyUnavailable); }
    }

    public Task<GooglePlayPurchaseTokenSecretEntity?> FindByClaimIdAsync(Guid claimId, CancellationToken cancellationToken) => dbContext.GooglePlayPurchaseTokenSecrets.SingleOrDefaultAsync(item => item.GooglePlayPurchaseClaimId == claimId, cancellationToken);
    public Task<GooglePlayPurchaseTokenSecretEntity?> FindByFingerprintAsync(string fingerprint, CancellationToken cancellationToken) => dbContext.GooglePlayPurchaseTokenSecrets.SingleOrDefaultAsync(item => item.PurchaseTokenFingerprint == fingerprint, cancellationToken);

    public async Task MarkSupersededAsync(Guid claimId, DateTimeOffset? retentionDeleteAfterUtc, CancellationToken cancellationToken)
    {
        var secret = await FindByClaimIdAsync(claimId, cancellationToken);
        if (secret is null) return;
        secret.SupersededAtUtc = utcClock.UtcNow;
        secret.RetentionDeleteAfterUtc = retentionDeleteAfterUtc;
        secret.UpdatedAtUtc = utcClock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateReconciliationMetadataAsync(Guid claimId, DateTimeOffset? lastProviderCheckAtUtc, DateTimeOffset? nextProviderCheckAtUtc, int attemptCount, string? safeResultCode, DateTimeOffset? finalRecheckUntilUtc, bool acknowledgementPending, CancellationToken cancellationToken)
    {
        var secret = await FindByClaimIdAsync(claimId, cancellationToken);
        if (secret is null) return;
        secret.LastProviderCheckAtUtc = lastProviderCheckAtUtc;
        secret.NextProviderCheckAtUtc = nextProviderCheckAtUtc;
        secret.ReconciliationAttemptCount = Math.Max(0, attemptCount);
        secret.LastSafeResultCode = GooglePlayRtdnSafeErrorCodes.Normalize(safeResultCode);
        secret.FinalRecheckUntilUtc = finalRecheckUntilUtc;
        secret.AcknowledgementPending = acknowledgementPending;
        secret.UpdatedAtUtc = utcClock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsFingerprint(string value) => value.Length == EntityConstants.Lengths.GooglePlayPurchaseTokenFingerprintLength && value.All(character => char.IsAsciiHexDigit(character));
    private static GooglePlayPurchaseTokenSecretPersistenceResult Result(GooglePlayPurchaseTokenSecretPersistenceResultCode code) => new(code);
}
