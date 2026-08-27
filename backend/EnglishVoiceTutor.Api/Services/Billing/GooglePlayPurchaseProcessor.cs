namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayPurchaseProcessor(
    IGooglePlayPurchaseVerifier verifier,
    IGooglePlayVerifiedPurchasePersistenceService persistenceService,
    IGooglePlayPurchaseTokenProtectionService tokenProtectionService,
    IGooglePlaySubscriptionsV2Client subscriptionsClient,
    ILogger<GooglePlayPurchaseProcessor> logger,
    IGooglePlayTrialDeferralService? trialDeferralService = null) : IGooglePlayPurchaseProcessor
{
    public Task<GooglePlayPurchaseProcessingResult> ProcessAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) =>
        ProcessAsync(userId, purchaseToken, new GooglePlayPurchaseProcessingContext(), cancellationToken);

    public async Task<GooglePlayPurchaseProcessingResult> ProcessAsync(Guid userId, string purchaseToken, GooglePlayPurchaseProcessingContext context, CancellationToken cancellationToken)
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
        var verifiedPurchase = context.ProviderConfirmedRevocation
            && verification.VerifiedPurchase.LifecycleState == GooglePlaySubscriptionLifecycleState.Expired
            ? verification.VerifiedPurchase with { LifecycleState = GooglePlaySubscriptionLifecycleState.Revoked }
            : verification.VerifiedPurchase;

        string protectedPurchaseToken;
        try { protectedPurchaseToken = tokenProtectionService.Protect(purchaseToken); }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return Result(GooglePlayPurchaseProcessingResultCode.TemporarilyUnavailable); }
        var persistence = await persistenceService.PersistAsync(new GooglePlayVerifiedPurchasePersistenceRequest(userId, purchaseToken, verifiedPurchase, protectedPurchaseToken), cancellationToken);
        if (persistence.Code is not GooglePlayVerifiedPurchasePersistenceResultCode.Applied and not GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent)
        {
            return Result(MapPersistenceResult(persistence.Code));
        }

        if (!RequiresAcknowledgement(verifiedPurchase.LifecycleState)
            || verifiedPurchase.AcknowledgementState == GooglePlayPurchaseAcknowledgementState.Acknowledged)
        {
            await persistenceService.UpdateAcknowledgementStateAsync(purchaseToken, false, null, cancellationToken);
            return await ProcessTrialDeferralAsync(userId, purchaseToken, protectedPurchaseToken, cancellationToken);
        }

        try
        {
            await subscriptionsClient.AcknowledgeAsync(
                verifiedPurchase.PackageName,
                verifiedPurchase.ProductId,
                purchaseToken,
                cancellationToken);
            await persistenceService.UpdateAcknowledgementStateAsync(purchaseToken, false, null, cancellationToken);
            return await ProcessTrialDeferralAsync(userId, purchaseToken, protectedPurchaseToken, cancellationToken);
        }
        catch (GooglePlaySubscriptionsV2ClientException exception) when (exception.Failure == GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable)
        {
            await persistenceService.UpdateAcknowledgementStateAsync(purchaseToken, true, GooglePlayRtdnSafeErrorCodes.ProviderUnavailable, cancellationToken);
            logger.LogWarning("Google Play purchase acknowledgement requires retry. AuthenticatedUserResolved={AuthenticatedUserResolved}.", true);
            return Result(GooglePlayPurchaseProcessingResultCode.AcknowledgementPending);
        }
        catch (GooglePlaySubscriptionsV2ClientException exception) when (exception.Failure == GooglePlaySubscriptionsV2ClientFailure.InvalidPurchase)
        {
            await persistenceService.UpdateAcknowledgementStateAsync(purchaseToken, true, GooglePlayRtdnSafeErrorCodes.ProviderRejected, cancellationToken);
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

    private static GooglePlayPurchaseProcessingResultCode MapPersistenceResult(GooglePlayVerifiedPurchasePersistenceResultCode code) => code switch
    {
        GooglePlayVerifiedPurchasePersistenceResultCode.Applied or GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent => GooglePlayPurchaseProcessingResultCode.Verified,
        GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict => GooglePlayPurchaseProcessingResultCode.OwnershipConflict,
        GooglePlayVerifiedPurchasePersistenceResultCode.ProductMismatch or GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict => GooglePlayPurchaseProcessingResultCode.InvalidPurchase,
        GooglePlayVerifiedPurchasePersistenceResultCode.TestPurchaseNotSupported => GooglePlayPurchaseProcessingResultCode.UnsupportedProduct,
        _ => GooglePlayPurchaseProcessingResultCode.TemporarilyUnavailable
    };

    private static bool RequiresAcknowledgement(GooglePlaySubscriptionLifecycleState lifecycleState) => lifecycleState is
        GooglePlaySubscriptionLifecycleState.Active
        or GooglePlaySubscriptionLifecycleState.InGracePeriod
        or GooglePlaySubscriptionLifecycleState.Canceled;

    private async Task<GooglePlayPurchaseProcessingResult> ProcessTrialDeferralAsync(
        Guid userId,
        string purchaseToken,
        string protectedPurchaseToken,
        CancellationToken cancellationToken)
    {
        if (trialDeferralService is null) return Result(GooglePlayPurchaseProcessingResultCode.Verified);
        try
        {
            var deferral = await trialDeferralService.ProcessAsync(userId, purchaseToken, protectedPurchaseToken, cancellationToken);
            return Result(deferral.Code switch
            {
                GooglePlayTrialDeferralResultCode.NotRequired or GooglePlayTrialDeferralResultCode.Completed => GooglePlayPurchaseProcessingResultCode.Verified,
                GooglePlayTrialDeferralResultCode.Pending => GooglePlayPurchaseProcessingResultCode.TrialDeferralPending,
                _ => GooglePlayPurchaseProcessingResultCode.TrialDeferralAmbiguous
            });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            logger.LogWarning("Google Play trial deferral requires retry. ResultCode={ResultCode}.", GooglePlayTrialDeferralSafeErrorCodes.PersistenceUnavailable);
            return Result(GooglePlayPurchaseProcessingResultCode.TrialDeferralPending);
        }
    }

    private static GooglePlayPurchaseProcessingResult Result(GooglePlayPurchaseProcessingResultCode code) => new(code);
}
