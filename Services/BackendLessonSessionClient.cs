using System.Net;
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
    private const string LessonAccessDeniedErrorCode = "lesson_access_denied";
    private const string ActiveLessonExistsErrorCode = "active_lesson_exists";
    private const string LessonSessionEndedElsewhereErrorCode = "lesson_session_ended_elsewhere";
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
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    var lessonAccessDenied = await TryReadLessonAccessDeniedResponseAsync(response, cancellationToken);
                    if (lessonAccessDenied is not null)
                    {
                        return BackendLessonSessionClientResult.LessonAccessDenied(lessonAccessDenied);
                    }
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var activeLessonBlocked = await TryReadActiveLessonExistsResponseAsync(response, cancellationToken);
                    if (activeLessonBlocked is not null)
                    {
                        return BackendLessonSessionClientResult.ActiveLessonBlocked(activeLessonBlocked);
                    }
                }

                return BackendLessonSessionClientResult.Failure($"Backend lesson session POST failed with HTTP {(int)response.StatusCode}.", backendWasReached: true);
            }

            var lessonSession = await response.Content.ReadFromJsonAsync<BackendLessonSessionResponse>(JsonOptions, cancellationToken);
            return lessonSession is null
                ? BackendLessonSessionClientResult.Failure("Backend lesson session POST returned an empty response.", backendWasReached: true)
                : BackendLessonSessionClientResult.Success(lessonSession);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendLessonSessionClientResult.Failure("Backend lesson session POST timed out.", isBackendReachabilityFailure: true);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendLessonSessionClientResult.Failure("Backend lesson session POST is unavailable.", isBackendReachabilityFailure: IsBackendReachabilityFailure(exception));
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
                if (await IsLessonSessionEndedElsewhereResponseAsync(response, cancellationToken))
                {
                    return BackendLessonSessionClientResult.LessonSessionEndedElsewhere("Backend lesson session ended elsewhere.");
                }

                return BackendLessonSessionClientResult.Failure($"Backend lesson session PUT failed with HTTP {(int)response.StatusCode}.", backendWasReached: true);
            }

            var lessonSession = await response.Content.ReadFromJsonAsync<BackendLessonSessionResponse>(JsonOptions, cancellationToken);
            return lessonSession is null
                ? BackendLessonSessionClientResult.Failure("Backend lesson session PUT returned an empty response.", backendWasReached: true)
                : BackendLessonSessionClientResult.Success(lessonSession);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendLessonSessionClientResult.Failure("Backend lesson session PUT timed out.", isBackendReachabilityFailure: true);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendLessonSessionClientResult.Failure("Backend lesson session PUT is unavailable.", isBackendReachabilityFailure: IsBackendReachabilityFailure(exception));
        }
    }



    public Task<BackendLessonSessionClientResult> HeartbeatAsync(
        string? backendBaseUrl,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return SendSessionLifecyclePostAsync(
            backendBaseUrl,
            sessionId,
            BackendConstants.DevLessonSessionHeartbeatEndpointTemplate,
            BackendConstants.LessonSessionHeartbeatEndpointTemplate,
            "heartbeat",
            cancellationToken);
    }

    public Task<BackendLessonSessionClientResult> AbandonAsync(
        string? backendBaseUrl,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return SendSessionLifecyclePostAsync(
            backendBaseUrl,
            sessionId,
            BackendConstants.DevLessonSessionAbandonEndpointTemplate,
            BackendConstants.LessonSessionAbandonEndpointTemplate,
            "abandon",
            cancellationToken);
    }

    public async Task<BackendActiveLessonAbandonClientResult> AbandonActiveAsync(
        string? backendBaseUrl,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();

        try
        {
            var authSession = await authSessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
            var endpoint = string.IsNullOrWhiteSpace(authSession?.AccessToken)
                ? BackendConstants.DevActiveLessonSessionAbandonEndpoint
                : BackendConstants.ActiveLessonSessionAbandonEndpoint;
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpoint));
            AuthenticatedRequestHelper.AddBearerTokenIfPresent(httpRequest, authSession?.AccessToken);
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BackendActiveLessonAbandonClientResult.Failure($"Backend active lesson session abandon POST failed with HTTP {(int)response.StatusCode}.", backendWasReached: true);
            }

            var abandonResponse = await response.Content.ReadFromJsonAsync<BackendActiveLessonAbandonResponse>(JsonOptions, cancellationToken);
            return abandonResponse is null
                ? BackendActiveLessonAbandonClientResult.Failure("Backend active lesson session abandon POST returned an empty response.", backendWasReached: true)
                : BackendActiveLessonAbandonClientResult.Success(abandonResponse);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendActiveLessonAbandonClientResult.Failure("Backend active lesson session abandon POST timed out.", isBackendReachabilityFailure: true);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendActiveLessonAbandonClientResult.Failure("Backend active lesson session abandon POST is unavailable.", isBackendReachabilityFailure: IsBackendReachabilityFailure(exception));
        }
    }

    private async Task<BackendLessonSessionClientResult> SendSessionLifecyclePostAsync(
        string? backendBaseUrl,
        Guid sessionId,
        string devEndpointTemplate,
        string authenticatedEndpointTemplate,
        string operationName,
        CancellationToken cancellationToken)
    {
        using var httpClient = CreateHttpClient();

        try
        {
            var authSession = await authSessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
            var endpointTemplate = string.IsNullOrWhiteSpace(authSession?.AccessToken)
                ? devEndpointTemplate
                : authenticatedEndpointTemplate;
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, string.Format(endpointTemplate, sessionId)));
            AuthenticatedRequestHelper.AddBearerTokenIfPresent(httpRequest, authSession?.AccessToken);
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (await IsLessonSessionEndedElsewhereResponseAsync(response, cancellationToken))
                {
                    return BackendLessonSessionClientResult.LessonSessionEndedElsewhere($"Backend lesson session {operationName} ended elsewhere.");
                }

                return BackendLessonSessionClientResult.Failure($"Backend lesson session {operationName} POST failed with HTTP {(int)response.StatusCode}.", backendWasReached: true);
            }

            var lessonSession = await response.Content.ReadFromJsonAsync<BackendLessonSessionResponse>(JsonOptions, cancellationToken);
            return lessonSession is null
                ? BackendLessonSessionClientResult.Failure($"Backend lesson session {operationName} POST returned an empty response.", backendWasReached: true)
                : BackendLessonSessionClientResult.Success(lessonSession);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendLessonSessionClientResult.Failure($"Backend lesson session {operationName} POST timed out.", isBackendReachabilityFailure: true);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendLessonSessionClientResult.Failure($"Backend lesson session {operationName} POST is unavailable.", isBackendReachabilityFailure: IsBackendReachabilityFailure(exception));
        }
    }

    private static bool IsBackendReachabilityFailure(Exception exception)
    {
        if (exception is HttpRequestException httpRequestException)
        {
            return !httpRequestException.StatusCode.HasValue
                && httpRequestException.HttpRequestError is HttpRequestError.ConnectionError
                    or HttpRequestError.NameResolutionError
                    or HttpRequestError.SecureConnectionError
                    or HttpRequestError.Unknown;
        }

        return exception is TaskCanceledException or InvalidOperationException;
    }

    private static async Task<bool> IsLessonSessionEndedElsewhereResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<BackendErrorResponse>(JsonOptions, cancellationToken);
            if (errorResponse is null)
            {
                return false;
            }

            return string.Equals(errorResponse.Error, LessonSessionEndedElsewhereErrorCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(errorResponse.Code, LessonSessionEndedElsewhereErrorCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(errorResponse.ErrorCode, LessonSessionEndedElsewhereErrorCode, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            return false;
        }
    }

    private static async Task<BackendActiveLessonExistsResponse?> TryReadActiveLessonExistsResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var activeLessonResponse = await response.Content.ReadFromJsonAsync<BackendActiveLessonExistsResponse>(JsonOptions, cancellationToken);
            if (activeLessonResponse is null)
            {
                return null;
            }

            return string.Equals(activeLessonResponse.Error, ActiveLessonExistsErrorCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(activeLessonResponse.Code, ActiveLessonExistsErrorCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(activeLessonResponse.ErrorCode, ActiveLessonExistsErrorCode, StringComparison.OrdinalIgnoreCase)
                ? activeLessonResponse
                : null;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }

    private static async Task<BackendLessonAccessDeniedResponse?> TryReadLessonAccessDeniedResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var deniedResponse = await response.Content.ReadFromJsonAsync<BackendLessonAccessDeniedResponse>(JsonOptions, cancellationToken);
            if (deniedResponse is null)
            {
                return null;
            }

            return string.Equals(deniedResponse.Error, LessonAccessDeniedErrorCode, StringComparison.OrdinalIgnoreCase)
                ? deniedResponse
                : null;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            return null;
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
