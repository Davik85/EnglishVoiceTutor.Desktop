namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class BillingEventEntity
{
    public Guid Id { get; set; }
    public string BillingProvider { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ProviderEventId { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SafeMetadataJson { get; set; }
    public string? ErrorMessage { get; set; }
}
