using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed record BackendDiagnosticsResult(
    bool IsBackendHealthy,
    BackendHealthResponse? BackendHealth,
    bool IsDatabaseHealthy,
    DatabaseHealthResponse? DatabaseHealth,
    string? DatabaseError,
    HttpStatusCode? BackendStatusCode,
    HttpStatusCode? DatabaseStatusCode,
    string ErrorCategory);

public sealed class BackendDiagnosticsService
{
    private const string HealthyStatus = "Healthy";
    private const int MaxSafeErrorLength = 160;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BackendDiagnosticsResult> CheckAsync(string? backendBaseUrl, CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();
        var backendHealth = await GetHealthAsync<BackendHealthResponse>(httpClient, backendBaseUrl, BackendConstants.HealthEndpoint, cancellationToken);
        var isBackendHealthy = backendHealth.Payload is not null && IsHealthyStatus(backendHealth.Payload.Status);

        if (!isBackendHealthy)
        {
            return new BackendDiagnosticsResult(
                false,
                backendHealth.Payload,
                false,
                null,
                null,
                backendHealth.StatusCode,
                null,
                backendHealth.ErrorCategory);
        }

        var databaseHealth = await GetHealthAsync<DatabaseHealthResponse>(httpClient, backendBaseUrl, BackendConstants.DatabaseHealthEndpoint, cancellationToken);
        var isDatabaseHealthy = databaseHealth.Payload is not null
            && IsHealthyStatus(databaseHealth.Payload.Status)
            && databaseHealth.Payload.CanConnect;

        return new BackendDiagnosticsResult(
            true,
            backendHealth.Payload,
            isDatabaseHealthy,
            databaseHealth.Payload,
            isDatabaseHealthy ? null : SanitizeError(databaseHealth.Payload?.Error),
            backendHealth.StatusCode,
            databaseHealth.StatusCode,
            isDatabaseHealthy ? "none" : databaseHealth.ErrorCategory);
    }

    private static async Task<HealthRequestResult<T>> GetHealthAsync<T>(HttpClient httpClient, string? backendBaseUrl, string endpointPath, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpointPath), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new HealthRequestResult<T>(default, response.StatusCode, (int)response.StatusCode >= 500 ? "server_error" : "http_error");
            }

            var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return payload is null
                ? new HealthRequestResult<T>(default, response.StatusCode, "unexpected_response")
                : new HealthRequestResult<T>(payload, response.StatusCode, "none");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new HealthRequestResult<T>(default, null, "timeout");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return new HealthRequestResult<T>(default, null, exception is JsonException ? "unexpected_response" : "network");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(BackendConstants.BackendHealthTimeoutSeconds)
        };

        httpClient.DefaultRequestHeaders.Add(
            BackendConstants.NgrokSkipBrowserWarningHeaderName,
            BackendConstants.NgrokSkipBrowserWarningHeaderValue);
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
            BackendConstants.BackendUserAgentProductName,
            BackendConstants.BackendUserAgentVersion));

        return httpClient;
    }

    private static bool IsHealthyStatus(string? status)
    {
        return string.Equals(status, HealthyStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static string? SanitizeError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return null;
        }

        var safeError = error.ReplaceLineEndings(" ").Trim();
        return safeError.Length <= MaxSafeErrorLength
            ? safeError
            : string.Concat(safeError.AsSpan(0, MaxSafeErrorLength), "...");
    }

    private sealed record HealthRequestResult<T>(T? Payload, HttpStatusCode? StatusCode, string ErrorCategory);
}
