namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class DisabledGooglePlayPurchaseVerifier : IGooglePlayPurchaseVerifier
{
    public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new GooglePlayPurchaseVerificationResult(GooglePlayPurchaseVerificationResultCode.NotConfigured));
    }
}
