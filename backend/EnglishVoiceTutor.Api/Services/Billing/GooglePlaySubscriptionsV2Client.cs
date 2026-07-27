using Google;
using Google.Apis.AndroidPublisher.v3;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlaySubscriptionsV2Client(IGooglePlayAndroidPublisherServiceFactory serviceFactory) : IGooglePlaySubscriptionsV2Client
{
    public async Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken)
    {
        try
        {
            using var service = await serviceFactory.CreateAsync(cancellationToken);
            var response = await service.Purchases.Subscriptionsv2.Get(packageName, purchaseToken).ExecuteAsync(cancellationToken);
            if (response is null) return null;
            var lineItems = response.LineItems?
                .Select(item => new GooglePlaySubscriptionLineItemSnapshot(item.ProductId, NormalizeTimestamp(item.ExpiryTimeDateTimeOffset)))
                .ToArray()
                ?? [];
            return new GooglePlaySubscriptionV2Snapshot(
                response.SubscriptionState,
                NormalizeTimestamp(response.StartTimeDateTimeOffset),
                lineItems,
                MapAcknowledgementState(response.AcknowledgementState),
                response.TestPurchase is not null,
                !string.IsNullOrWhiteSpace(response.LinkedPurchaseToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.BadRequest)
        {
            throw new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.InvalidPurchase);
        }
        catch (GoogleApiException)
        {
            throw new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable);
        }
        catch (HttpRequestException)
        {
            throw new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable);
        }
    }

    private static DateTimeOffset? NormalizeTimestamp(DateTimeOffset? value) => value?.ToUniversalTime();

    private static GooglePlayPurchaseAcknowledgementState? MapAcknowledgementState(string? value) => value switch
    {
        "ACKNOWLEDGEMENT_STATE_PENDING" => GooglePlayPurchaseAcknowledgementState.Pending,
        "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED" => GooglePlayPurchaseAcknowledgementState.Acknowledged,
        _ => null
    };
}
