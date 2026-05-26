namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminUserUsageEventSnapshot
{
    public Guid UsageEventId { get; set; }
    public Guid? SessionId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string StudyLanguage { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? AudioInputTokens { get; set; }
    public int? AudioOutputTokens { get; set; }
    public int? AudioDurationMs { get; set; }
    public int? InputChars { get; set; }
    public long? OutputBytes { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
