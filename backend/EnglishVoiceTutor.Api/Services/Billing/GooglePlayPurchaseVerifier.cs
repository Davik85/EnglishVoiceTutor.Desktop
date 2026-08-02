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
            var result = MapSnapshot(snapshot, options, userId);
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

    private static GooglePlayPurchaseVerificationResult MapSnapshot(GooglePlaySubscriptionV2Snapshot? snapshot, GooglePlayBillingOptions options, Guid userId)
    {
        if (snapshot is null || snapshot.HasLinkedPurchaseToken) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        if (string.Equals(snapshot.SubscriptionState, "SUBSCRIPTION_STATE_PENDING", StringComparison.Ordinal)) return Result(GooglePlayPurchaseVerificationResultCode.Pending);
        if (!string.Equals(snapshot.SubscriptionState, "SUBSCRIPTION_STATE_ACTIVE", StringComparison.Ordinal)) return Result(GooglePlayPurchaseVerificationResultCode.InvalidPurchase);

        if (snapshot.StartTimeUtc is null || snapshot.AcknowledgementState is not GooglePlayPurchaseAcknowledgementState.Pending and not GooglePlayPurchaseAcknowledgementState.Acknowledged)
        {
            return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        }

        var lineItems = snapshot.LineItems
            .Where(item => !string.IsNullOrWhiteSpace(item.ProductId))
            .ToArray();
        if (lineItems.Length == 0 || lineItems.Any(item => item.ExpiryTimeUtc is null)) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);

        var productIds = lineItems.Select(item => item.ProductId!).Distinct(StringComparer.Ordinal).ToArray();
        if (productIds.Length != 1 || !options.AllowedProductIds.Contains(productIds[0], StringComparer.Ordinal)) return Result(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct);
        if (snapshot.IsTestPurchase && !IsAllowedTestPurchase(options, userId)) return Result(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct);

        var expiryTimes = lineItems
            .Where(item => string.Equals(item.ProductId, productIds[0], StringComparison.Ordinal))
            .Select(item => item.ExpiryTimeUtc!.Value)
            .Distinct()
            .ToArray();
        if (expiryTimes.Length != 1) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);

        var startedAtUtc = snapshot.StartTimeUtc.Value.ToUniversalTime();
        var expiresAtUtc = expiryTimes[0].ToUniversalTime();
        if (expiresAtUtc <= startedAtUtc) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);

        return new GooglePlayPurchaseVerificationResult(
            GooglePlayPurchaseVerificationResultCode.Verified,
            new GooglePlayVerifiedPurchase(
                options.PackageName,
                productIds[0],
                startedAtUtc,
                expiresAtUtc,
                snapshot.AcknowledgementState.Value,
                snapshot.IsTestPurchase));
    }

    private static GooglePlayPurchaseVerificationResult Result(GooglePlayPurchaseVerificationResultCode code) => new(code);

    private static bool IsAllowedTestPurchase(GooglePlayBillingOptions options, Guid userId) =>
        options.Enabled
        && options.TestPurchasesEnabled
        && options.AllowedTestPurchaseUserIds.Any(value => Guid.TryParse(value, out var allowedUserId) && allowedUserId == userId);
}
