namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayPurchaseProcessor(
    IGooglePlayPurchaseVerifier verifier,
    IGooglePlayVerifiedPurchasePersistenceService persistenceService,
    IGooglePlaySubscriptionsV2Client subscriptionsClient,
    ILogger<GooglePlayPurchaseProcessor> logger) : IGooglePlayPurchaseProcessor
{
    public async Task<GooglePlayPurchaseProcessingResult> ProcessAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken)
    {
        var verification = await verifier.VerifyAsync(userId, purchaseToken, cancellationToken);
        if (verification.Code != GooglePlayPurchaseVerificationResultCode.Verified)
        {
            return Result(verification.Code switch
            {
                GooglePlayPurchaseVerificationResultCode.Pending => GooglePlayPurchaseProcessingResultCode.Pending,
                GooglePlayPurchaseVerificationResultCode.InvalidPurchase => GooglePlayPurchaseProcessingResultCode.InvalidPurchase,
                GooglePlayPurchaseVerificationResultCode.UnsupportedProduct => GooglePlayPurchaseProcessingResultCode.UnsupportedProduct,
                GooglePlayPurchaseVerificationResultCode.NotConfigured => GooglePlayPurchaseProcessingResultCode.NotConfigured,
                _ => GooglePlayPurchaseProcessingResultCode.TemporarilyUnavailable
            });
        }

        if (verification.VerifiedPurchase is null) return Result(GooglePlayPurchaseProcessingResultCode.TemporarilyUnavailable);

        var persistence = await persistenceService.PersistAsync(
            new GooglePlayVerifiedPurchasePersistenceRequest(userId, purchaseToken, verification.VerifiedPurchase),
            cancellationToken);
        if (persistence.Code is not GooglePlayVerifiedPurchasePersistenceResultCode.Applied and not GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent)
        {
            return Result(persistence.Code switch
            {
                GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict => GooglePlayPurchaseProcessingResultCode.OwnershipConflict,
                GooglePlayVerifiedPurchasePersistenceResultCode.ProductMismatch or GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict => GooglePlayPurchaseProcessingResultCode.InvalidPurchase,
                GooglePlayVerifiedPurchasePersistenceResultCode.TestPurchaseNotSupported => GooglePlayPurchaseProcessingResultCode.UnsupportedProduct,
                _ => GooglePlayPurchaseProcessingResultCode.TemporarilyUnavailable
            });
        }

        if (verification.VerifiedPurchase.AcknowledgementState == GooglePlayPurchaseAcknowledgementState.Acknowledged)
        {
            return Result(GooglePlayPurchaseProcessingResultCode.Verified);
        }

        try
        {
            await subscriptionsClient.AcknowledgeAsync(
                verification.VerifiedPurchase.PackageName,
                verification.VerifiedPurchase.ProductId,
                purchaseToken,
                cancellationToken);
            return Result(GooglePlayPurchaseProcessingResultCode.Verified);
        }
        catch (GooglePlaySubscriptionsV2ClientException exception) when (exception.Failure == GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable)
        {
            logger.LogWarning("Google Play purchase acknowledgement requires retry. AuthenticatedUserResolved={AuthenticatedUserResolved}.", true);
            return Result(GooglePlayPurchaseProcessingResultCode.AcknowledgementPending);
        }
        catch (GooglePlaySubscriptionsV2ClientException exception) when (exception.Failure == GooglePlaySubscriptionsV2ClientFailure.InvalidPurchase)
        {
            logger.LogWarning("Google Play purchase acknowledgement has a consistency failure requiring reconciliation. AuthenticatedUserResolved={AuthenticatedUserResolved}.", true);
            return Result(GooglePlayPurchaseProcessingResultCode.AcknowledgementInconsistent);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            logger.LogWarning("Google Play purchase acknowledgement requires retry. AuthenticatedUserResolved={AuthenticatedUserResolved}.", true);
            return Result(GooglePlayPurchaseProcessingResultCode.AcknowledgementPending);
        }
    }

    private static GooglePlayPurchaseProcessingResult Result(GooglePlayPurchaseProcessingResultCode code) => new(code);
}
