using Google;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using System.Globalization;
using System.Net;

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
                .Select(item => new GooglePlaySubscriptionLineItemSnapshot(
                    item.ProductId,
                    NormalizeTimestamp(item.ExpiryTimeDateTimeOffset),
                    item.DeferredItemReplacement?.ProductId)
                {
                    HasDeferredItemRemoval = item.DeferredItemRemoval is not null,
                    HasAutoRenewingPlan = item.AutoRenewingPlan is not null,
                    AutoRenewEnabled = item.AutoRenewingPlan?.AutoRenewEnabled,
                    HasPrepaidPlan = item.PrepaidPlan is not null,
                    BasePlanId = item.OfferDetails?.BasePlanId,
                    OfferId = item.OfferDetails?.OfferId,
                    OfferPhase = MapOfferPhase(item.OfferPhase),
                    HasSignupPromotion = item.SignupPromotion is not null,
                    HasItemReplacement = item.ItemReplacement is not null,
                    HasLatestSuccessfulOrderId = !string.IsNullOrWhiteSpace(item.LatestSuccessfulOrderId)
                })
                .ToArray()
                ?? [];
            return new GooglePlaySubscriptionV2Snapshot(
                response.SubscriptionState,
                NormalizeTimestamp(response.StartTimeDateTimeOffset),
                lineItems,
                MapAcknowledgementState(response.AcknowledgementState),
                response.TestPurchase is not null,
                string.IsNullOrWhiteSpace(response.LinkedPurchaseToken) ? null : response.LinkedPurchaseToken,
                response.ETag);
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

    public async Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken)
    {
        try
        {
            using var service = await serviceFactory.CreateAsync(cancellationToken);
            await service.Purchases.Subscriptions.Acknowledge(
                    new SubscriptionPurchasesAcknowledgeRequest(),
                    packageName,
                    productId,
                    purchaseToken)
                .ExecuteAsync(cancellationToken);
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

    public async Task<GooglePlaySubscriptionDeferResponseSnapshot> DeferAsync(
        string packageName,
        string purchaseToken,
        string etag,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        try
        {
            using var service = await serviceFactory.CreateAsync(cancellationToken);
            var response = await service.Purchases.Subscriptionsv2.Defer(
                    new DeferSubscriptionPurchaseRequest
                    {
                        DeferralContext = new DeferralContext
                        {
                            ETag = etag,
                            DeferDuration = FormatDuration(duration),
                            ValidateOnly = false
                        }
                    },
                    packageName,
                    purchaseToken)
                .ExecuteAsync(cancellationToken);
            var items = response?.ItemExpiryTimeDetails?
                .Select(item => new GooglePlaySubscriptionDeferItemSnapshot(item.ProductId, NormalizeTimestamp(item.ExpiryTimeDateTimeOffset)))
                .ToArray()
                ?? [];
            return new GooglePlaySubscriptionDeferResponseSnapshot(items);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.Conflict)
        {
            throw new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.PreconditionFailed);
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            throw new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.InvalidPurchase);
        }
        catch (GoogleApiException)
        {
            throw new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.ProviderOutcomeUnknown);
        }
        catch (HttpRequestException)
        {
            throw new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.ProviderOutcomeUnknown);
        }
    }

    private static DateTimeOffset? NormalizeTimestamp(DateTimeOffset? value) => value?.ToUniversalTime();

    private static GooglePlaySubscriptionOfferPhase? MapOfferPhase(OfferPhase? value)
    {
        if (value is null) return null;
        var populated = new[] { value.BasePrice is not null, value.FreeTrial is not null, value.IntroductoryPrice is not null, value.ProrationPeriod is not null }.Count(item => item);
        if (populated != 1) return GooglePlaySubscriptionOfferPhase.Ambiguous;
        if (value.BasePrice is not null) return GooglePlaySubscriptionOfferPhase.BasePrice;
        if (value.FreeTrial is not null) return GooglePlaySubscriptionOfferPhase.FreeTrial;
        if (value.IntroductoryPrice is not null) return GooglePlaySubscriptionOfferPhase.IntroductoryPrice;
        return GooglePlaySubscriptionOfferPhase.Proration;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        var seconds = duration.Ticks / TimeSpan.TicksPerSecond;
        var fractionalTicks = duration.Ticks % TimeSpan.TicksPerSecond;
        return fractionalTicks == 0
            ? seconds.ToString(CultureInfo.InvariantCulture) + "s"
            : seconds.ToString(CultureInfo.InvariantCulture) + "." + fractionalTicks.ToString("0000000", CultureInfo.InvariantCulture).TrimEnd('0') + "s";
    }

    private static GooglePlayPurchaseAcknowledgementState? MapAcknowledgementState(string? value) => value switch
    {
        "ACKNOWLEDGEMENT_STATE_PENDING" => GooglePlayPurchaseAcknowledgementState.Pending,
        "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED" => GooglePlayPurchaseAcknowledgementState.Acknowledged,
        _ => null
    };
}
