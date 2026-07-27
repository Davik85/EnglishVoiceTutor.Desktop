using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Billing;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayPurchaseVerificationService : IGooglePlayPurchaseVerificationService
{
    private readonly IGooglePlayPurchaseVerifier verifier;
    private readonly ILogger<GooglePlayPurchaseVerificationService> logger;

    public GooglePlayPurchaseVerificationService(IGooglePlayPurchaseVerifier verifier, ILogger<GooglePlayPurchaseVerificationService> logger)
    {
        this.verifier = verifier;
        this.logger = logger;
    }

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

        GooglePlayPurchaseVerificationResult result;
        try
        {
            result = await verifier.VerifyAsync(userId, request.PurchaseToken, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            logger.LogWarning("Google Play purchase verification completed with safe result {ResultCode}. AuthenticatedUserResolved={AuthenticatedUserResolved}.", "temporarily_unavailable", true);
            return Unavailable("temporarily_unavailable", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationTemporarilyUnavailableMessage);
        }

        var mapped = Map(result.Code);
        logger.LogInformation("Google Play purchase verification completed with safe result {ResultCode}. AuthenticatedUserResolved={AuthenticatedUserResolved}.", mapped.Response.Result, true);
        return mapped;
    }

    private static GooglePlayPurchaseVerificationServiceResult Map(GooglePlayPurchaseVerificationResultCode code) => code switch
    {
        GooglePlayPurchaseVerificationResultCode.NotConfigured => Unavailable("not_configured", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationUnavailableMessage),
        GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable => Unavailable("temporarily_unavailable", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationTemporarilyUnavailableMessage),
        GooglePlayPurchaseVerificationResultCode.Verified => Ok("verified", "Purchase verified.", true),
        GooglePlayPurchaseVerificationResultCode.Pending => Ok("pending", "Purchase is pending.", true),
        GooglePlayPurchaseVerificationResultCode.AlreadyProcessed => Ok("already_processed", "Purchase was already processed.", true),
        GooglePlayPurchaseVerificationResultCode.InvalidPurchase => Ok("invalid_purchase", "Purchase could not be verified.", false),
        GooglePlayPurchaseVerificationResultCode.UnsupportedProduct => Ok("unsupported_product", "This purchase is not supported.", false),
        GooglePlayPurchaseVerificationResultCode.OwnershipConflict => Ok("ownership_conflict", "Purchase cannot be applied to this account.", false),
        _ => Unavailable("temporarily_unavailable", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationTemporarilyUnavailableMessage)
    };

    private static GooglePlayPurchaseVerificationServiceResult BadRequest(string message) => new(StatusCodes.Status400BadRequest, new GooglePlayPurchaseVerificationResponse { Result = "invalid_purchase", Message = message, SubscriptionStatusRefreshRecommended = false });
    private static GooglePlayPurchaseVerificationServiceResult Unavailable(string result, string message) => new(StatusCodes.Status503ServiceUnavailable, new GooglePlayPurchaseVerificationResponse { Result = result, Message = message, SubscriptionStatusRefreshRecommended = false });
    private static GooglePlayPurchaseVerificationServiceResult Ok(string result, string message, bool refresh) => new(StatusCodes.Status200OK, new GooglePlayPurchaseVerificationResponse { Result = result, Message = message, SubscriptionStatusRefreshRecommended = refresh });
}
