namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class GooglePlayPurchaseTokenSecretEntity
{
    public Guid Id { get; set; }
    public Guid GooglePlayPurchaseClaimId { get; set; }
    public string PurchaseTokenFingerprint { get; set; } = string.Empty;
    public string ProtectedPurchaseToken { get; set; } = string.Empty;
    public string ProtectionFormatVersion { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? SupersededAtUtc { get; set; }
    public DateTimeOffset? RetentionDeleteAfterUtc { get; set; }
    public DateTimeOffset? LastProviderCheckAtUtc { get; set; }
    public DateTimeOffset? NextProviderCheckAtUtc { get; set; }
    public int ReconciliationAttemptCount { get; set; }
    public string? LastSafeResultCode { get; set; }
    public DateTimeOffset? FinalRecheckUntilUtc { get; set; }
    public bool AcknowledgementPending { get; set; }
    public GooglePlayPurchaseClaimEntity GooglePlayPurchaseClaim { get; set; } = null!;
}
