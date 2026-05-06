using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class LessonChatBackendService
{
    private const string HealthyStatus = "ok";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();

        try
        {
            using var response = await httpClient.GetAsync(BackendConstants.HealthEndpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var healthResponse = await response.Content.ReadFromJsonAsync<BackendHealthResponse>(JsonOptions, cancellationToken);

            return healthResponse is not null
                && string.Equals(healthResponse.Status, HealthyStatus, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public async Task<BackendConfigStatusResponse?> GetBackendConfigStatusAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();

        try
        {
            using var response = await httpClient.GetAsync(BackendConstants.BackendConfigStatusEndpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<BackendConfigStatusResponse>(JsonOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<LessonChatBackendResponse> SendLessonMessageAsync(
        LessonChatBackendRequest request,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();

        using var response = await httpClient.PostAsJsonAsync(
            BackendConstants.LessonChatReplyEndpoint,
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

    public async Task<string> SendLessonHintRequestAsync(
        LessonChatBackendRequest request,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();

        using var response = await httpClient.PostAsJsonAsync(
            BackendConstants.LessonChatHintEndpoint,
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var backendResponse = await response.Content.ReadFromJsonAsync<LessonHintBackendResponse>(JsonOptions, cancellationToken);

        if (backendResponse is null || string.IsNullOrWhiteSpace(backendResponse.HintText))
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidResponseMessage);
        }

        return backendResponse.HintText;
    }

    public async Task<string> SendAudioForTranscriptionAsync(
        string audioFilePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audioFilePath) || !File.Exists(audioFilePath))
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidTranscriptionResponseMessage);
        }

        using var httpClient = CreateHttpClient();
        await using var audioStream = File.OpenRead(audioFilePath);
        using var formContent = new MultipartFormDataContent();
        using var audioContent = new StreamContent(audioStream);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue(BackendConstants.WavContentType);

        formContent.Add(
            audioContent,
            BackendConstants.MultipartFileFieldName,
            Path.GetFileName(audioFilePath));

        using var response = await httpClient.PostAsync(
            BackendConstants.AudioTranscriptionEndpoint,
            formContent,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var backendResponse = await response.Content.ReadFromJsonAsync<AudioTranscriptionBackendResponse>(JsonOptions, cancellationToken);

        if (backendResponse is null)
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidTranscriptionResponseMessage);
        }

        return backendResponse.Text.Trim();
    }

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            BaseAddress = new Uri(BackendConstants.DefaultBackendBaseUrl),
            Timeout = TimeSpan.FromSeconds(BackendConstants.BackendRequestTimeoutSeconds)
        };
    }
}
