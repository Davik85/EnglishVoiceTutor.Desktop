namespace EnglishVoiceTutor.Api.Options;

public sealed class RestoreCredentialsOptions
{
    public const string SectionName = "RestoreCredentials";
    public const int DefaultChallengeLifetimeSeconds = 300;
    private const string AndroidApkKeyHashOriginPrefix = "android:apk-key-hash:";

    public bool Enabled { get; set; }
    public string RpId { get; set; } = string.Empty;
    public string RpName { get; set; } = string.Empty;
    public List<string> AllowedOrigins { get; set; } = [];
    public int ChallengeLifetimeSeconds { get; set; } = DefaultChallengeLifetimeSeconds;

    public void ValidateWhenEnabled()
    {
        if (!Enabled) return;
        if (!IsValidRelyingPartyId(RpId) || string.IsNullOrWhiteSpace(RpName)
            || AllowedOrigins.Count == 0 || AllowedOrigins.Any(origin => !IsValidAllowedOrigin(origin))
            || ChallengeLifetimeSeconds <= 0)
        {
            throw new InvalidOperationException("RestoreCredentials is enabled but its relying-party configuration is incomplete or invalid.");
        }
    }

    private static bool IsValidRelyingPartyId(string? rpId) =>
        !string.IsNullOrWhiteSpace(rpId) && Uri.CheckHostName(rpId) == UriHostNameType.Dns;

    private static bool IsValidAllowedOrigin(string? origin) =>
        IsValidHttpsOrigin(origin) || IsValidAndroidApkKeyHashOrigin(origin);

    private static bool IsValidHttpsOrigin(string? origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var value) && value.Scheme == Uri.UriSchemeHttps;

    private static bool IsValidAndroidApkKeyHashOrigin(string? origin)
    {
        if (origin is null || !origin.StartsWith(AndroidApkKeyHashOriginPrefix, StringComparison.Ordinal)) return false;

        var encodedHash = origin[AndroidApkKeyHashOriginPrefix.Length..];
        if (encodedHash.Length == 0 || encodedHash.Length % 4 == 1 || encodedHash.Any(character =>
                !(character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_')))
        {
            return false;
        }

        try
        {
            var base64 = encodedHash.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            var hash = Convert.FromBase64String(base64);
            return hash.Length == 32
                && string.Equals(Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_'), encodedHash, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
