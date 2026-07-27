using System.Security.Cryptography;
using System.Text;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayPurchaseTokenFingerprintService : IGooglePlayPurchaseTokenFingerprintService
{
    public string CreateFingerprint(string purchaseToken)
    {
        if (string.IsNullOrWhiteSpace(purchaseToken)) throw new ArgumentException("Purchase token is required.", nameof(purchaseToken));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(purchaseToken))).ToLowerInvariant();
    }
}
