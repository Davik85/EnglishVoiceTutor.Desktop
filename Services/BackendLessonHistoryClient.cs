using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services.Auth;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BackendLessonHistoryClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AuthSessionStorageService authSessionStorageService = new();

    public async Task<BackendLessonHistoryClientResult> GetHistoryAsync(
        string? backendBaseUrl,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, BackendConstants.DevLessonHistoryEndpoint));
            var session = await authSessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(session?.AccessToken))
            {
                return BackendLessonHistoryClientResult.Failure("Backend lesson history GET skipped because no authenticated session is available.");
            }

            AuthenticatedRequestHelper.AddBearerTokenIfPresent(request, session.AccessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BackendLessonHistoryClientResult.Failure($"Backend lesson history GET failed with HTTP {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<BackendLessonHistoryListResponse>(JsonOptions, cancellationToken);
            if (payload is null)
            {
                return BackendLessonHistoryClientResult.Failure("Backend lesson history GET returned an empty response.");
            }

            return BackendLessonHistoryClientResult.Success(payload.Items ?? []);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendLessonHistoryClientResult.Failure("Backend lesson history GET timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendLessonHistoryClientResult.Failure("Backend lesson history GET is unavailable.");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(BackendConstants.LessonHistoryRequestTimeoutSeconds)
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
