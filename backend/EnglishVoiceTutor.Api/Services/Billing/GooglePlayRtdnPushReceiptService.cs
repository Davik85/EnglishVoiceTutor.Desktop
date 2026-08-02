using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public enum GooglePlayRtdnPushReceiptStatus { NoContent, Unauthorized, BadRequest, TemporarilyUnavailable }
public sealed record GooglePlayRtdnPushReceiptResult(GooglePlayRtdnPushReceiptStatus Status);
internal enum GooglePlayRtdnParseStatus { Valid, Invalid, PendingRefundRetry }
internal sealed record GooglePlayRtdnParseResult(GooglePlayRtdnParseStatus Status, GooglePlayRtdnReceipt? Receipt);

public interface IGooglePlayRtdnPushReceiptService
{
    Task<GooglePlayRtdnPushReceiptResult> ReceiveAsync(IReadOnlyList<string?> authorizationValues, string body, CancellationToken cancellationToken);
}

public sealed class GooglePlayRtdnPushReceiptService(
    IOptions<GooglePlayRtdnOptions> rtdnOptionsAccessor,
    IOptions<GooglePlayBillingOptions> billingOptionsAccessor,
    IGooglePlayPubSubOidcTokenValidator tokenValidator,
    GooglePlayRtdnEventPersistenceService persistenceService,
    IGooglePlayPurchaseTokenFingerprintService fingerprintService) : IGooglePlayRtdnPushReceiptService
{
    private const string Provider = "google_play";

    public async Task<GooglePlayRtdnPushReceiptResult> ReceiveAsync(IReadOnlyList<string?> authorizationValues, string body, CancellationToken cancellationToken)
    {
        var bearerToken = GetSingleBearerToken(authorizationValues);
        if (bearerToken is null || !(await tokenValidator.ValidateAsync(bearerToken, cancellationToken)).IsValid)
        {
            return new(GooglePlayRtdnPushReceiptStatus.Unauthorized);
        }

        var parsed = Parse(body, rtdnOptionsAccessor.Value, billingOptionsAccessor.Value, fingerprintService);
        if (parsed.Status == GooglePlayRtdnParseStatus.Invalid) return new(GooglePlayRtdnPushReceiptStatus.BadRequest);
        if (parsed.Status == GooglePlayRtdnParseStatus.PendingRefundRetry) return new(GooglePlayRtdnPushReceiptStatus.TemporarilyUnavailable);

        var persisted = await persistenceService.RecordReceiptAsync(parsed.Receipt!, cancellationToken);
        return persisted.Code switch
        {
            GooglePlayRtdnReceiptResultCode.Received or GooglePlayRtdnReceiptResultCode.Duplicate => new(GooglePlayRtdnPushReceiptStatus.NoContent),
            GooglePlayRtdnReceiptResultCode.InvalidInput => new(GooglePlayRtdnPushReceiptStatus.BadRequest),
            _ => new(GooglePlayRtdnPushReceiptStatus.TemporarilyUnavailable)
        };
    }

    private static string? GetSingleBearerToken(IReadOnlyList<string?> values)
    {
        if (values.Count != 1) return null;
        var value = values[0];
        if (value is null) return null;
        const string prefix = "Bearer ";
        if (!value.StartsWith(prefix, StringComparison.Ordinal) || value.Length == prefix.Length || value[prefix.Length..].Any(char.IsWhiteSpace)) return null;
        return value[prefix.Length..];
    }

    private static GooglePlayRtdnParseResult Parse(string body, GooglePlayRtdnOptions rtdnOptions, GooglePlayBillingOptions billingOptions, IGooglePlayPurchaseTokenFingerprintService fingerprintService)
    {
        try
        {
            using var envelope = JsonDocument.Parse(body);
            var root = envelope.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !TryString(root, "subscription", out var subscription) ||
                !string.Equals(subscription, rtdnOptions.ExpectedPubSubSubscription, StringComparison.Ordinal) ||
                !TryObject(root, "message", out var message) ||
                !TryString(message, "messageId", out var messageId) || messageId.Length > EntityConstants.Lengths.GooglePlayRtdnMessageIdMaxLength ||
                !TryString(message, "data", out var data)) return Invalid();

            byte[] bytes;
            try { bytes = Convert.FromBase64String(data); }
            catch (FormatException) { return Invalid(); }

            using var notification = JsonDocument.Parse(bytes);
            var notificationRoot = notification.RootElement;
            if (notificationRoot.ValueKind != JsonValueKind.Object ||
                !TryString(notificationRoot, "packageName", out var packageName) ||
                !string.Equals(packageName, billingOptions.PackageName, StringComparison.Ordinal)) return Invalid();

            var kinds = new[]
            {
                ("subscription_notification", "subscriptionNotification"),
                ("test_notification", "testNotification"),
                ("voided_purchase_notification", "voidedPurchaseNotification"),
                ("one_time_product_notification", "oneTimeProductNotification"),
                ("pending_refund_review_notification", "pendingRefundReviewNotification")
            }.Where(item => TryObject(notificationRoot, item.Item2, out _)).ToArray();
            if (kinds.Length != 1) return Invalid();

            var kind = kinds[0];
            TryObject(notificationRoot, kind.Item2, out var payload);
            string? fingerprint = null;
            if (kind.Item2 is "subscriptionNotification" or "oneTimeProductNotification" or "voidedPurchaseNotification")
            {
                if (!TryString(payload, "purchaseToken", out var purchaseToken)) return Invalid();
                fingerprint = fingerprintService.CreateFingerprint(purchaseToken);
            }
            else if (kind.Item2 == "pendingRefundReviewNotification")
            {
                if (!TryString(payload, "pendingRefundToken", out _)) return Invalid();
                return new(GooglePlayRtdnParseStatus.PendingRefundRetry, null);
            }

            DateTimeOffset? publishedAtUtc = null;
            if (TryString(message, "publishTime", out var publishTime) && DateTimeOffset.TryParse(publishTime, out var parsedPublishedAt))
            {
                publishedAtUtc = parsedPublishedAt.ToUniversalTime();
            }

            return new(GooglePlayRtdnParseStatus.Valid, new GooglePlayRtdnReceipt(Provider, messageId, subscription, packageName, kind.Item1, fingerprint, publishedAtUtc));
        }
        catch (JsonException) { return Invalid(); }
        catch (ArgumentException) { return Invalid(); }
    }

    private static GooglePlayRtdnParseResult Invalid() => new(GooglePlayRtdnParseStatus.Invalid, null);

    private static bool TryObject(JsonElement element, string name, out JsonElement value) => element.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Object;
    private static bool TryString(JsonElement element, string name, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String) return false;
        value = property.GetString()?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }
}
