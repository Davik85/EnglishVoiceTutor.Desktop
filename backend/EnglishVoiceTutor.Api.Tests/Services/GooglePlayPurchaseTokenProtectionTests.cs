using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.AspNetCore.DataProtection;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayPurchaseTokenProtectionTests
{
    [Fact]
    public void ProtectAndUnprotectRoundTripKeepsTheTokenOutOfCiphertextAndErrors()
    {
        using var temporary = new TemporaryProtectionFiles();
        using var provider = temporary.CreateProvider();
        var service = new GooglePlayPurchaseTokenProtectionService(provider);
        const string token = "test-purchase-token";

        var protectedValue = service.Protect(token);
        var result = service.TryUnprotect(protectedValue);

        Assert.True(result.Succeeded);
        Assert.Equal(token, result.PurchaseToken);
        Assert.NotEqual(token, protectedValue);
        Assert.DoesNotContain(token, protectedValue, StringComparison.Ordinal);
        Assert.DoesNotContain(token, result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void DifferentPurposeAndMalformedValueReturnSafeFailure()
    {
        using var temporary = new TemporaryProtectionFiles();
        using var provider = temporary.CreateProvider();
        var service = new GooglePlayPurchaseTokenProtectionService(provider);
        const string token = "test-purchase-token";
        var protectedValue = provider.Provider.CreateProtector("LanguageVoiceTutor.GooglePlay.PurchaseToken.other").Protect(token);

        var differentPurpose = service.TryUnprotect(protectedValue);
        var malformed = service.TryUnprotect("not-valid-protected-data");

        Assert.False(differentPurpose.Succeeded);
        Assert.Null(differentPurpose.PurchaseToken);
        Assert.False(malformed.Succeeded);
        Assert.Null(malformed.PurchaseToken);
        Assert.DoesNotContain(token, differentPurpose.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(protectedValue, differentPurpose.ToString(), StringComparison.Ordinal);
    }

    private sealed class TemporaryProtectionFiles : IDisposable
    {
        private const string Password = "test-password";
        private readonly string _root = Path.Combine(Path.GetTempPath(), "EnglishVoiceTutor", "GooglePlayProtectionTests", Guid.NewGuid().ToString("N"));

        public BackendDataProtectionProvider CreateProvider()
        {
            var keys = Path.Combine(_root, "keys");
            Directory.CreateDirectory(keys);
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=Google Play protection test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
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
