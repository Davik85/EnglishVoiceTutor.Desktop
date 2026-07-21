namespace EnglishVoiceTutor.Api.Contracts.FeedbackReports;

public sealed class CreateAccountDeletionRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string? Reason { get; init; }
}

public sealed class CreateAccountDeletionRequestResponse
{
    public Guid ReportId { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool AlreadyRequested { get; init; }
}
