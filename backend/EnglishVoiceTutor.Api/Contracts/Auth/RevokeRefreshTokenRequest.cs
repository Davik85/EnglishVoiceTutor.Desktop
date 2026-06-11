namespace EnglishVoiceTutor.Api.Contracts.Auth;

public sealed class RevokeRefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
