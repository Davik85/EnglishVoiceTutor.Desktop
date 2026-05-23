namespace EnglishVoiceTutor.Api.Constants;

public static class AuthConstants
{
    public const int MinimumPasswordLength = 8;
    public const string TokenTypeBearer = "Bearer";
    public const string ActiveUserStatus = "active";
    public const string InvalidCredentialsError = "Invalid email or password.";
    public const string DuplicateEmailError = "An account with this email already exists.";
    public const string MissingAuthUserError = "Authenticated user was not found.";
}
