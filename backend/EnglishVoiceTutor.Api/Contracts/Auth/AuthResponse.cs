namespace EnglishVoiceTutor.Api.Contracts.Auth;

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public AuthUserDto User { get; set; } = null!;
}
