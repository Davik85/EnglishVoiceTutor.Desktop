using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.Services.Auth;

public static class AuthenticatedRequestHelper
{
    public static void AddBearerTokenIfPresent(HttpRequestMessage request, string? accessToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static async Task<HttpResponseMessage> SendWithRefreshRetryAsync(
        HttpClient httpClient,
        Func<string?, HttpRequestMessage> requestFactory,
        AuthBackendService authBackendService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(requestFactory);
        ArgumentNullException.ThrowIfNull(authBackendService);

        var sessionResult = await authBackendService.EnsureAuthenticatedSessionAsync(cancellationToken);
        var accessToken = sessionResult.Status == AuthSessionEnsureStatus.Success
            ? sessionResult.Session?.AccessToken
            : null;

        var request = requestFactory(accessToken);
        AddBearerTokenIfPresent(request, accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || string.IsNullOrWhiteSpace(accessToken))
        {
            return response;
        }

        response.Dispose();
        var retrySessionResult = await authBackendService.RefreshAuthenticatedSessionOnceAsync(cancellationToken);
        if (retrySessionResult.Status != AuthRefreshStatus.Success || string.IsNullOrWhiteSpace(retrySessionResult.Session?.AccessToken))
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = requestFactory(null),
                ReasonPhrase = "Authentication refresh failed"
            };
        }

        var retryRequest = requestFactory(retrySessionResult.Session.AccessToken);
        AddBearerTokenIfPresent(retryRequest, retrySessionResult.Session.AccessToken);
        return await httpClient.SendAsync(retryRequest, cancellationToken);
    }
}
