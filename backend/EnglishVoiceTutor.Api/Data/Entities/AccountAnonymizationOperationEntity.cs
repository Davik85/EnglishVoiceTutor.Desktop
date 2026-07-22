namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class AccountAnonymizationOperationEntity
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public Guid TargetUserId { get; set; }
    public Guid PolicySnapshotId { get; set; }
    public Guid ActorAdminUserId { get; set; }
    public string State { get; set; } = string.Empty;
    public int PreflightVersion { get; set; }
    public string PreflightFingerprint { get; set; } = string.Empty;
    public string ProcedureVersion { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string CategoryCountsJson { get; set; } = string.Empty;
    public string BlockingCodesJson { get; set; } = string.Empty;
    public string RetentionSummaryJson { get; set; } = string.Empty;
    public string ProviderStatesJson { get; set; } = string.Empty;
    public string BackupReconciliationState { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int ConcurrencyRevision { get; set; }
    public UserFeedbackReportEntity Report { get; set; } = null!;
    public UserEntity TargetUser { get; set; } = null!;
    public AccountAnonymizationPolicySnapshotEntity PolicySnapshot { get; set; } = null!;
    public AdminUserEntity ActorAdminUser { get; set; } = null!;
}
