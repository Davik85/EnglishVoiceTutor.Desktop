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
        services.AddScoped<GooglePlayPurchaseTokenSecretPersistenceService>();
        services.AddScoped<GooglePlayRtdnEventPersistenceService>();
        return services;
    }
}

public sealed class BackendDataProtectionProvider : IDisposable
{
    public const string ApplicationName = "LanguageVoiceTutor.Backend";

    private readonly X509Certificate2 _certificate;

    private BackendDataProtectionProvider(IDataProtectionProvider provider, X509Certificate2 certificate)
    {
        Provider = provider;
        _certificate = certificate;
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

            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                options.CertificatePath,
                options.CertificatePassword,
                X509KeyStorageFlags.EphemeralKeySet);

            if (!certificate.HasPrivateKey)
            {
                certificate.Dispose();
                throw new InvalidOperationException("Backend Data Protection certificate must include a private key.");
            }

            var provider = DataProtectionProvider.Create(
                keyRingDirectory,
                builder => builder
                    .SetApplicationName(ApplicationName)
                    .ProtectKeysWithCertificate(certificate));

            return new BackendDataProtectionProvider(provider, certificate);
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

    public void Dispose() => _certificate.Dispose();
}
