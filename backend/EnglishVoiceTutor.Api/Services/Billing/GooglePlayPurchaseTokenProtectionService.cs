using System.Security.Cryptography;
using EnglishVoiceTutor.Api.Services;
using Microsoft.AspNetCore.DataProtection;

namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayPurchaseTokenProtectionService
{
    string Protect(string purchaseToken);
    GooglePlayPurchaseTokenUnprotectResult TryUnprotect(string protectedPurchaseToken);
}

public sealed record GooglePlayPurchaseTokenUnprotectResult(bool Succeeded, string? PurchaseToken)
{
    public static GooglePlayPurchaseTokenUnprotectResult Failure { get; } = new(false, null);
    public override string ToString() => Succeeded ? "Succeeded" : "Failed";
}

public sealed class GooglePlayPurchaseTokenProtectionService(BackendDataProtectionProvider backendDataProtectionProvider) : IGooglePlayPurchaseTokenProtectionService
{
    public const string ProtectionFormatVersion = "v1";
    public const string Purpose = "LanguageVoiceTutor.GooglePlay.PurchaseToken.v1";

    public string Protect(string purchaseToken)
    {
        if (string.IsNullOrWhiteSpace(purchaseToken)) throw new ArgumentException("A purchase token is required.", nameof(purchaseToken));
        return backendDataProtectionProvider.Provider.CreateProtector(Purpose).Protect(purchaseToken);
    }

    public GooglePlayPurchaseTokenUnprotectResult TryUnprotect(string protectedPurchaseToken)
    {
        if (string.IsNullOrWhiteSpace(protectedPurchaseToken)) return GooglePlayPurchaseTokenUnprotectResult.Failure;
        try
        {
            return new GooglePlayPurchaseTokenUnprotectResult(true, backendDataProtectionProvider.Provider.CreateProtector(Purpose).Unprotect(protectedPurchaseToken));
        }
        catch (CryptographicException)
        {
            return GooglePlayPurchaseTokenUnprotectResult.Failure;
        }
    }
}
