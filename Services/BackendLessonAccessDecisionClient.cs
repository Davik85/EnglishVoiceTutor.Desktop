using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services.Auth;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BackendLessonAccessDecisionClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AuthSessionStorageService authSessionStorageService = new();

    public async Task<BackendLessonAccessDecisionClientResult> GetAsync(
        string? backendBaseUrl,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();

        try
        {
            var session = await authSessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
            var hasAccessToken = !string.IsNullOrWhiteSpace(session?.AccessToken);
            var endpoint = hasAccessToken
                ? BackendConstants.MeLessonAccessEndpoint
                : BackendConstants.DevLessonAccessEndpoint;
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpoint));

            if (hasAccessToken)
            {
                AuthenticatedRequestHelper.AddBearerTokenIfPresent(httpRequest, session?.AccessToken);
            }

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return BackendLessonAccessDecisionClientResult.Failure(
                    $"Backend lesson access GET {endpoint} failed with HTTP {(int)response.StatusCode}.",
                    response.StatusCode);
            }

            var lessonAccess = await response.Content.ReadFromJsonAsync<BackendLessonAccessDecisionResponse>(JsonOptions, cancellationToken);
            return lessonAccess is null
                ? BackendLessonAccessDecisionClientResult.Failure($"Backend lesson access GET {endpoint} returned an empty response.")
                : BackendLessonAccessDecisionClientResult.Success(lessonAccess);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendLessonAccessDecisionClientResult.Failure("Backend lesson access GET timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendLessonAccessDecisionClientResult.Failure("Backend lesson access GET is unavailable.");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(BackendConstants.BackendLessonAccessTimeoutSeconds)
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
