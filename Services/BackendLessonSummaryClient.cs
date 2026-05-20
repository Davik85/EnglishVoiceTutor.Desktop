using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BackendLessonSummaryClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BackendLessonSummaryClientResult> UpsertAsync(
        string? backendBaseUrl,
        Guid sessionId,
        UpsertBackendLessonSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpClient = CreateHttpClient();

        try
        {
            var endpoint = string.Format(BackendConstants.DevLessonSessionSummaryEndpointTemplate, sessionId);
            using var response = await httpClient.PutAsJsonAsync(
                BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpoint),
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BackendLessonSummaryClientResult.Failure($"Backend lesson summary PUT failed with HTTP {(int)response.StatusCode}.");
            }

            var summary = await response.Content.ReadFromJsonAsync<BackendLessonSummaryResponse>(JsonOptions, cancellationToken);
            return summary is null
                ? BackendLessonSummaryClientResult.Failure("Backend lesson summary PUT returned an empty response.")
                : BackendLessonSummaryClientResult.Success(summary);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendLessonSummaryClientResult.Failure("Backend lesson summary PUT timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendLessonSummaryClientResult.Failure("Backend lesson summary PUT is unavailable.");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(BackendConstants.LessonSummaryRequestTimeoutSeconds)
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
