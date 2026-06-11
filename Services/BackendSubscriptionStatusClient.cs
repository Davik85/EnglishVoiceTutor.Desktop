using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services.Auth;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BackendSubscriptionStatusClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AuthSessionStorageService authSessionStorageService = new();

    public async Task<BackendSubscriptionStatusClientResult> GetAsync(
        string? backendBaseUrl,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();
        var endpoint = BackendConstants.DevSubscriptionStatusEndpoint;
        var endpointUri = BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpoint);

        try
        {
            var session = await authSessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
            var hasAccessToken = !string.IsNullOrWhiteSpace(session?.AccessToken);
            endpoint = hasAccessToken
                ? BackendConstants.MeSubscriptionStatusEndpoint
                : BackendConstants.DevSubscriptionStatusEndpoint;
            endpointUri = BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpoint);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpointUri);

            if (hasAccessToken)
            {
                AuthenticatedRequestHelper.AddBearerTokenIfPresent(httpRequest, session?.AccessToken);
            }

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            await RecordSubscriptionDiagnosticsAsync(endpoint, endpointUri, backendBaseUrl, response, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return BackendSubscriptionStatusClientResult.Failure(
                    $"Backend subscription status GET {endpoint} failed with HTTP {(int)response.StatusCode}.",
                    response.StatusCode);
            }

            var subscriptionStatus = await response.Content.ReadFromJsonAsync<BackendSubscriptionStatusResponse>(JsonOptions, cancellationToken);
            return subscriptionStatus is null
                ? BackendSubscriptionStatusClientResult.Failure($"Backend subscription status GET {endpoint} returned an empty response.")
                : BackendSubscriptionStatusClientResult.Success(subscriptionStatus);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            await BackendRequestDiagnosticsService.RecordAsync(GetSubscriptionRequestName(endpoint), HttpMethod.Get, endpointUri, backendBaseUrl, exception: exception, cancellationToken: CancellationToken.None);
            return BackendSubscriptionStatusClientResult.Failure("Backend subscription status GET timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            await BackendRequestDiagnosticsService.RecordAsync(GetSubscriptionRequestName(endpoint), HttpMethod.Get, endpointUri, backendBaseUrl, exception: exception, cancellationToken: CancellationToken.None);
            return BackendSubscriptionStatusClientResult.Failure("Backend subscription status GET is unavailable.");
        }
    }

    private static async Task RecordSubscriptionDiagnosticsAsync(
        string endpointPath,
        Uri endpointUri,
        string? backendBaseUrl,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var safeBodySnippet = response.IsSuccessStatusCode
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);
        await BackendRequestDiagnosticsService.RecordAsync(
            GetSubscriptionRequestName(endpointPath),
            HttpMethod.Get,
            endpointUri,
            backendBaseUrl,
            response.StatusCode,
            responseBodySnippet: safeBodySnippet,
            cancellationToken: cancellationToken);
    }

    private static string GetSubscriptionRequestName(string endpointPath)
    {
        return endpointPath == BackendConstants.MeSubscriptionStatusEndpoint
            ? "subscription_status"
            : "dev_subscription_status";
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(BackendConstants.BackendSubscriptionStatusTimeoutSeconds)
        };

        httpClient.DefaultRequestHeaders.Add(
            BackendConstants.NgrokSkipBrowserWarningHeaderName,
            BackendConstants.NgrokSkipBrowserWarningHeaderValue);
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
            BackendConstants.BackendUserAgentProductName,
            BackendConstants.BackendUserAgentVersion));

        return httpClient;
    }
}
