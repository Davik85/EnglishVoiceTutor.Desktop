using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.AspNetCore.DataProtection;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayPendingRefundReviewProtectionTests
{
    [Fact]
    public void ProtectAndUnprotectRoundTripKeepsSensitiveValuesOutOfCiphertextAndResultText()
    {
        using var temporary = new TemporaryProtectionFiles();
        using var provider = temporary.CreateProvider();
        var service = new GooglePlayPendingRefundReviewProtectionService(provider);
        const string token = "test-pending-refund-token";
        const string orderId = "test-order-id";

        var protectedValue = service.Protect(token, orderId);
        var result = service.TryUnprotect(protectedValue);

        Assert.True(result.Succeeded);
        Assert.Equal(token, result.PendingRefundToken);
        Assert.Equal(orderId, result.OrderId);
        Assert.DoesNotContain(token, protectedValue, StringComparison.Ordinal);
        Assert.DoesNotContain(orderId, protectedValue, StringComparison.Ordinal);
        Assert.DoesNotContain(token, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(orderId, result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MalformedAndWrongPurposePayloadsFailWithoutExposingRawValues()
    {
        using var temporary = new TemporaryProtectionFiles();
        using var provider = temporary.CreateProvider();
        var service = new GooglePlayPendingRefundReviewProtectionService(provider);
        const string token = "test-pending-refund-token";
        const string orderId = "test-order-id";
        var wrongPurpose = provider.Provider.CreateProtector("LanguageVoiceTutor.GooglePlay.PendingRefundReview.other").Protect(JsonSerializer.Serialize(new { Version = "v1", PendingRefundToken = token, OrderId = orderId }));

        var malformed = service.TryUnprotect("not-valid-protected-data");
        var wrongPurposeResult = service.TryUnprotect(wrongPurpose);

        Assert.False(malformed.Succeeded);
        Assert.False(wrongPurposeResult.Succeeded);
        Assert.Null(wrongPurposeResult.PendingRefundToken);
        Assert.Null(wrongPurposeResult.OrderId);
        Assert.DoesNotContain(token, wrongPurposeResult.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(orderId, wrongPurposeResult.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(wrongPurpose, wrongPurposeResult.ToString(), StringComparison.Ordinal);
    }

    private sealed class TemporaryProtectionFiles : IDisposable
    {
        private const string Password = "test-password";
        private readonly string _root = Path.Combine(Path.GetTempPath(), "EnglishVoiceTutor", "GooglePlayPendingRefundProtectionTests", Guid.NewGuid().ToString("N"));

        public BackendDataProtectionProvider CreateProvider()
        {
            var keys = Path.Combine(_root, "keys");
            Directory.CreateDirectory(keys);
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=Google Play pending-refund protection test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
            var certificatePath = Path.Combine(_root, "test.pfx");
            File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Pfx, Password));
            return BackendDataProtectionProvider.Create(new BackendDataProtectionOptions { Enabled = true, KeyRingPath = keys, CertificatePath = certificatePath, CertificatePassword = Password });
        }

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
    }
}
