namespace EnglishVoiceTutor.Api.Contracts.Auth;

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
