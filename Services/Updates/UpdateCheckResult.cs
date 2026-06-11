using System.Net;

namespace EnglishVoiceTutor.Desktop.Services.Updates;

public sealed record UpdateCheckResult(
    bool IsSuccess,
    UpdateManifestValidationResult? ValidationResult,
    string ErrorMessage,
    string ManifestUrl,
    string FailureCategory,
    HttpStatusCode? HttpStatusCode,
    string ExceptionMessage)
{
    public static UpdateCheckResult Success(UpdateManifestValidationResult validationResult, string manifestUrl) =>
        new(true, validationResult, string.Empty, manifestUrl, string.Empty, null, string.Empty);

    public static UpdateCheckResult Failure(
        string errorMessage,
        string manifestUrl,
        string failureCategory,
        HttpStatusCode? httpStatusCode = null,
        string exceptionMessage = "") =>
        new(false, null, errorMessage, manifestUrl, failureCategory, httpStatusCode, exceptionMessage);
}
