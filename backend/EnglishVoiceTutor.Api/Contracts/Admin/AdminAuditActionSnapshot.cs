namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminAuditActionSnapshot
{
    public Guid AdminActionId { get; init; }
    public Guid? AdminUserId { get; init; }
    public Guid TargetUserId { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public string? SafeMetadataJson { get; init; }
}
