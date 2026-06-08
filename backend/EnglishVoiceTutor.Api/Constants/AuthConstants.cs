namespace EnglishVoiceTutor.Api.Constants;

public static class AuthConstants
{
    public const int MinimumPasswordLength = 8;
    public const string TokenTypeBearer = "Bearer";
    public const string ActiveUserStatus = "active";
    public const string InvalidCredentialsError = "Email or password is incorrect.";
    public const string DuplicateEmailError = "An account with this email already exists.";
    public const string MissingAuthUserError = "Authenticated user was not found.";
    public const string PasswordResetAcceptedMessage = "Password reset instructions were sent if this email is registered.";
    public const string PasswordResetConfirmedMessage = "Password updated.";
    public const string PasswordResetInvalidMessage = "Password reset code is invalid or expired.";
    public const string PasswordResetDeliveryUnavailableMessage = "Password reset email delivery is not configured. Please contact support.";
    public const string PasswordChangeSuccessMessage = "Password updated.";
    public const string PasswordChangeInvalidCurrentPasswordMessage = "Current password is incorrect.";
    public static readonly string PasswordChangeInvalidLengthMessage = $"Password must be at least {MinimumPasswordLength} characters.";
    public const string PasswordChangeInvalidMessage = "Password could not be updated.";
}
