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
        var lifecycleState = MapLifecycleState(snapshot.SubscriptionState);
        if (lifecycleState is null) return Result(GooglePlayPurchaseVerificationResultCode.InvalidPurchase);

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
        GooglePlaySubscriptionLineItemSnapshot? selected;
        if (lifecycleState is GooglePlaySubscriptionLifecycleState.Active or GooglePlaySubscriptionLifecycleState.InGracePeriod)
        {
            selected = SelectEntitlementRetainingLineItem(linkedToken, current, historical, future);
        }
        else
        {
            selected = SelectNonActiveLifecycleLineItem(linkedToken, current, historical, future);
        }
        if (selected is null) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        if (!options.AllowedProductIds.Contains(selected.ProductId!, StringComparer.Ordinal)) return Result(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct);

        var startedAtUtc = snapshot.StartTimeUtc.Value.ToUniversalTime();
        var expiresAtUtc = selected.ExpiryTimeUtc!.Value.ToUniversalTime();
        if (expiresAtUtc <= startedAtUtc) return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        if ((lifecycleState is GooglePlaySubscriptionLifecycleState.Active or GooglePlaySubscriptionLifecycleState.InGracePeriod) && expiresAtUtc <= now)
            return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);
        if (lifecycleState == GooglePlaySubscriptionLifecycleState.Expired && expiresAtUtc > now)
            return Result(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable);

        var verifiedPurchase = new GooglePlayVerifiedPurchase(
                options.PackageName,
                selected.ProductId!,
                startedAtUtc,
                expiresAtUtc,
                snapshot.AcknowledgementState.Value,
                snapshot.IsTestPurchase,
                lifecycleState.Value) { LinkedPurchaseToken = linkedToken };
        return new GooglePlayPurchaseVerificationResult(GooglePlayPurchaseVerificationResultCode.Verified, verifiedPurchase);
    }

    private static GooglePlaySubscriptionLineItemSnapshot? SelectEntitlementRetainingLineItem(
        string? linkedToken,
        IReadOnlyList<GooglePlaySubscriptionLineItemSnapshot> current,
        IReadOnlyList<GooglePlaySubscriptionLineItemSnapshot> historical,
        IReadOnlyList<GooglePlaySubscriptionLineItemSnapshot> future)
    {
        if (linkedToken is null)
        {
            return current.Count == 1 && historical.Count == 0 && future.Count == 0 && string.IsNullOrWhiteSpace(current[0].DeferredItemReplacementProductId)
                ? current[0]
                : null;
        }

        if (current.Count != 1) return null;
        var selected = current[0];
        if (historical.Count == 0 && future.Count == 0 && string.IsNullOrWhiteSpace(selected.DeferredItemReplacementProductId)) return selected;
        if (historical.Count == 0 && future.Count == 1 && !string.IsNullOrWhiteSpace(selected.DeferredItemReplacementProductId) && string.Equals(selected.DeferredItemReplacementProductId, future[0].ProductId, StringComparison.Ordinal) && string.IsNullOrWhiteSpace(future[0].DeferredItemReplacementProductId)) return selected;
        if (historical.Count == 1 && future.Count == 0 && string.IsNullOrWhiteSpace(selected.DeferredItemReplacementProductId) && string.IsNullOrWhiteSpace(historical[0].DeferredItemReplacementProductId)) return selected;
        return null;
    }

    private static GooglePlaySubscriptionLineItemSnapshot? SelectNonActiveLifecycleLineItem(
        string? linkedToken,
        IReadOnlyList<GooglePlaySubscriptionLineItemSnapshot> current,
        IReadOnlyList<GooglePlaySubscriptionLineItemSnapshot> historical,
        IReadOnlyList<GooglePlaySubscriptionLineItemSnapshot> future)
    {
        if (future.Count != 0) return null;
        var dated = current.Concat(historical).OrderByDescending(item => item.ExpiryTimeUtc).ToArray();
        if (dated.Length == 0 || dated.Any(item => !string.IsNullOrWhiteSpace(item.DeferredItemReplacementProductId))) return null;
        if (dated.Length > 1 && linkedToken is null) return null;
        if (dated.Length > 1 && dated[0].ExpiryTimeUtc == dated[1].ExpiryTimeUtc) return null;
        return dated[0];
    }

    private static GooglePlaySubscriptionLifecycleState? MapLifecycleState(string? state) => state switch
    {
        "SUBSCRIPTION_STATE_ACTIVE" => GooglePlaySubscriptionLifecycleState.Active,
        "SUBSCRIPTION_STATE_IN_GRACE_PERIOD" => GooglePlaySubscriptionLifecycleState.InGracePeriod,
        "SUBSCRIPTION_STATE_CANCELED" => GooglePlaySubscriptionLifecycleState.Canceled,
        "SUBSCRIPTION_STATE_ON_HOLD" => GooglePlaySubscriptionLifecycleState.OnHold,
        "SUBSCRIPTION_STATE_PAUSED" => GooglePlaySubscriptionLifecycleState.Paused,
        "SUBSCRIPTION_STATE_EXPIRED" => GooglePlaySubscriptionLifecycleState.Expired,
        _ => null
    };

    private static GooglePlayPurchaseVerificationResult Result(GooglePlayPurchaseVerificationResultCode code) => new(code);

    private static bool IsAllowedTestPurchase(GooglePlayBillingOptions options, Guid userId) =>
        options.Enabled
        && options.TestPurchasesEnabled
        && options.AllowedTestPurchaseUserIds.Any(value => Guid.TryParse(value, out var allowedUserId) && allowedUserId == userId);
}
