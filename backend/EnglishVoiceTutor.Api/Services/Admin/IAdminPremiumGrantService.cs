using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminPremiumGrantService
{
    Task<AdminManualPremiumGrantResult> GrantPremiumAsync(
        Guid adminUserId,
        Guid targetUserId,
        AdminManualPremiumGrantRequest request,
        CancellationToken cancellationToken);
}

public sealed class AdminManualPremiumGrantResult
{
    public bool IsInvalid { get; init; }
    public bool IsNotFound { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public AdminManualPremiumGrantResponse? Response { get; init; }
}
