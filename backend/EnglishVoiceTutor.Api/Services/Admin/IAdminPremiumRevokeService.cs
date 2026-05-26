using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminPremiumRevokeService
{
    Task<AdminManualPremiumRevokeResult> RevokePremiumAsync(
        Guid adminUserId,
        Guid targetUserId,
        Guid entitlementId,
        AdminManualPremiumRevokeRequest request,
        CancellationToken cancellationToken);
}

public sealed class AdminManualPremiumRevokeResult
{
    public bool IsInvalid { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsConflict { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public AdminManualPremiumRevokeResponse? Response { get; init; }
}
