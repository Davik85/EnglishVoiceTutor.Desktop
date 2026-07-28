using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Billing;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayPurchaseVerificationService : IGooglePlayPurchaseVerificationService
{
    private readonly IGooglePlayPurchaseVerifier verifier;
    private readonly IGooglePlayVerifiedPurchasePersistenceService persistenceService;
    private readonly ILogger<GooglePlayPurchaseVerificationService> logger;

    public GooglePlayPurchaseVerificationService(
        IGooglePlayPurchaseVerifier verifier,
        IGooglePlayVerifiedPurchasePersistenceService persistenceService,
        ILogger<GooglePlayPurchaseVerificationService> logger)
    {
        this.verifier = verifier;
        this.persistenceService = persistenceService;
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
            var mapped = result.Code == GooglePlayPurchaseVerificationResultCode.Verified
                ? await MapVerifiedAsync(userId, request.PurchaseToken, result.VerifiedPurchase, cancellationToken)
                : MapProviderResult(result.Code);
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

    private async Task<GooglePlayPurchaseVerificationServiceResult> MapVerifiedAsync(Guid userId, string purchaseToken, GooglePlayVerifiedPurchase? verifiedPurchase, CancellationToken cancellationToken)
    {
        if (verifiedPurchase is null)
        {
            return Unavailable("temporarily_unavailable", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationTemporarilyUnavailableMessage);
        }

        var persistence = await persistenceService.PersistAsync(
            new GooglePlayVerifiedPurchasePersistenceRequest(userId, purchaseToken, verifiedPurchase),
            cancellationToken);
        return persistence.Code switch
        {
            GooglePlayVerifiedPurchasePersistenceResultCode.Applied => Ok("verified", "Purchase verified.", true),
            GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent => Ok("already_processed", "Purchase was already processed.", true),
            GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict => Ok("ownership_conflict", "Purchase cannot be applied to this account.", false),
            GooglePlayVerifiedPurchasePersistenceResultCode.ProductMismatch or GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict => Ok("invalid_purchase", "Purchase could not be verified.", false),
            GooglePlayVerifiedPurchasePersistenceResultCode.TestPurchaseNotSupported => Ok("unsupported_product", "This purchase is not supported.", false),
            _ => Unavailable("temporarily_unavailable", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationTemporarilyUnavailableMessage)
        };
    }

    private static GooglePlayPurchaseVerificationServiceResult MapProviderResult(GooglePlayPurchaseVerificationResultCode code) => code switch
    {
        GooglePlayPurchaseVerificationResultCode.NotConfigured => Unavailable("not_configured", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationUnavailableMessage),
        GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable => Unavailable("temporarily_unavailable", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationTemporarilyUnavailableMessage),
        GooglePlayPurchaseVerificationResultCode.Pending => Ok("pending", "Purchase is pending.", true),
        GooglePlayPurchaseVerificationResultCode.InvalidPurchase => Ok("invalid_purchase", "Purchase could not be verified.", false),
        GooglePlayPurchaseVerificationResultCode.UnsupportedProduct => Ok("unsupported_product", "This purchase is not supported.", false),
        _ => Unavailable("temporarily_unavailable", SubscriptionConstants.Billing.GooglePlayPurchaseVerificationTemporarilyUnavailableMessage)
    };

    private static GooglePlayPurchaseVerificationServiceResult BadRequest(string message) => new(StatusCodes.Status400BadRequest, new GooglePlayPurchaseVerificationResponse { Result = "invalid_purchase", Message = message, SubscriptionStatusRefreshRecommended = false });
    private static GooglePlayPurchaseVerificationServiceResult Unavailable(string result, string message) => new(StatusCodes.Status503ServiceUnavailable, new GooglePlayPurchaseVerificationResponse { Result = result, Message = message, SubscriptionStatusRefreshRecommended = false });
    private static GooglePlayPurchaseVerificationServiceResult Ok(string result, string message, bool refresh) => new(StatusCodes.Status200OK, new GooglePlayPurchaseVerificationResponse { Result = result, Message = message, SubscriptionStatusRefreshRecommended = refresh });
}
