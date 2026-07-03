using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services.Auth;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BackendTrialClaimClient
{
    private const string RequiresLoginMessage = "Sign in is required to claim trial.";
    private const string ClaimRequestUnavailableMessage = "Backend trial claim POST is unavailable.";
    private const string ClaimRequestTimedOutMessage = "Backend trial claim POST timed out.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AuthBackendService authBackendService = new();

    public async Task<BackendTrialClaimClientResult> ClaimAsync(
        string? backendBaseUrl,
        CancellationToken cancellationToken = default)
    {
        authBackendService.SetBackendBaseUrl(backendBaseUrl);
        if (!await authBackendService.HasStoredSessionAsync(cancellationToken))
        {
            return BackendTrialClaimClientResult.Failure(RequiresLoginMessage, requiresLogin: true);
        }

        using var httpClient = CreateHttpClient();
        try
        {
            var endpointUri = BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, BackendConstants.MeTrialClaimEndpoint);
            using var response = await AuthenticatedRequestHelper.SendWithRefreshRetryAsync(
                httpClient,
                _ => new HttpRequestMessage(HttpMethod.Post, endpointUri),
                authBackendService,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return BackendTrialClaimClientResult.Failure(
                    $"Backend trial claim POST {BackendConstants.MeTrialClaimEndpoint} failed with HTTP {(int)response.StatusCode}.");
            }

            var claimResponse = await response.Content.ReadFromJsonAsync<BackendTrialClaimResponse>(JsonOptions, cancellationToken);
            return claimResponse is null
                ? BackendTrialClaimClientResult.Failure($"Backend trial claim POST {BackendConstants.MeTrialClaimEndpoint} returned an empty response.")
                : BackendTrialClaimClientResult.Success(claimResponse);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendTrialClaimClientResult.Failure(ClaimRequestTimedOutMessage);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendTrialClaimClientResult.Failure(ClaimRequestUnavailableMessage);
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(BackendConstants.BackendTrialClaimTimeoutSeconds)
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
