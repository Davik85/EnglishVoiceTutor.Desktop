namespace EnglishVoiceTutor.Api.Contracts.Auth;

public sealed class PasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
}
