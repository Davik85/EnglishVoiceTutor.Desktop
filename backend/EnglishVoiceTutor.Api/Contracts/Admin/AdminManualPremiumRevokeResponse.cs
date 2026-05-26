namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminManualPremiumRevokeResponse
{
    public Guid EntitlementId { get; init; }
    public Guid UserId { get; init; }
    public string PlanId { get; init; } = string.Empty;
    public string EntitlementType { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset? ExpiresAtUtc { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset RevokedAtUtc { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public bool AuditWritten { get; init; }
}
