namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class GooglePlayInitialPremiumDeferralEntity
{
    public Guid Id { get; set; }
    public Guid GooglePlayPurchaseClaimId { get; set; }
    public Guid UserId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public DateTimeOffset ProviderPurchaseStartedAtUtc { get; set; }
    public DateTimeOffset BaselineProviderExpiryUtc { get; set; }
    public DateTimeOffset ExistingCoverageStartsAtUtc { get; set; }
    public DateTimeOffset ExistingCoverageTailUtc { get; set; }
    public bool IsLicenseTestPurchase { get; set; }
    public long ApprovedDeferDurationTicks { get; set; }
    public DateTimeOffset TargetProviderExpiryUtc { get; set; }
    public string? CommandEtag { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTimeOffset? LastAttemptAtUtc { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public string? LastSafeErrorCode { get; set; }
    public DateTimeOffset? ProviderResponseExpiryUtc { get; set; }
    public DateTimeOffset? AuthoritativeProviderExpiryUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public long ConcurrencyRevision { get; set; }
}
