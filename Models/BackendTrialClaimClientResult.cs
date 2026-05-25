namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendTrialClaimClientResult(
    bool IsSuccess,
    BackendTrialClaimResponse? Value,
    string? ErrorMessage,
    bool RequiresLogin)
{
    public static BackendTrialClaimClientResult Success(BackendTrialClaimResponse value)
    {
        return new BackendTrialClaimClientResult(true, value, null, false);
    }

    public static BackendTrialClaimClientResult Failure(string errorMessage, bool requiresLogin = false)
    {
        return new BackendTrialClaimClientResult(false, null, errorMessage, requiresLogin);
    }
}
