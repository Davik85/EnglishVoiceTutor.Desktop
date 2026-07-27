namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayPurchaseTokenFingerprintService
{
    string CreateFingerprint(string purchaseToken);
}
