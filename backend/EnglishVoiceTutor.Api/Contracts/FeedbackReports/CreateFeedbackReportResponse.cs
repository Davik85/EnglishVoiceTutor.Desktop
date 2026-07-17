namespace EnglishVoiceTutor.Api.Contracts.FeedbackReports;

public sealed class CreateFeedbackReportResponse
{
    public Guid ReportId { get; init; }
    public string Status { get; init; } = "received";
}
