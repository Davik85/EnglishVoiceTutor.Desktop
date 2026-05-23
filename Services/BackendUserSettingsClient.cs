using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BackendUserSettingsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<BackendUserSettingsClientResult<BackendUserSettingsResponse>> GetDevelopmentSettingsAsync(
        string? backendBaseUrl,
        CancellationToken cancellationToken = default)
    {
        return GetAsync(backendBaseUrl, BackendConstants.DevUserSettingsEndpoint, accessToken: null, cancellationToken);
    }

    public Task<BackendUserSettingsClientResult<BackendUserSettingsResponse>> GetAuthenticatedSettingsAsync(
        string? backendBaseUrl,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return GetAsync(backendBaseUrl, BackendConstants.MeSettingsEndpoint, accessToken, cancellationToken);
    }

    public Task<BackendUserSettingsClientResult<BackendUserSettingsResponse>> UpdateDevelopmentSettingsAsync(
        string? backendBaseUrl,
        UpdateBackendUserSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        return UpdateAsync(backendBaseUrl, BackendConstants.DevUserSettingsEndpoint, accessToken: null, request, cancellationToken);
    }

    public Task<BackendUserSettingsClientResult<BackendUserSettingsResponse>> UpdateAuthenticatedSettingsAsync(
        string? backendBaseUrl,
        string accessToken,
        UpdateBackendUserSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        return UpdateAsync(backendBaseUrl, BackendConstants.MeSettingsEndpoint, accessToken, request, cancellationToken);
    }

    private async Task<BackendUserSettingsClientResult<BackendUserSettingsResponse>> GetAsync(
        string? backendBaseUrl,
        string endpointPath,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        using var httpClient = CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpointPath));
        AddBearerToken(request, accessToken);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure(
                    BuildHttpFailureMessage("GET", endpointPath, response.StatusCode),
                    response.StatusCode);
            }

            var settings = await response.Content.ReadFromJsonAsync<BackendUserSettingsResponse>(JsonOptions, cancellationToken);
            return settings is null
                ? BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure($"Backend settings GET {endpointPath} returned an empty response.")
                : BackendUserSettingsClientResult<BackendUserSettingsResponse>.Success(settings);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure($"Backend settings GET {endpointPath} timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure($"Backend settings GET {endpointPath} is unavailable.");
        }
    }

    private async Task<BackendUserSettingsClientResult<BackendUserSettingsResponse>> UpdateAsync(
        string? backendBaseUrl,
        string endpointPath,
        string? accessToken,
        UpdateBackendUserSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpClient = CreateHttpClient();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpointPath))
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        AddBearerToken(httpRequest, accessToken);

        try
        {
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure(
                    BuildHttpFailureMessage("PUT", endpointPath, response.StatusCode),
                    response.StatusCode);
            }

            var settings = await response.Content.ReadFromJsonAsync<BackendUserSettingsResponse>(JsonOptions, cancellationToken);
            return settings is null
                ? BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure($"Backend settings PUT {endpointPath} returned an empty response.")
                : BackendUserSettingsClientResult<BackendUserSettingsResponse>.Success(settings);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure($"Backend settings PUT {endpointPath} timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure($"Backend settings PUT {endpointPath} is unavailable.");
        }
    }

    private static string BuildHttpFailureMessage(string method, string endpointPath, HttpStatusCode statusCode)
    {
        return $"Backend settings {method} {endpointPath} failed with HTTP {(int)statusCode}.";
    }

    private static void AddBearerToken(HttpRequestMessage request, string? accessToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(BackendConstants.BackendUserSettingsTimeoutSeconds)
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
