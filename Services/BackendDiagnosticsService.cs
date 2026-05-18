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
    string? DatabaseError);

public sealed class BackendDiagnosticsService
{
    private const string HealthyStatus = "Healthy";
    private const int MaxSafeErrorLength = 160;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BackendDiagnosticsResult> CheckAsync(string? backendBaseUrl, CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();
        var backendHealth = await GetHealthAsync<BackendHealthResponse>(httpClient, backendBaseUrl, BackendConstants.HealthEndpoint, cancellationToken);
        var isBackendHealthy = backendHealth is not null && IsHealthyStatus(backendHealth.Status);

        if (!isBackendHealthy)
        {
            return new BackendDiagnosticsResult(false, backendHealth, false, null, null);
        }

        var databaseHealth = await GetHealthAsync<DatabaseHealthResponse>(httpClient, backendBaseUrl, BackendConstants.DatabaseHealthEndpoint, cancellationToken);
        var isDatabaseHealthy = databaseHealth is not null
            && IsHealthyStatus(databaseHealth.Status)
            && databaseHealth.CanConnect;

        return new BackendDiagnosticsResult(
            true,
            backendHealth,
            isDatabaseHealthy,
            databaseHealth,
            isDatabaseHealthy ? null : SanitizeError(databaseHealth?.Error));
    }

    private static async Task<T?> GetHealthAsync<T>(HttpClient httpClient, string? backendBaseUrl, string endpointPath, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpointPath), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        }
        catch
        {
            return default;
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
}
