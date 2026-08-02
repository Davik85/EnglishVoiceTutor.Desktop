using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Billing;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayPurchaseVerificationService(
    IGooglePlayPurchaseProcessor processor,
    ILogger<GooglePlayPurchaseVerificationService> logger) : IGooglePlayPurchaseVerificationService
{
    public async Task<GooglePlayPurchaseVerificationServiceResult> VerifyAsync(Guid userId, GooglePlayPurchaseVerificationRequest? request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userId == Guid.Empty || request is null || string.IsNullOrWhiteSpace(request.PurchaseToken))
        {
            return BadRequest(SubscriptionConstants.Billing.GooglePlayPurchaseTokenRequiredMessage);
        }

        if (request.PurchaseToken.Length > SubscriptionConstants.Billing.GooglePlayPurchaseTokenMaximumLength)
        {
            return BadRequest(SubscriptionConstants.Billing.GooglePlayPurchaseTokenTooLongMessage);
        }

        try
        {
            var mapped = MapProcessorResult((await processor.ProcessAsync(userId, request.PurchaseToken, cancellationToken)).Code);
            logger.LogInformation("Google Play purchase verification completed with safe result {ResultCode}. AuthenticatedUserResolved={AuthenticatedUserResolved}.", mapped.Response.Result, true);
            return mapped;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            logger.LogWarning("Google Play purchase verification completed with safe result {ResultCode}. AuthenticatedUserResolved={AuthenticatedUserResolved}.", "temporarily_unavailable", true);
            return Unavailable("temporarily_unavailable", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationTemporarilyUnavailableMessage);
        }
    }

    private static GooglePlayPurchaseVerificationServiceResult MapProcessorResult(GooglePlayPurchaseProcessingResultCode code) => code switch
    {
        GooglePlayPurchaseProcessingResultCode.Verified => Ok("verified", "Purchase verified.", true),
        GooglePlayPurchaseProcessingResultCode.AcknowledgementPending => Unavailable("acknowledgement_pending", "Purchase verification is pending acknowledgement.", true),
        GooglePlayPurchaseProcessingResultCode.AcknowledgementInconsistent => Ok("invalid_purchase", "Purchase could not be verified.", true),
        GooglePlayPurchaseProcessingResultCode.NotConfigured => Unavailable("not_configured", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationUnavailableMessage),
        GooglePlayPurchaseProcessingResultCode.TemporarilyUnavailable => Unavailable("temporarily_unavailable", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationTemporarilyUnavailableMessage),
        GooglePlayPurchaseProcessingResultCode.Pending => Ok("pending", "Purchase is pending.", true),
        GooglePlayPurchaseProcessingResultCode.InvalidPurchase => Ok("invalid_purchase", "Purchase could not be verified.", false),
        GooglePlayPurchaseProcessingResultCode.UnsupportedProduct => Ok("unsupported_product", "This purchase is not supported.", false),
        GooglePlayPurchaseProcessingResultCode.OwnershipConflict => Ok("ownership_conflict", "Purchase cannot be applied to this account.", false),
        _ => Unavailable("temporarily_unavailable", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationTemporarilyUnavailableMessage)
    };

    private static GooglePlayPurchaseVerificationServiceResult BadRequest(string message) => new(StatusCodes.Status400BadRequest, new GooglePlayPurchaseVerificationResponse { Result = "invalid_purchase", Message = message, SubscriptionStatusRefreshRecommended = false });
    private static GooglePlayPurchaseVerificationServiceResult Unavailable(string result, string message, bool refresh = false) => new(StatusCodes.Status503ServiceUnavailable, new GooglePlayPurchaseVerificationResponse { Result = result, Message = message, SubscriptionStatusRefreshRecommended = refresh });
    private static GooglePlayPurchaseVerificationServiceResult Ok(string result, string message, bool refresh) => new(StatusCodes.Status200OK, new GooglePlayPurchaseVerificationResponse { Result = result, Message = message, SubscriptionStatusRefreshRecommended = refresh });
}
