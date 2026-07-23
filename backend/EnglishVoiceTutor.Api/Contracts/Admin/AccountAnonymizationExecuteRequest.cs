namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AccountAnonymizationExecuteRequest
{
    public Guid OperationId { get; init; }
    public string PreflightFingerprint { get; init; } = string.Empty;
}

public sealed class AccountAnonymizationExecuteResponse
{
    public Guid OperationId { get; init; }
    public string State { get; init; } = string.Empty;
    public string VerificationState { get; init; } = string.Empty;
    public DateTimeOffset? CompletedAtUtc { get; init; }
}
