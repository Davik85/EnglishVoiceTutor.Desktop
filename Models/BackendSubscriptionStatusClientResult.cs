using System.Net;

namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendSubscriptionStatusClientResult(
    bool IsSuccess,
    BackendSubscriptionStatusResponse? Value,
    string? ErrorMessage,
    HttpStatusCode? StatusCode)
{
    public static BackendSubscriptionStatusClientResult Success(BackendSubscriptionStatusResponse value)
    {
        return new BackendSubscriptionStatusClientResult(true, value, null, null);
    }

    public static BackendSubscriptionStatusClientResult Failure(string? errorMessage = null, HttpStatusCode? statusCode = null)
    {
        return new BackendSubscriptionStatusClientResult(false, null, errorMessage, statusCode);
    }
}
