namespace EnglishVoiceTutor.Desktop.Services.Updates;

public sealed record UpdateCheckResult(
    bool IsSuccess,
    UpdateManifestValidationResult? ValidationResult,
    string ErrorMessage)
{
    public static UpdateCheckResult Success(UpdateManifestValidationResult validationResult) =>
        new(true, validationResult, string.Empty);

    public static UpdateCheckResult Failure(string errorMessage) =>
        new(false, null, errorMessage);
}
