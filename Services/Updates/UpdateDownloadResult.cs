namespace EnglishVoiceTutor.Desktop.Services.Updates;

public sealed record UpdateDownloadResult(
    bool IsSuccess,
    string FilePath,
    string ErrorMessage)
{
    public static UpdateDownloadResult Success(string filePath) => new(true, filePath, string.Empty);

    public static UpdateDownloadResult Failure(string errorMessage, string filePath = "") => new(false, filePath, errorMessage);
}
