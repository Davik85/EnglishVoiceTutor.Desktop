namespace EnglishVoiceTutor.Desktop.Models.Auth;

public sealed class PasswordResetConfirmRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
