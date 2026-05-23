using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services.Auth;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class BackendLessonMessageClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AuthSessionStorageService authSessionStorageService = new();

    public async Task<BackendLessonMessageClientResult> CreateAsync(
        string? backendBaseUrl,
        Guid sessionId,
        CreateBackendLessonMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpClient = CreateHttpClient();

        try
        {
            var endpoint = string.Format(BackendConstants.DevLessonSessionMessagesEndpointTemplate, sessionId);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpoint))
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            var session = await authSessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
            AuthenticatedRequestHelper.AddBearerTokenIfPresent(httpRequest, session?.AccessToken);
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BackendLessonMessageClientResult.Failure($"Backend lesson message POST failed with HTTP {(int)response.StatusCode}.");
            }

            var message = await response.Content.ReadFromJsonAsync<BackendLessonMessageResponse>(JsonOptions, cancellationToken);
            return message is null
                ? BackendLessonMessageClientResult.Failure("Backend lesson message POST returned an empty response.")
                : BackendLessonMessageClientResult.Success(message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BackendLessonMessageClientResult.Failure("Backend lesson message POST timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return BackendLessonMessageClientResult.Failure("Backend lesson message POST is unavailable.");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(BackendConstants.LessonMessageRequestTimeoutSeconds)
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
