using System.Net;

namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendUserSettingsClientResult<T>(bool IsSuccess, T? Value, string? ErrorMessage, HttpStatusCode? StatusCode)
{
    public static BackendUserSettingsClientResult<T> Success(T value)
    {
        return new BackendUserSettingsClientResult<T>(true, value, null, null);
    }

    public static BackendUserSettingsClientResult<T> Failure(string? errorMessage = null, HttpStatusCode? statusCode = null)
    {
        return new BackendUserSettingsClientResult<T>(false, default, errorMessage, statusCode);
    }
}
