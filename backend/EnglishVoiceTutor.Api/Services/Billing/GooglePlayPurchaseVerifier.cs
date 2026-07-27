using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayPurchaseVerifier(
    IGooglePlaySubscriptionsV2Client client,
    IOptions<GooglePlayBillingOptions> optionsAccessor,
    ILogger<GooglePlayPurchaseVerifier> logger) : IGooglePlayPurchaseVerifier
{
    public async Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = optionsAccessor.Value;
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.PackageName) || !options.AllowedProductIds.Any(productId => !string.IsNullOrWhiteSpace(productId)))
        {
            return Result(GooglePlayPurchaseVerificationResultCode.NotConfigured);
        }

        try
        {
            var snapshot = await client.GetAsync(options.PackageName, purchaseToken, cancellationToken);
            var result = MapSnapshot(snapshot, options.AllowedProductIds);
            logger.LogInformation("Google Play subscriptions-v2 verification completed with safe result {ResultCode}.", result.Code);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (GooglePlaySubscriptionsV2ClientException exception)
        {
            var result = exception.Failure == GooglePlaySubscriptionsV2ClientFailure.InvalidPurchase
                ? Result(GooglePlayPurchaseVerificationResultCode.InvalidPurchase)
                : Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
            logger.LogWarning("Google Play subscriptions-v2 verification completed with safe result {ResultCode}.", result.Code);
            return result;
        }
        catch (Exception)
        {
            logger.LogWarning("Google Play subscriptions-v2 verification completed with safe result {ResultCode}.", GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
            return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        }
    }

    private static GooglePlayPurchaseVerificationResult MapSnapshot(GooglePlaySubscriptionV2Snapshot? snapshot, IReadOnlyCollection<string> allowedProductIds)
    {
        if (snapshot is null || snapshot.HasLinkedPurchaseToken) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        if (string.Equals(snapshot.SubscriptionState, "SUBSCRIPTION_STATE_PENDING", StringComparison.Ordinal)) return Result(GooglePlayPurchaseVerificationResultCode.Pending);
        if (!string.Equals(snapshot.SubscriptionState, "SUBSCRIPTION_STATE_ACTIVE", StringComparison.Ordinal)) return Result(GooglePlayPurchaseVerificationResultCode.InvalidPurchase);

        var productIds = snapshot.ProductIds.Where(productId => !string.IsNullOrWhiteSpace(productId)).Distinct(StringComparer.Ordinal).ToArray();
        if (productIds.Length == 0) return Result(GooglePlayPurchaseVerificationResultCode.InvalidPurchase);
        if (productIds.Length != 1 || !allowedProductIds.Contains(productIds[0], StringComparer.Ordinal)) return Result(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct);
        return new GooglePlayPurchaseVerificationResult(GooglePlayPurchaseVerificationResultCode.Verified, new GooglePlayVerifiedPurchase(productIds[0]));
    }

    private static GooglePlayPurchaseVerificationResult Result(GooglePlayPurchaseVerificationResultCode code) => new(code);
}
