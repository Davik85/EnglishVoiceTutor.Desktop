namespace EnglishVoiceTutor.Api.Contracts.Usage;

public sealed class FreeLimitExceededResponse
{
    public string Error { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string LimitType { get; set; } = string.Empty;
    public int Used { get; set; }
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public DateOnly UsageDate { get; set; }
    public string StudyLanguage { get; set; } = string.Empty;
    public DateTimeOffset CheckedAtUtc { get; set; }
    public string Source { get; set; } = string.Empty;
}
