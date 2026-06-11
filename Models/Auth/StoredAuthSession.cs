namespace EnglishVoiceTutor.Desktop.Models.Auth;

public sealed class StoredAuthSession
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset RefreshTokenExpiresAtUtc { get; set; }
    public AuthUserDto User { get; set; } = null!;
}
