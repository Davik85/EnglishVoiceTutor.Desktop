using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BackendUserSettingsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BackendUserSettingsClientResult<BackendUserSettingsResponse>> GetAsync(
        string? backendBaseUrl,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();

        try
        {
            using var response = await httpClient.GetAsync(
                BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, BackendConstants.DevUserSettingsEndpoint),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure($"Backend settings GET failed with HTTP {(int)response.StatusCode}.");
            }

            var settings = await response.Content.ReadFromJsonAsync<BackendUserSettingsResponse>(JsonOptions, cancellationToken);
            return settings is null
                ? BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure("Backend settings GET returned an empty response.")
                : BackendUserSettingsClientResult<BackendUserSettingsResponse>.Success(settings);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure("Backend settings GET timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure("Backend settings GET is unavailable.");
        }
    }

    public async Task<BackendUserSettingsClientResult<BackendUserSettingsResponse>> UpdateAsync(
        string? backendBaseUrl,
        UpdateBackendUserSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpClient = CreateHttpClient();

        try
        {
            using var response = await httpClient.PutAsJsonAsync(
                BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, BackendConstants.DevUserSettingsEndpoint),
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure($"Backend settings PUT failed with HTTP {(int)response.StatusCode}.");
            }

            var settings = await response.Content.ReadFromJsonAsync<BackendUserSettingsResponse>(JsonOptions, cancellationToken);
            return settings is null
                ? BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure("Backend settings PUT returned an empty response.")
                : BackendUserSettingsClientResult<BackendUserSettingsResponse>.Success(settings);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure("Backend settings PUT timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendUserSettingsClientResult<BackendUserSettingsResponse>.Failure("Backend settings PUT is unavailable.");
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
