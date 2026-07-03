using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services.Auth;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BackendCheckoutSessionClient
{
    private const string RequiresLoginMessage = "Sign in is required to start checkout.";
    private const string CheckoutRequestUnavailableMessage = "Backend checkout-session POST is unavailable.";
    private const string CheckoutRequestTimedOutMessage = "Backend checkout-session POST timed out.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AuthBackendService authBackendService = new();

    public async Task<BackendCheckoutSessionClientResult> CreateAsync(
        string? backendBaseUrl,
        CancellationToken cancellationToken = default)
    {
        authBackendService.SetBackendBaseUrl(backendBaseUrl);
        if (!await authBackendService.HasStoredSessionAsync(cancellationToken))
        {
            return BackendCheckoutSessionClientResult.Failure(RequiresLoginMessage, requiresLogin: true);
        }

        using var httpClient = CreateHttpClient();
        try
        {
            var endpointUri = BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, BackendConstants.MeBillingCheckoutSessionEndpoint);
            using var response = await AuthenticatedRequestHelper.SendWithRefreshRetryAsync(
                httpClient,
                _ => new HttpRequestMessage(HttpMethod.Post, endpointUri)
                {
                    Content = JsonContent.Create(
                        new BackendCheckoutSessionRequest
                        {
                            PlanId = BackendConstants.BackendCheckoutPremiumPlanId
                        },
                        options: JsonOptions)
                },
                authBackendService,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return BackendCheckoutSessionClientResult.Failure(
                    $"Backend checkout-session POST {BackendConstants.MeBillingCheckoutSessionEndpoint} failed with HTTP {(int)response.StatusCode}.");
            }

            var checkoutResponse = await response.Content.ReadFromJsonAsync<BackendCheckoutSessionResponse>(JsonOptions, cancellationToken);
            return checkoutResponse is null
                ? BackendCheckoutSessionClientResult.Failure($"Backend checkout-session POST {BackendConstants.MeBillingCheckoutSessionEndpoint} returned an empty response.")
                : BackendCheckoutSessionClientResult.Success(checkoutResponse);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendCheckoutSessionClientResult.Failure(CheckoutRequestTimedOutMessage);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendCheckoutSessionClientResult.Failure(CheckoutRequestUnavailableMessage);
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(BackendConstants.BackendCheckoutSessionTimeoutSeconds)
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
