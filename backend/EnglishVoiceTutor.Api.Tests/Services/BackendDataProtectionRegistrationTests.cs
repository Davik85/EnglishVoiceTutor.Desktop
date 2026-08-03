using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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

    [Fact]
    public void OldKeyRingPayloadCanBeUnprotectedAfterRotationWhenTheOldCertificateIsConfigured()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string password = "test-password";
        var certificateA = temporary.CreateCertificate("a.pfx", password);
        var certificateB = temporary.CreateCertificate("b.pfx", password);
        temporary.CreateKeyRingDirectory();

        string payload;
        using (var providerA = BackendDataProtectionProvider.Create(temporary.CreateOptions(certificateA, password)))
        {
            payload = providerA.Provider.CreateProtector("rotation-test").Protect("old-payload");
        }

        using var providerB = BackendDataProtectionProvider.Create(temporary.CreateOptions(certificateB, password, certificateA, password));
        Assert.Equal("old-payload", providerB.Provider.CreateProtector("rotation-test").Unprotect(payload));
    }

    [Fact]
    public void OldKeyRingPayloadCannotBeUnprotectedAfterRotationWithoutTheOldCertificate()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string password = "test-password";
        var certificateA = temporary.CreateCertificate("a.pfx", password);
        var certificateB = temporary.CreateCertificate("b.pfx", password);
        temporary.CreateKeyRingDirectory();

        string payload;
        using (var providerA = BackendDataProtectionProvider.Create(temporary.CreateOptions(certificateA, password)))
        {
            payload = providerA.Provider.CreateProtector("rotation-test").Protect("old-payload");
        }

        using var providerB = BackendDataProtectionProvider.Create(temporary.CreateOptions(certificateB, password));
        Assert.ThrowsAny<CryptographicException>(() => providerB.Provider.CreateProtector("rotation-test").Unprotect(payload));
    }

    [Fact]
    public void NewlyCreatedKeyAfterRotationUsesTheActiveCertificateRatherThanThePreviousCertificate()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string password = "test-password";
        var certificateA = temporary.CreateCertificate("a.pfx", password);
        var certificateB = temporary.CreateCertificate("b.pfx", password);
        temporary.CreateKeyRingDirectory();

        using (var providerA = BackendDataProtectionProvider.Create(temporary.CreateOptions(certificateA, password)))
        {
            _ = providerA.Provider.CreateProtector("rotation-test").Protect("old-payload");
        }

        using var providerB = BackendDataProtectionProvider.Create(temporary.CreateOptions(certificateB, password, certificateA, password));
        var newPayload = CreatePayloadWithNewKey(temporary.KeyRingPath, certificateB, password);

        Assert.Equal("new-payload", providerB.Provider.CreateProtector("rotation-test").Unprotect(newPayload));
        using var providerAOnly = BackendDataProtectionProvider.Create(temporary.CreateOptions(certificateA, password));
        Assert.ThrowsAny<CryptographicException>(() => providerAOnly.Provider.CreateProtector("rotation-test").Unprotect(newPayload));
    }

    [Fact]
    public void MultiplePreviousCertificatesDecryptOldKeysAndTheNewActiveCertificateProtectsNewKeys()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string password = "test-password";
        var certificateA = temporary.CreateCertificate("a.pfx", password);
        var certificateB = temporary.CreateCertificate("b.pfx", password);
        var certificateC = temporary.CreateCertificate("c.pfx", password);
        temporary.CreateKeyRingDirectory();

        string payloadA;
        using (var providerA = BackendDataProtectionProvider.Create(temporary.CreateOptions(certificateA, password)))
        {
            payloadA = providerA.Provider.CreateProtector("rotation-test").Protect("payload-a");
        }

        string payloadB;
        using (var providerB = BackendDataProtectionProvider.Create(temporary.CreateOptions(certificateB, password, certificateA, password)))
        {
            payloadB = CreatePayloadWithNewKey(temporary.KeyRingPath, certificateB, password, "payload-b");
            Assert.Equal("payload-b", providerB.Provider.CreateProtector("rotation-test").Unprotect(payloadB));
        }

        using var providerC = BackendDataProtectionProvider.Create(temporary.CreateOptions(certificateC, password, certificateA, password, certificateB, password));
        Assert.Equal("payload-a", providerC.Provider.CreateProtector("rotation-test").Unprotect(payloadA));
        Assert.Equal("payload-b", providerC.Provider.CreateProtector("rotation-test").Unprotect(payloadB));

        var payloadC = CreatePayloadWithNewKey(temporary.KeyRingPath, certificateC, password, "payload-c");
        Assert.Equal("payload-c", providerC.Provider.CreateProtector("rotation-test").Unprotect(payloadC));
        using var providerBOnly = BackendDataProtectionProvider.Create(temporary.CreateOptions(certificateB, password, certificateA, password));
        Assert.ThrowsAny<CryptographicException>(() => providerBOnly.Provider.CreateProtector("rotation-test").Unprotect(payloadC));
    }

    [Fact]
    public void RelativePreviousCertificatePathAndMissingPasswordAreRejected()
    {
        var relativePath = Assert.Throws<InvalidOperationException>(() =>
            BackendDataProtectionProvider.Create(new BackendDataProtectionOptions
            {
                Enabled = true, KeyRingPath = Path.GetTempPath(), CertificatePath = Path.Combine(Path.GetTempPath(), "active.pfx"), CertificatePassword = "password",
                UnprotectCertificates = [new() { CertificatePath = "previous.pfx", CertificatePassword = "password" }]
            }));
        Assert.Contains("absolute", relativePath.Message, StringComparison.OrdinalIgnoreCase);

        var missingPassword = Assert.Throws<InvalidOperationException>(() =>
            BackendDataProtectionProvider.Create(new BackendDataProtectionOptions
            {
                Enabled = true, KeyRingPath = Path.GetTempPath(), CertificatePath = Path.Combine(Path.GetTempPath(), "active.pfx"), CertificatePassword = "password",
                UnprotectCertificates = [new() { CertificatePath = Path.Combine(Path.GetTempPath(), "previous.pfx") }]
            }));
        Assert.DoesNotContain("previous.pfx", missingPassword.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingOrInvalidPreviousCertificateAndIncorrectPasswordsFailWithoutDisclosure()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string activePassword = "active-certificate-password";
        const string previousPassword = "previous-certificate-password";
        const string missingPreviousPassword = "missing-previous-certificate-password";
        const string invalidPreviousPassword = "invalid-previous-certificate-password";
        const string incorrectActivePassword = "incorrect-active-certificate-password";
        const string incorrectPreviousPassword = "incorrect-previous-certificate-password";
        var active = temporary.CreateCertificate("active.pfx", activePassword);
        var previous = temporary.CreateCertificate("previous.pfx", previousPassword);
        temporary.CreateKeyRingDirectory();
        var missing = Path.Combine(temporary.RootPath, "missing.pfx");
        var invalid = Path.Combine(temporary.RootPath, "invalid.pfx");
        File.WriteAllBytes(invalid, [1, 2, 3]);

        var missingException = Assert.Throws<InvalidOperationException>(() =>
            BackendDataProtectionProvider.Create(temporary.CreateOptions(active, activePassword, missing, missingPreviousPassword)));
        var invalidException = Assert.Throws<InvalidOperationException>(() =>
            BackendDataProtectionProvider.Create(temporary.CreateOptions(active, activePassword, invalid, invalidPreviousPassword)));
        var activePasswordException = Assert.Throws<InvalidOperationException>(() =>
            BackendDataProtectionProvider.Create(temporary.CreateOptions(active, incorrectActivePassword)));
        var previousPasswordException = Assert.Throws<InvalidOperationException>(() =>
            BackendDataProtectionProvider.Create(temporary.CreateOptions(active, activePassword, previous, incorrectPreviousPassword)));

        AssertSafeFailure(missingException, [activePassword, missingPreviousPassword], [active, missing]);
        AssertSafeFailure(invalidException, [activePassword, invalidPreviousPassword], [active, invalid]);
        AssertSafeFailure(activePasswordException, [incorrectActivePassword], [active]);
        AssertSafeFailure(previousPasswordException, [activePassword, incorrectPreviousPassword], [active, previous]);
    }

    [Fact]
    public void ActiveAndPreviousCertificatesWithoutPrivateKeysFail()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string password = "test-password";
        var active = temporary.CreateCertificate("active.pfx", password);
        var publicOnly = temporary.CreatePublicOnlyCertificate("public-only.pfx", password);
        temporary.CreateKeyRingDirectory();

        Assert.Throws<InvalidOperationException>(() => BackendDataProtectionProvider.Create(temporary.CreateOptions(publicOnly, password)));
        Assert.Throws<InvalidOperationException>(() => BackendDataProtectionProvider.Create(temporary.CreateOptions(active, password, publicOnly, password)));
    }

    [Fact]
    public void DuplicateActiveAndPreviousCertificateIsRejectedWithoutDisclosingItsPath()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string password = "test-password";
        var certificate = temporary.CreateCertificate("duplicate.pfx", password);
        temporary.CreateKeyRingDirectory();

        var exception = Assert.Throws<InvalidOperationException>(() => BackendDataProtectionProvider.Create(temporary.CreateOptions(certificate, password, certificate, password)));
        Assert.DoesNotContain(certificate, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DisposalDisposesEveryOwnedCertificateAfterNormalUse()
    {
        using var temporary = new TemporaryDataProtectionFiles();
        const string password = "test-password";
        var active = temporary.CreateCertificate("active.pfx", password);
        var previous = temporary.CreateCertificate("previous.pfx", password);
        temporary.CreateKeyRingDirectory();
        var provider = BackendDataProtectionProvider.Create(temporary.CreateOptions(active, password, previous, password));
        _ = provider.Provider.CreateProtector("rotation-test").Protect("payload");
        var certificates = (IReadOnlyList<X509Certificate2>)typeof(BackendDataProtectionProvider).GetField("_certificates", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;

        provider.Dispose();

        Assert.All(certificates, certificate => Assert.ThrowsAny<CryptographicException>(() => certificate.Export(X509ContentType.Cert)));
    }

    private static string CreatePayloadWithNewKey(string keyRingPath, string certificatePath, string password, string value = "new-payload")
    {
        using var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, password, X509KeyStorageFlags.EphemeralKeySet);
        var services = new ServiceCollection();
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
            .SetApplicationName(BackendDataProtectionProvider.ApplicationName)
            .ProtectKeysWithCertificate(certificate);
        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IKeyManager>().CreateNewKey(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(90));
        return serviceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("rotation-test").Protect(value);
    }

    private static void AssertSafeFailure(InvalidOperationException exception, IEnumerable<string> passwords, IEnumerable<string> certificatePaths)
    {
        foreach (var password in passwords) Assert.DoesNotContain(password, exception.Message, StringComparison.Ordinal);
        foreach (var certificatePath in certificatePaths)
        {
            Assert.DoesNotContain(certificatePath, exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(Path.GetFileName(certificatePath), exception.Message, StringComparison.Ordinal);
        }
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

        public BackendDataProtectionOptions CreateOptions(string certificatePath, string password, params string[] previousPathAndPasswords)
        {
            if (previousPathAndPasswords.Length % 2 != 0) throw new ArgumentException("Previous certificate paths and passwords must be paired.");
            return new()
            {
                Enabled = true,
                KeyRingPath = KeyRingPath,
                CertificatePath = certificatePath,
                CertificatePassword = password,
                UnprotectCertificates = previousPathAndPasswords.Chunk(2).Select(value => new BackendDataProtectionCertificateOptions { CertificatePath = value[0], CertificatePassword = value[1] }).ToList()
            };
        }

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

        public string CreatePublicOnlyCertificate(string fileName, string password)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=LanguageVoiceTutor Data Protection Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
            using var publicOnly = X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
            var path = Path.Combine(RootPath, fileName);
            File.WriteAllBytes(path, publicOnly.Export(X509ContentType.Pfx, password));
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
