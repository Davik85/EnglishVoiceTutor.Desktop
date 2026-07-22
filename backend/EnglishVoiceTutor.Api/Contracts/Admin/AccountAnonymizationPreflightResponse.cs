namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AccountAnonymizationPreflightResponse
{
    public Guid OperationId { get; init; }
    public Guid ReportId { get; init; }
    public string State { get; init; } = string.Empty;
    public int PreflightVersion { get; init; }
    public string PreflightFingerprint { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public IReadOnlyDictionary<string, int> CategoryCounts { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<string> BlockingReasonCodes { get; init; } = [];
    public AccountAnonymizationRetentionSummaryResponse RetentionSummary { get; init; } = new();
    public IReadOnlyList<AccountAnonymizationProviderStateResponse> ProviderStates { get; init; } = [];
    public string BackupReconciliationState { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}

public sealed class AccountAnonymizationRetentionSummaryResponse
{
    public int ImmediateDeleteOrAnonymizeCount { get; init; }
    public int UnresolvedDecisionCount { get; init; }
}

public sealed class AccountAnonymizationProviderStateResponse
{
    public string ProviderKey { get; init; } = string.Empty;
    public int RecordCount { get; init; }
    public IReadOnlyList<string> StateCodes { get; init; } = [];
}
