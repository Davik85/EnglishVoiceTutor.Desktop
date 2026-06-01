namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendErrorResponse
{
    public string Error { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
