using EnglishVoiceTutor.Api.Contracts.Billing;

namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayPurchaseVerificationService
{
    Task<GooglePlayPurchaseVerificationServiceResult> VerifyAsync(Guid userId, GooglePlayPurchaseVerificationRequest? request, CancellationToken cancellationToken);
}
