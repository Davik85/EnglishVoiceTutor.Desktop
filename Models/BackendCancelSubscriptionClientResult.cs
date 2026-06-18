namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendCancelSubscriptionClientResult(
    bool IsSuccess,
    BackendCancelSubscriptionResponse? Value,
    string? ErrorMessage,
    bool RequiresLogin)
{
    public static BackendCancelSubscriptionClientResult Success(BackendCancelSubscriptionResponse value) => new(true, value, null, false);
    public static BackendCancelSubscriptionClientResult Failure(string errorMessage, bool requiresLogin = false) => new(false, null, errorMessage, requiresLogin);
}
