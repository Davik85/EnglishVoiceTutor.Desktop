namespace EnglishVoiceTutor.Api.Contracts.FeedbackReports;

public sealed class CreateFeedbackReportRequest
{
    public string Category { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ReportedAiText { get; init; }
    public string ClientPlatform { get; init; } = string.Empty;
    public string ClientVersion { get; init; } = string.Empty;
}
