using System.Security.Cryptography;
using System.Text.Json;
using EnglishVoiceTutor.Api.Services;
using Microsoft.AspNetCore.DataProtection;

namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayPendingRefundReviewProtectionService { string Protect(string pendingRefundToken, string orderId); GooglePlayPendingRefundReviewUnprotectResult TryUnprotect(string protectedPayload); }
public sealed record GooglePlayPendingRefundReviewUnprotectResult(bool Succeeded, string? PendingRefundToken, string? OrderId)
{
    public static GooglePlayPendingRefundReviewUnprotectResult Failure { get; } = new(false, null, null);
    public override string ToString() => Succeeded ? "Succeeded" : "Failed";
}
public sealed class GooglePlayPendingRefundReviewProtectionService(BackendDataProtectionProvider provider) : IGooglePlayPendingRefundReviewProtectionService
{
    public const string Purpose = "LanguageVoiceTutor.GooglePlay.PendingRefundReview.v1";
    public const string ProtectionFormatVersion = "v1";
    public string Protect(string pendingRefundToken, string orderId)
    {
        if (string.IsNullOrWhiteSpace(pendingRefundToken) || string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("Pending-refund payload is invalid.");
        return provider.Provider.CreateProtector(Purpose).Protect(JsonSerializer.Serialize(new Payload(ProtectionFormatVersion, pendingRefundToken, orderId)));
    }
    public GooglePlayPendingRefundReviewUnprotectResult TryUnprotect(string protectedPayload)
    {
        if (string.IsNullOrWhiteSpace(protectedPayload)) return GooglePlayPendingRefundReviewUnprotectResult.Failure;
        try { var value = JsonSerializer.Deserialize<Payload>(provider.Provider.CreateProtector(Purpose).Unprotect(protectedPayload)); return value is { Version: ProtectionFormatVersion } && !string.IsNullOrWhiteSpace(value.PendingRefundToken) && !string.IsNullOrWhiteSpace(value.OrderId) ? new(true, value.PendingRefundToken, value.OrderId) : GooglePlayPendingRefundReviewUnprotectResult.Failure; }
        catch (Exception ex) when (ex is CryptographicException or JsonException) { return GooglePlayPendingRefundReviewUnprotectResult.Failure; }
    }
    private sealed record Payload(string Version, string PendingRefundToken, string OrderId);
}

public interface IGooglePlayPendingRefundFingerprintService { string CreatePendingRefundTokenFingerprint(string value); string CreateOrderIdFingerprint(string value); }
public sealed class GooglePlayPendingRefundFingerprintService : IGooglePlayPendingRefundFingerprintService
{
    public string CreatePendingRefundTokenFingerprint(string value) => Create(value);
    public string CreateOrderIdFingerprint(string value) => Create(value);
    private static string Create(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A fingerprint input is required."); return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant(); }
}
