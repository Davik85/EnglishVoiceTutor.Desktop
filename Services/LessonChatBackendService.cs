using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class LessonChatBackendService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<LessonChatBackendResponse> SendMockLessonMessageAsync(
        LessonChatBackendRequest request,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(BackendConstants.DefaultBackendBaseUrl),
            Timeout = TimeSpan.FromSeconds(BackendConstants.BackendRequestTimeoutSeconds)
        };

        using var response = await httpClient.PostAsJsonAsync(
            BackendConstants.MockLessonChatEndpoint,
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var backendResponse = await response.Content.ReadFromJsonAsync<LessonChatBackendResponse>(JsonOptions, cancellationToken);

        if (backendResponse is null || string.IsNullOrWhiteSpace(backendResponse.BotReply) || backendResponse.Feedback is null)
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidResponseMessage);
        }

        return backendResponse;
    }
}
