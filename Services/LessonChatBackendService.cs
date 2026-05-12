using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed record BotSpeechBackendResponse(byte[] AudioBytes, string ContentType, string FileExtension);

public sealed class LessonChatBackendService
{
    private const string HealthyStatus = "ok";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private string backendBaseUrl = BackendConstants.DefaultBackendBaseUrl;

    public void SetBackendBaseUrl(string? value)
    {
        backendBaseUrl = BackendEndpointBuilder.NormalizeBaseUrl(value);
    }

    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return CheckHealthAsync(backendBaseUrl, cancellationToken);
    }

    public async Task<bool> CheckHealthAsync(string? backendBaseUrlOverride, CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient(BackendConstants.BackendHealthTimeoutSeconds);

        try
        {
            using var response = await httpClient.GetAsync(CreateEndpointUri(backendBaseUrlOverride, BackendConstants.HealthEndpoint), cancellationToken);

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

    public Task<BackendConfigStatusResponse?> GetBackendConfigStatusAsync(CancellationToken cancellationToken = default)
    {
        return GetBackendConfigStatusAsync(backendBaseUrl, cancellationToken);
    }

    public async Task<BackendConfigStatusResponse?> GetBackendConfigStatusAsync(string? backendBaseUrlOverride, CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();

        try
        {
            using var response = await httpClient.GetAsync(CreateEndpointUri(backendBaseUrlOverride, BackendConstants.BackendConfigStatusEndpoint), cancellationToken);

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
            CreateEndpointUri(BackendConstants.LessonChatReplyEndpoint),
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
            CreateEndpointUri(BackendConstants.LessonChatHintEndpoint),
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
            CreateEndpointUri(BackendConstants.AudioTranscriptionEndpoint),
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


    public async Task<BotSpeechBackendResponse> CreateBotSpeechAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidSpeechResponseMessage);
        }

        using var httpClient = CreateHttpClient(BackendConstants.BotVoiceRequestTimeoutSeconds);

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                CreateEndpointUri(BackendConstants.AudioSpeechEndpoint),
                new AudioSpeechBackendRequest
                {
                    Text = text
                },
                JsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            if (audioBytes.Length == 0)
            {
                throw new InvalidOperationException(BackendConstants.BackendInvalidSpeechResponseMessage);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? BackendConstants.SpeechResponseContentType;
            return new BotSpeechBackendResponse(audioBytes, contentType, GetAudioFileExtension(contentType));
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Bot voice backend TTS request failed: {exception}");
            throw;
        }
    }


    public async Task<string> TranslateTextAsync(
        string text,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(targetLanguage))
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidTranslationResponseMessage);
        }

        using var httpClient = CreateHttpClient();

        using var response = await httpClient.PostAsJsonAsync(
            CreateEndpointUri(BackendConstants.TranslationEndpoint),
            new TranslationBackendRequest
            {
                Text = text,
                TargetLanguage = targetLanguage
            },
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var backendResponse = await response.Content.ReadFromJsonAsync<TranslationBackendResponse>(JsonOptions, cancellationToken);

        if (backendResponse is null || string.IsNullOrWhiteSpace(backendResponse.TranslatedText))
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidTranslationResponseMessage);
        }

        return backendResponse.TranslatedText.Trim();
    }

    private HttpClient CreateHttpClient(int timeoutSeconds = BackendConstants.BackendRequestTimeoutSeconds)
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        if (!httpClient.DefaultRequestHeaders.Contains(BackendConstants.NgrokSkipBrowserWarningHeaderName))
        {
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                BackendConstants.NgrokSkipBrowserWarningHeaderName,
                BackendConstants.NgrokSkipBrowserWarningHeaderValue);
        }

        if (!httpClient.DefaultRequestHeaders.UserAgent.Any(
                productInfo => string.Equals(
                    productInfo.Product?.Name,
                    BackendConstants.BackendUserAgentProductName,
                    StringComparison.OrdinalIgnoreCase)))
        {
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    BackendConstants.BackendUserAgentProductName,
                    BackendConstants.BackendUserAgentVersion));
        }

        return httpClient;
    }


    private static string GetAudioFileExtension(string contentType)
    {
        if (contentType.Equals(BackendConstants.WavContentType, StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("audio/wave", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("audio/x-wav", StringComparison.OrdinalIgnoreCase))
        {
            return AudioConstants.WavFileExtension;
        }

        return AudioConstants.Mp3FileExtension;
    }

    private Uri CreateEndpointUri(string endpointPath)
    {
        return CreateEndpointUri(backendBaseUrl, endpointPath);
    }

    private static Uri CreateEndpointUri(string? backendBaseUrl, string endpointPath)
    {
        return BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpointPath);
    }
}
