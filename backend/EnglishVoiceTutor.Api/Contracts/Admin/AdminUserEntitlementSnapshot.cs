namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminUserEntitlementSnapshot
{
    public Guid EntitlementId { get; set; }
    public string PlanId { get; set; } = string.Empty;
    public string EntitlementType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
