namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayPurchaseClaimService
{
    Task<GooglePlayPurchaseClaimResult> ClaimAsync(Guid userId, string purchaseToken, string productId, CancellationToken cancellationToken);
}
