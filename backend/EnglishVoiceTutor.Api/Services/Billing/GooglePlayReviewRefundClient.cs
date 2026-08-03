using System.Net;
using System.Net.Http.Json;

namespace EnglishVoiceTutor.Api.Services.Billing;

public enum GooglePlayReviewRefundResultCode { Processed, PermanentFailure, RetryableFailure }
public sealed record GooglePlayReviewRefundResult(GooglePlayReviewRefundResultCode Code) { public override string ToString() => Code.ToString(); }
public interface IGooglePlayReviewRefundClient { Task<GooglePlayReviewRefundResult> ReviewAsync(string packageName, string orderId, string pendingRefundToken, bool sampleContentProvided, string refundPreference, CancellationToken cancellationToken); }
public sealed class GooglePlayReviewRefundClient(IGooglePlayAndroidPublisherServiceFactory factory) : IGooglePlayReviewRefundClient
{
    public async Task<GooglePlayReviewRefundResult> ReviewAsync(string packageName, string orderId, string pendingRefundToken, bool sampleContentProvided, string refundPreference, CancellationToken cancellationToken)
    {
        try
        {
            using var service = await factory.CreateAsync(cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{Uri.EscapeDataString(packageName)}/orders/{Uri.EscapeDataString(orderId)}:reviewrefund") { Content = JsonContent.Create(new { pendingRefundToken, sampleContentProvided, refundPreference }) };
            using var response = await service.HttpClient.SendAsync(request, cancellationToken);
            if ((int)response.StatusCode is >= 200 and < 300) return new(GooglePlayReviewRefundResultCode.Processed);
            return response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound ? new(GooglePlayReviewRefundResultCode.PermanentFailure) : new(GooglePlayReviewRefundResultCode.RetryableFailure);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return new(GooglePlayReviewRefundResultCode.RetryableFailure); }
    }
}
