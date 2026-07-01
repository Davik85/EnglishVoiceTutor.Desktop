namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminActivityEventsResponse
{
    public List<AdminActivityEventSnapshot> Items { get; set; } = [];
    public int Limit { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public string Note { get; set; } = "Read-only actor-centric activity assembled from existing audit tables plus dedicated admin_auth_audit_events after migration.";
}

public sealed class AdminActivityEventSnapshot
{
    public string EventId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? ActorAdminUserId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public string? TargetUserEmail { get; set; }
    public Guid? TargetAdminUserId { get; set; }
    public string? TargetAdminUserEmail { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? StableKey { get; set; }
    public string? Reason { get; set; }
    public string? AdminNote { get; set; }
    public string? SafeMetadataJson { get; set; }
}
