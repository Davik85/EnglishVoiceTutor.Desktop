namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayPurchaseVerifier
{
    Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken);
}
