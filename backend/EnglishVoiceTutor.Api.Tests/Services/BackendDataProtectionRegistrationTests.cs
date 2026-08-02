using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class BackendDataProtectionRegistrationTests
{
    [Fact]
    public void DisabledModeDoesNotAccessConfiguredPathsOrChangeRelevantServiceRegistrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDataProtectionProvider>(DataProtectionProvider.Create(new DirectoryInfo(Path.GetTempPath())));
        var before = services.ToArray();
        var nonexistentKeyRingPath = Path.Combine(Path.GetTempPath(), "EnglishVoiceTutor", "DataProtectionTests", Guid.NewGuid().ToString("N"));

        services.AddBackendDataProtection(Configuration(
            ("BackendDataProtection:Enabled", "false"),
            ("BackendDataProtection:KeyRingPath", nonexistentKeyRingPath),
            ("BackendDataProtection:CertificatePath", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.pfx")),
            ("BackendDataProtection:CertificatePassword", "not-used")));

        Assert.Equal(before, services);
        Assert.False(Directory.Exists(nonexistentKeyRingPath));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(BackendDataProtectionProvider));
    }

    [Fact]
    public void MissingEnabledSettingsFailWithoutRevealingPassword()
    {
        const string password = "do-not-disclose-this-password";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BackendDataProtectionProvider.Create(new BackendDataProtectionOptions { Enabled = true, CertificatePassword = password }));

        Assert.DoesNotContain(password, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RelativePathsAreRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BackendDataProtectionProvider.Create(new BackendDataProtectionOptions
            {
                Enabled = true,
                KeyRingPath = "relative-keys",
                CertificatePath = "relative-certificate.pfx",
                CertificatePassword = "test-password"
            }));

        Assert.Contains("absolute", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingCertificateFailsWithoutRevealingPassword()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string password = "do-not-disclose-this-password";
        temporary.CreateKeyRingDirectory();
        var options = temporary.CreateOptions(Path.Combine(temporary.RootPath, "missing.pfx"), password);

        var exception = Assert.Throws<InvalidOperationException>(() => BackendDataProtectionProvider.Create(options));

        Assert.DoesNotContain(password, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidCertificateFailsWithoutRevealingPassword()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string password = "do-not-disclose-this-password";
        temporary.CreateKeyRingDirectory();
        var certificatePath = Path.Combine(temporary.RootPath, "invalid.pfx");
        File.WriteAllBytes(certificatePath, [1, 2, 3]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BackendDataProtectionProvider.Create(temporary.CreateOptions(certificatePath, password)));

        Assert.DoesNotContain(password, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingEnabledKeyRingDirectoryFailsWithoutCreatingItOrRevealingPassword()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string password = "do-not-disclose-this-password";
        var certificatePath = temporary.CreateCertificate("primary.pfx", password);
        var options = temporary.CreateOptions(certificatePath, password);

        var exception = Assert.Throws<InvalidOperationException>(() => BackendDataProtectionProvider.Create(options));

        Assert.False(Directory.Exists(temporary.KeyRingPath));
        Assert.DoesNotContain(password, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FreshProvidersShareConfiguredKeyRingAndApplicationNameButNotOtherNamesOrCertificates()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string password = "test-password";
        var certificatePath = temporary.CreateCertificate("primary.pfx", password);
        var options = temporary.CreateOptions(certificatePath, password);
        temporary.CreateKeyRingDirectory();

        using var first = BackendDataProtectionProvider.Create(options);
        var payload = first.Provider.CreateProtector("google-play-purchase-token").Protect("test-payload");

        using var second = BackendDataProtectionProvider.Create(options);
        Assert.Equal("test-payload", second.Provider.CreateProtector("google-play-purchase-token").Unprotect(payload));

        using var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, password, X509KeyStorageFlags.EphemeralKeySet);
        var differentApplication = DataProtectionProvider.Create(
            new DirectoryInfo(temporary.KeyRingPath),
            builder => builder.SetApplicationName("LanguageVoiceTutor.Backend.Other").ProtectKeysWithCertificate(certificate));
        Assert.ThrowsAny<CryptographicException>(() => differentApplication.CreateProtector("google-play-purchase-token").Unprotect(payload));

        var differentCertificatePath = temporary.CreateCertificate("different.pfx", password);
        using var differentCertificate = X509CertificateLoader.LoadPkcs12FromFile(differentCertificatePath, password, X509KeyStorageFlags.EphemeralKeySet);
        var differentCertificateProvider = DataProtectionProvider.Create(
            new DirectoryInfo(temporary.KeyRingPath),
            builder => builder.SetApplicationName(BackendDataProtectionProvider.ApplicationName).ProtectKeysWithCertificate(differentCertificate));
        Assert.ThrowsAny<CryptographicException>(() => differentCertificateProvider.CreateProtector("google-play-purchase-token").Unprotect(payload));
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(value => value.Key, value => (string?)value.Value)).Build();

    private sealed class TemporaryDataProtectionFiles : IDisposable
    {
        public TemporaryDataProtectionFiles()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "EnglishVoiceTutor", "DataProtectionTests", Guid.NewGuid().ToString("N"));
            KeyRingPath = Path.Combine(RootPath, "keys");
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }
        public string KeyRingPath { get; }

        public BackendDataProtectionOptions CreateOptions(string certificatePath, string password) => new()
        {
            Enabled = true,
            KeyRingPath = KeyRingPath,
            CertificatePath = certificatePath,
            CertificatePassword = password
        };

        public void CreateKeyRingDirectory() => Directory.CreateDirectory(KeyRingPath);

        public string CreateCertificate(string fileName, string password)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=LanguageVoiceTutor Data Protection Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
            var path = Path.Combine(RootPath, fileName);
            File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}
