using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Constants;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayPurchaseVerifier(
    IGooglePlaySubscriptionsV2Client client,
    IOptions<GooglePlayBillingOptions> optionsAccessor,
    IUtcClock utcClock,
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
            var result = MapSnapshot(snapshot, options, userId, purchaseToken, utcClock.UtcNow);
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

    private static GooglePlayPurchaseVerificationResult MapSnapshot(GooglePlaySubscriptionV2Snapshot? snapshot, GooglePlayBillingOptions options, Guid userId, string purchaseToken, DateTimeOffset now)
    {
        if (snapshot is null) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        if (string.Equals(snapshot.SubscriptionState, "SUBSCRIPTION_STATE_PENDING", StringComparison.Ordinal)) return Result(GooglePlayPurchaseVerificationResultCode.Pending);
        if (!string.Equals(snapshot.SubscriptionState, "SUBSCRIPTION_STATE_ACTIVE", StringComparison.Ordinal)) return Result(GooglePlayPurchaseVerificationResultCode.InvalidPurchase);

        if (snapshot.StartTimeUtc is null || snapshot.AcknowledgementState is not GooglePlayPurchaseAcknowledgementState.Pending and not GooglePlayPurchaseAcknowledgementState.Acknowledged)
        {
            return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        }

        var lineItems = snapshot.LineItems.ToArray();
        if (lineItems.Length == 0 || lineItems.Any(item => string.IsNullOrWhiteSpace(item.ProductId))) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        if (snapshot.IsTestPurchase && !IsAllowedTestPurchase(options, userId)) return Result(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct);
        if (lineItems.Any(item => !options.AllowedProductIds.Contains(item.ProductId!, StringComparer.Ordinal))) return Result(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct);

        var linkedToken = snapshot.LinkedPurchaseToken;
        if (linkedToken is not null && (linkedToken.Length > SubscriptionConstants.Billing.GooglePlayPurchaseTokenMaximumLength || string.IsNullOrWhiteSpace(linkedToken) || linkedToken.Any(char.IsWhiteSpace))) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        if (linkedToken is not null && string.Equals(linkedToken, purchaseToken, StringComparison.Ordinal)) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        var current = lineItems.Where(item => item.ExpiryTimeUtc is not null && item.ExpiryTimeUtc.Value > now).ToArray();
        var historical = lineItems.Where(item => item.ExpiryTimeUtc is not null && item.ExpiryTimeUtc.Value <= now).ToArray();
        var future = lineItems.Where(item => item.ExpiryTimeUtc is null).ToArray();
        GooglePlaySubscriptionLineItemSnapshot selected;
        if (linkedToken is null)
        {
            if (current.Length != 1 || historical.Length != 0 || future.Length != 0 || !string.IsNullOrWhiteSpace(current[0].DeferredItemReplacementProductId)) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
            selected = current[0];
        }
        else
        {
            if (current.Length != 1) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
            selected = current[0];
            if (historical.Length == 0 && future.Length == 0 && string.IsNullOrWhiteSpace(selected.DeferredItemReplacementProductId)) { }
            else if (historical.Length == 0 && future.Length == 1 && !string.IsNullOrWhiteSpace(selected.DeferredItemReplacementProductId) && string.Equals(selected.DeferredItemReplacementProductId, future[0].ProductId, StringComparison.Ordinal) && string.IsNullOrWhiteSpace(future[0].DeferredItemReplacementProductId))
            { }
            else if (historical.Length == 1 && future.Length == 0 && string.IsNullOrWhiteSpace(selected.DeferredItemReplacementProductId) && string.IsNullOrWhiteSpace(historical[0].DeferredItemReplacementProductId)) { }
            else return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        }
        if (!options.AllowedProductIds.Contains(selected.ProductId!, StringComparer.Ordinal)) return Result(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct);

        var startedAtUtc = snapshot.StartTimeUtc.Value.ToUniversalTime();
        var expiresAtUtc = selected.ExpiryTimeUtc!.Value.ToUniversalTime();
        if (expiresAtUtc <= startedAtUtc) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);

        var verifiedPurchase = new GooglePlayVerifiedPurchase(
                options.PackageName,
                selected.ProductId!,
                startedAtUtc,
                expiresAtUtc,
                snapshot.AcknowledgementState.Value,
                snapshot.IsTestPurchase) { LinkedPurchaseToken = linkedToken };
        return new GooglePlayPurchaseVerificationResult(GooglePlayPurchaseVerificationResultCode.Verified, verifiedPurchase);
    }

    private static GooglePlayPurchaseVerificationResult Result(GooglePlayPurchaseVerificationResultCode code) => new(code);

    private static bool IsAllowedTestPurchase(GooglePlayBillingOptions options, Guid userId) =>
        options.Enabled
        && options.TestPurchasesEnabled
        && options.AllowedTestPurchaseUserIds.Any(value => Guid.TryParse(value, out var allowedUserId) && allowedUserId == userId);
}
