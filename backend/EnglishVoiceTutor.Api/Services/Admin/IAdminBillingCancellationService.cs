using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminBillingCancellationService
{
    Task<AdminBillingCancelRenewalResult> CancelRenewalAsync(
        Guid adminUserId,
        Guid targetUserId,
        AdminBillingCancelRenewalRequest request,
        CancellationToken cancellationToken);
}

public sealed class AdminBillingCancelRenewalResult
{
    public bool IsInvalid { get; init; }
    public bool IsNotFound { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public AdminBillingCancelRenewalResponse? Response { get; init; }
}
