using Google;
using Google.Apis.AndroidPublisher.v3;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlaySubscriptionsV2Client(AndroidPublisherService service) : IGooglePlaySubscriptionsV2Client
{
    public async Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken)
    {
        try
        {
            var response = await service.Purchases.Subscriptionsv2.Get(packageName, purchaseToken).ExecuteAsync(cancellationToken);
            if (response is null) return null;
            var productIds = response.LineItems?
                .Select(item => item.ProductId)
                .Where(productId => !string.IsNullOrWhiteSpace(productId))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .ToArray()
                ?? [];
            return new GooglePlaySubscriptionV2Snapshot(
                response.SubscriptionState,
                productIds,
                response.AcknowledgementState,
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
}
