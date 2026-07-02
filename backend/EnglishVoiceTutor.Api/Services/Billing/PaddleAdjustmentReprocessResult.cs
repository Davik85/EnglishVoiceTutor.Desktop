namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed record PaddleAdjustmentReprocessResult(
    string Result,
    string? BlockReason,
    string? EventType,
    string ProviderEventId,
    string? ProviderTransactionId,
    string? ProviderSubscriptionId,
    string UserResolutionSource,
    Guid? ResolvedUserId,
    bool FullRefundDetected,
    bool ChargebackDetected,
    int EntitlementCandidatesCount,
    int RevokedCount)
{
    public bool Succeeded => string.Equals(Result, PaddleAdjustmentReprocessResults.Revoked, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Result, PaddleAdjustmentReprocessResults.AlreadyRevoked, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Result, PaddleAdjustmentReprocessResults.PartialRefundSkipped, StringComparison.OrdinalIgnoreCase);
}

public static class PaddleAdjustmentReprocessResults
{
    public const string Revoked = "Revoked";
    public const string AlreadyRevoked = "AlreadyRevoked";
    public const string PartialRefundSkipped = "PartialRefundSkipped";
    public const string NotFound = "NotFound";
    public const string RefusedEventType = "RefusedEventType";
    public const string Blocked = "Blocked";
    public const string Failed = "Failed";
}
