using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayPurchaseClaimService(
    AppDbContext dbContext,
    IGooglePlayPurchaseTokenFingerprintService fingerprintService,
    IUtcClock utcClock) : IGooglePlayPurchaseClaimService
{
    public async Task<GooglePlayPurchaseClaimResult> ClaimAsync(Guid userId, string purchaseToken, string productId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(purchaseToken) || string.IsNullOrWhiteSpace(productId))
        {
            return new GooglePlayPurchaseClaimResult(GooglePlayPurchaseClaimResultCode.InvalidInput);
        }

        string fingerprint;
        try
        {
            fingerprint = fingerprintService.CreateFingerprint(purchaseToken);
        }
        catch (ArgumentException)
        {
            return new GooglePlayPurchaseClaimResult(GooglePlayPurchaseClaimResultCode.InvalidInput);
        }

        try
        {
            var existing = await dbContext.GooglePlayPurchaseClaims.SingleOrDefaultAsync(item => item.PurchaseTokenFingerprint == fingerprint, cancellationToken);
            if (existing is not null)
            {
                if (existing.UserId != userId) return new GooglePlayPurchaseClaimResult(GooglePlayPurchaseClaimResultCode.OwnershipConflict);
                existing.LastSeenAtUtc = utcClock.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                return new GooglePlayPurchaseClaimResult(GooglePlayPurchaseClaimResultCode.AlreadyOwned);
            }

            var now = utcClock.UtcNow;
            var claim = new GooglePlayPurchaseClaimEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PurchaseTokenFingerprint = fingerprint,
                ProductId = productId,
                CreatedAtUtc = now,
                LastSeenAtUtc = now
            };
            dbContext.GooglePlayPurchaseClaims.Add(claim);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new GooglePlayPurchaseClaimResult(GooglePlayPurchaseClaimResultCode.Claimed);
        }
        catch (DbUpdateException)
        {
            foreach (var entry in dbContext.ChangeTracker.Entries<GooglePlayPurchaseClaimEntity>().Where(entry => entry.Entity.PurchaseTokenFingerprint == fingerprint && entry.State == EntityState.Added)) entry.State = EntityState.Detached;
            try
            {
                var existing = await dbContext.GooglePlayPurchaseClaims.SingleOrDefaultAsync(item => item.PurchaseTokenFingerprint == fingerprint, cancellationToken);
                if (existing is null) return new GooglePlayPurchaseClaimResult(GooglePlayPurchaseClaimResultCode.TemporarilyUnavailable);
                if (existing.UserId != userId) return new GooglePlayPurchaseClaimResult(GooglePlayPurchaseClaimResultCode.OwnershipConflict);
                existing.LastSeenAtUtc = utcClock.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                return new GooglePlayPurchaseClaimResult(GooglePlayPurchaseClaimResultCode.AlreadyOwned);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return new GooglePlayPurchaseClaimResult(GooglePlayPurchaseClaimResultCode.TemporarilyUnavailable);
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new GooglePlayPurchaseClaimResult(GooglePlayPurchaseClaimResultCode.TemporarilyUnavailable);
        }
    }
}
