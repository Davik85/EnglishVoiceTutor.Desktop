namespace EnglishVoiceTutor.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumSigningKeyLength = 32;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
