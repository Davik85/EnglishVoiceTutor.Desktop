using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services.Auth;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BackendCancelSubscriptionClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AuthSessionStorageService authSessionStorageService = new();

    public async Task<BackendCancelSubscriptionClientResult> CancelAsync(string? backendBaseUrl, CancellationToken cancellationToken = default)
    {
        var session = await authSessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(session?.AccessToken))
        {
            return BackendCancelSubscriptionClientResult.Failure("Sign in is required to cancel renewal.", requiresLogin: true);
        }

        using var httpClient = CreateHttpClient();
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, BackendConstants.MeBillingSubscriptionCancelEndpoint));
            AuthenticatedRequestHelper.AddBearerTokenIfPresent(httpRequest, session.AccessToken);
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return BackendCancelSubscriptionClientResult.Failure($"Backend subscription cancellation POST {BackendConstants.MeBillingSubscriptionCancelEndpoint} failed with HTTP {(int)response.StatusCode}.");
            }

            var cancelResponse = await response.Content.ReadFromJsonAsync<BackendCancelSubscriptionResponse>(JsonOptions, cancellationToken);
            return cancelResponse is null
                ? BackendCancelSubscriptionClientResult.Failure($"Backend subscription cancellation POST {BackendConstants.MeBillingSubscriptionCancelEndpoint} returned an empty response.")
                : BackendCancelSubscriptionClientResult.Success(cancelResponse);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendCancelSubscriptionClientResult.Failure("Backend subscription cancellation is unavailable.");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(BackendConstants.BackendCheckoutSessionTimeoutSeconds) };
        httpClient.DefaultRequestHeaders.Add(BackendConstants.NgrokSkipBrowserWarningHeaderName, BackendConstants.NgrokSkipBrowserWarningHeaderValue);
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(BackendConstants.BackendUserAgentProductName, BackendConstants.BackendUserAgentVersion));
        return httpClient;
    }
}
