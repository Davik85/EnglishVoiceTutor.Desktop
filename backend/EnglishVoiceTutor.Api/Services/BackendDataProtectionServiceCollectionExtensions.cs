using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.AspNetCore.DataProtection;

namespace EnglishVoiceTutor.Api.Services;

public static class BackendDataProtectionServiceCollectionExtensions
{
    public static IServiceCollection AddBackendDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(BackendDataProtectionOptions.SectionName).Get<BackendDataProtectionOptions>()
            ?? new BackendDataProtectionOptions();

        if (!options.Enabled)
        {
            return services;
        }

        services.AddSingleton(BackendDataProtectionProvider.Create(options));
        services.AddSingleton<IGooglePlayPurchaseTokenProtectionService, GooglePlayPurchaseTokenProtectionService>();
        services.AddSingleton<IGooglePlayPendingRefundReviewProtectionService, GooglePlayPendingRefundReviewProtectionService>();
        services.AddScoped<GooglePlayPurchaseTokenSecretPersistenceService>();
        services.AddScoped<GooglePlayRtdnEventPersistenceService>();
        return services;
    }
}

public sealed class BackendDataProtectionProvider : IDisposable
{
    public const string ApplicationName = "LanguageVoiceTutor.Backend";

    private readonly IReadOnlyList<X509Certificate2> _certificates;

    private BackendDataProtectionProvider(IDataProtectionProvider provider, IReadOnlyList<X509Certificate2> certificates)
    {
        Provider = provider;
        _certificates = certificates;
    }

    public IDataProtectionProvider Provider { get; }

    public static BackendDataProtectionProvider Create(BackendDataProtectionOptions options)
    {
        options.ValidateForEnabledMode();

        try
        {
            var keyRingDirectory = new DirectoryInfo(options.KeyRingPath);
            if (!keyRingDirectory.Exists)
            {
                throw new InvalidOperationException("Backend Data Protection key-ring directory is missing.");
            }

            var activeCertificate = LoadCertificate(
                options.CertificatePath,
                options.CertificatePassword,
                "Backend Data Protection certificate must include a private key.");
            var certificates = new List<X509Certificate2> { activeCertificate };

            try
            {
                foreach (var previous in options.UnprotectCertificates)
                {
                    var certificate = LoadCertificate(
                        previous.CertificatePath,
                        previous.CertificatePassword,
                        "Backend Data Protection previous certificate must include a private key.");
                    if (certificates.Any(loaded => string.Equals(loaded.Thumbprint, certificate.Thumbprint, StringComparison.OrdinalIgnoreCase)))
                    {
                        certificate.Dispose();
                        throw new InvalidOperationException("Backend Data Protection certificates must not be duplicated.");
                    }

                    certificates.Add(certificate);
                }

                var provider = DataProtectionProvider.Create(
                keyRingDirectory,
                builder => builder
                    .SetApplicationName(ApplicationName)
                    .ProtectKeysWithCertificate(activeCertificate)
                    .UnprotectKeysWithAnyCertificate(certificates.Skip(1).ToArray()));

                return new BackendDataProtectionProvider(provider, certificates);
            }
            catch
            {
                foreach (var certificate in certificates) certificate.Dispose();
                throw;
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is CryptographicException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("Backend Data Protection configuration could not be initialized.");
        }
    }

    public void Dispose()
    {
        foreach (var certificate in _certificates) certificate.Dispose();
    }

    private static X509Certificate2 LoadCertificate(string path, string password, string missingPrivateKeyMessage)
    {
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(path, password, X509KeyStorageFlags.EphemeralKeySet);
        if (!certificate.HasPrivateKey)
        {
            certificate.Dispose();
            throw new InvalidOperationException(missingPrivateKeyMessage);
        }

        return certificate;
    }
}
