namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendCheckoutSessionClientResult(
    bool IsSuccess,
    BackendCheckoutSessionResponse? Value,
    string? ErrorMessage,
    bool RequiresLogin)
{
    public static BackendCheckoutSessionClientResult Success(BackendCheckoutSessionResponse value)
    {
        return new BackendCheckoutSessionClientResult(true, value, null, false);
    }

    public static BackendCheckoutSessionClientResult Failure(string errorMessage, bool requiresLogin = false)
    {
        return new BackendCheckoutSessionClientResult(false, null, errorMessage, requiresLogin);
    }
}
