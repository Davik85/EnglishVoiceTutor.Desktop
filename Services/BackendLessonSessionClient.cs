using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services.Auth;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BackendLessonSessionClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AuthSessionStorageService authSessionStorageService = new();

    public async Task<BackendLessonSessionClientResult> StartAsync(
        string? backendBaseUrl,
        StartBackendLessonSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpClient = CreateHttpClient();

        try
        {
            var authSession = await authSessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
            var endpoint = string.IsNullOrWhiteSpace(authSession?.AccessToken)
                ? BackendConstants.DevLessonSessionsEndpoint
                : BackendConstants.MeLessonSessionsEndpoint;
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpoint))
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            AuthenticatedRequestHelper.AddBearerTokenIfPresent(httpRequest, authSession?.AccessToken);
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BackendLessonSessionClientResult.Failure($"Backend lesson session POST failed with HTTP {(int)response.StatusCode}.");
            }

            var lessonSession = await response.Content.ReadFromJsonAsync<BackendLessonSessionResponse>(JsonOptions, cancellationToken);
            return lessonSession is null
                ? BackendLessonSessionClientResult.Failure("Backend lesson session POST returned an empty response.")
                : BackendLessonSessionClientResult.Success(lessonSession);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendLessonSessionClientResult.Failure("Backend lesson session POST timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendLessonSessionClientResult.Failure("Backend lesson session POST is unavailable.");
        }
    }

    public async Task<BackendLessonSessionClientResult> FinishAsync(
        string? backendBaseUrl,
        Guid sessionId,
        FinishBackendLessonSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpClient = CreateHttpClient();

        try
        {
            var authSession = await authSessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
            var endpointTemplate = string.IsNullOrWhiteSpace(authSession?.AccessToken)
                ? BackendConstants.DevLessonSessionFinishEndpointTemplate
                : BackendConstants.MeLessonSessionFinishEndpointTemplate;
            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, string.Format(endpointTemplate, sessionId)))
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            AuthenticatedRequestHelper.AddBearerTokenIfPresent(httpRequest, authSession?.AccessToken);
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BackendLessonSessionClientResult.Failure($"Backend lesson session PUT failed with HTTP {(int)response.StatusCode}.");
            }

            var lessonSession = await response.Content.ReadFromJsonAsync<BackendLessonSessionResponse>(JsonOptions, cancellationToken);
            return lessonSession is null
                ? BackendLessonSessionClientResult.Failure("Backend lesson session PUT returned an empty response.")
                : BackendLessonSessionClientResult.Success(lessonSession);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendLessonSessionClientResult.Failure("Backend lesson session PUT timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendLessonSessionClientResult.Failure("Backend lesson session PUT is unavailable.");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(BackendConstants.LessonSessionRequestTimeoutSeconds)
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
