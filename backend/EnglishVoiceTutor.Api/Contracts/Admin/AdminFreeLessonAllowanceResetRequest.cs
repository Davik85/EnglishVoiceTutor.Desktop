namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminFreeLessonAllowanceResetRequest
{
    public string? UsageDate { get; init; }
    public string? Reason { get; init; }
}
