namespace EnglishVoiceTutor.Api.Constants;

public static class AuthConstants
{
    public const int MinimumPasswordLength = 8;
    public const string TokenTypeBearer = "Bearer";
    public const string ActiveUserStatus = "active";
    public const string InvalidCredentialsError = "Invalid email or password.";
    public const string DuplicateEmailError = "An account with this email already exists.";
    public const string MissingAuthUserError = "Authenticated user was not found.";
    public const string PasswordResetAcceptedMessage = "If an account exists for that email, password reset instructions will be sent when password reset email delivery is enabled.";
    public const string PasswordResetConfirmedMessage = "If the reset token is valid, the password has been updated.";
    public const string PasswordResetInvalidMessage = "Password reset could not be completed.";
}
