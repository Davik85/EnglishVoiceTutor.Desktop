namespace EnglishVoiceTutor.Api.Options;

public sealed class BackendDataProtectionOptions
{
    public const string SectionName = "BackendDataProtection";

    public bool Enabled { get; set; }
    public string KeyRingPath { get; set; } = string.Empty;
    public string CertificatePath { get; set; } = string.Empty;
    public string CertificatePassword { get; set; } = string.Empty;
    public List<BackendDataProtectionCertificateOptions> UnprotectCertificates { get; set; } = [];

    public void ValidateForEnabledMode()
    {
        if (!Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(KeyRingPath) || !Path.IsPathFullyQualified(KeyRingPath))
        {
            throw new InvalidOperationException("Backend Data Protection requires an absolute key-ring path when enabled.");
        }

        if (string.IsNullOrWhiteSpace(CertificatePath) || !Path.IsPathFullyQualified(CertificatePath))
        {
            throw new InvalidOperationException("Backend Data Protection requires an absolute certificate path when enabled.");
        }

        if (string.IsNullOrWhiteSpace(CertificatePassword))
        {
            throw new InvalidOperationException("Backend Data Protection requires a certificate password when enabled.");
        }

        foreach (var certificate in UnprotectCertificates)
        {
            if (string.IsNullOrWhiteSpace(certificate.CertificatePath) || !Path.IsPathFullyQualified(certificate.CertificatePath))
            {
                throw new InvalidOperationException("Backend Data Protection requires absolute previous-certificate paths when enabled.");
            }

            if (string.IsNullOrWhiteSpace(certificate.CertificatePassword))
            {
                throw new InvalidOperationException("Backend Data Protection requires previous-certificate passwords when enabled.");
            }
        }
    }
}

public sealed class BackendDataProtectionCertificateOptions
{
    public string CertificatePath { get; set; } = string.Empty;
    public string CertificatePassword { get; set; } = string.Empty;
}
