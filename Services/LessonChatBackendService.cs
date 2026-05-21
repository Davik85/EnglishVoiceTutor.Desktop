using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed record BotSpeechBackendResponse(byte[] AudioBytes, string ContentType, string FileExtension);

public sealed record BotSpeechStreamMetrics(long BackendHeaderMs, long FirstAudioChunkMs, long TotalStreamMs);

public sealed class AudioTranscriptionBackendException : Exception
{
    public AudioTranscriptionBackendException(HttpStatusCode statusCode)
        : base(BackendConstants.BackendInvalidTranscriptionResponseMessage)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}

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
        Debug.WriteLine($"Sending lesson chat request to {BackendConstants.LessonChatReplyEndpoint}: TargetLanguageId={request.TargetLanguageId}; TargetLanguageName={request.TargetLanguageName}; TargetLanguageCode={request.TargetLanguageCode}; Topic={request.TopicTitle}; Subtopic={request.SubtopicTitle}.");
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                CreateEndpointUri(BackendConstants.LessonChatReplyEndpoint),
                request,
                JsonOptions,
                cancellationToken);

            await ThrowFreeLimitExceededExceptionIfNeededAsync(response, AppConstants.ChatReplyFreeLimitMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            var backendResponse = await response.Content.ReadFromJsonAsync<LessonChatBackendResponse>(JsonOptions, cancellationToken);

            if (backendResponse is null || string.IsNullOrWhiteSpace(backendResponse.BotReply) || backendResponse.Feedback is null)
            {
                throw new InvalidOperationException(BackendConstants.BackendInvalidResponseMessage);
            }

            return backendResponse;
        }
        catch (HttpRequestException exception)
        {
            Debug.WriteLine($"Backend request failed: Operation=lesson_chat_reply; StatusCode={(int?)exception.StatusCode}; ExceptionType={exception.GetType().Name}.");
            throw;
        }
        catch (TaskCanceledException exception)
        {
            Debug.WriteLine($"Backend request failed: Operation=lesson_chat_reply; StatusCode=; ExceptionType={exception.GetType().Name}.");
            throw;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Backend request failed: Operation=lesson_chat_reply; StatusCode=; ExceptionType={exception.GetType().Name}.");
            throw;
        }
    }
    public async Task<BackendFeedbackDto> SendLessonFeedbackRequestAsync(
        LessonChatBackendRequest request,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = CreateHttpClient();
        Debug.WriteLine($"Sending lesson feedback request to {BackendConstants.LessonChatFeedbackEndpoint}: TargetLanguageId={request.TargetLanguageId}; SourceMessageId={request.SourceMessageId}; SourceMessageKind={request.SourceMessageKind}; Topic={request.TopicTitle}; Subtopic={request.SubtopicTitle}.");

        using var response = await httpClient.PostAsJsonAsync(
            CreateEndpointUri(BackendConstants.LessonChatFeedbackEndpoint),
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var backendResponse = await response.Content.ReadFromJsonAsync<BackendFeedbackDto>(JsonOptions, cancellationToken);

        if (backendResponse is null || string.IsNullOrWhiteSpace(backendResponse.ShortText))
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
        Debug.WriteLine($"Sending lesson hint request to {BackendConstants.LessonChatHintEndpoint}: TargetLanguageId={request.TargetLanguageId}; Topic={request.TopicTitle}; Subtopic={request.SubtopicTitle}; LessonPhase={request.LessonPhase}.");

        using var response = await httpClient.PostAsJsonAsync(
            CreateEndpointUri(BackendConstants.LessonChatHintEndpoint),
            request,
            JsonOptions,
            cancellationToken);

        await ThrowFreeLimitExceededExceptionIfNeededAsync(response, AppConstants.HintFreeLimitMessage, cancellationToken);
        response.EnsureSuccessStatusCode();

        var backendResponse = await response.Content.ReadFromJsonAsync<LessonHintBackendResponse>(JsonOptions, cancellationToken);

        if (backendResponse is null || string.IsNullOrWhiteSpace(backendResponse.HintText))
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidResponseMessage);
        }

        return backendResponse.HintText;
    }

    // Stable voice pipeline: used by normal voice input and default TTS Conversation Mode.
    public async Task<string> SendAudioForTranscriptionAsync(
        string audioFilePath,
        StudyLanguageDefinition? targetLanguage = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audioFilePath) || !File.Exists(audioFilePath))
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidTranscriptionResponseMessage);
        }

        using var httpClient = CreateHttpClient();
        await using var audioStream = File.OpenRead(audioFilePath);
        using var formContent = new MultipartFormDataContent();
        var resolvedTargetLanguage = targetLanguage ?? StudyLanguageCatalog.English;
        Debug.WriteLine($"Audio transcription request starting: TargetLanguageId={resolvedTargetLanguage.Id}; TranscriptionLanguageCode={resolvedTargetLanguage.TranscriptionLanguageCode}.");
        using var audioContent = new StreamContent(audioStream);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue(BackendConstants.WavContentType);

        formContent.Add(
            audioContent,
            BackendConstants.MultipartFileFieldName,
            Path.GetFileName(audioFilePath));
        formContent.Add(new StringContent(resolvedTargetLanguage.Id), "targetLanguageId");
        formContent.Add(new StringContent(resolvedTargetLanguage.EnglishName), "targetLanguageName");
        formContent.Add(new StringContent(resolvedTargetLanguage.NativeName), "targetLanguageNativeName");
        formContent.Add(new StringContent(resolvedTargetLanguage.TranscriptionLanguageCode), "targetLanguageCode");

        using var response = await httpClient.PostAsync(
            CreateEndpointUri(BackendConstants.AudioTranscriptionEndpoint),
            formContent,
            cancellationToken);

        await ThrowFreeLimitExceededExceptionIfNeededAsync(response, AppConstants.TranscriptionFreeLimitMessage, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new AudioTranscriptionBackendException(response.StatusCode);
        }

        var backendResponse = await response.Content.ReadFromJsonAsync<AudioTranscriptionBackendResponse>(JsonOptions, cancellationToken);

        if (backendResponse is null)
        {
            throw new AudioTranscriptionBackendException(response.StatusCode);
        }

        return backendResponse.Text.Trim();
    }


    public async Task<BotSpeechBackendResponse> CreateBotSpeechAsync(
        string text,
        CancellationToken cancellationToken = default,
        string purpose = BackendConstants.LessonChatTtsPurpose,
        double? speechSpeed = null,
        string? model = null,
        string? instructions = null,
        StudyLanguageDefinition? targetLanguage = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidSpeechResponseMessage);
        }

        using var httpClient = CreateHttpClient(BackendConstants.BotVoiceRequestTimeoutSeconds);
        var stopwatch = Stopwatch.StartNew();
        var endpointUri = CreateEndpointUri(BackendConstants.AudioSpeechEndpoint);
        var inputLength = text.Length;
        var resolvedModel = string.IsNullOrWhiteSpace(model) ? BackendConstants.LessonChatTtsModel : model;
        var instructionsToSend = BackendConstants.SpeechModelSupportsInstructions(resolvedModel) ? instructions : null;
        var resolvedTargetLanguage = targetLanguage ?? StudyLanguageCatalog.English;

        Debug.WriteLine($"Bot voice backend TTS request starting: Endpoint={BackendConstants.AudioSpeechEndpoint}; Purpose={purpose}; Model={resolvedModel}; SpeechSpeed={speechSpeed?.ToString(CultureInfo.InvariantCulture) ?? "default"}; HasInstructions={!string.IsNullOrWhiteSpace(instructionsToSend)}; InstructionsLength={instructionsToSend?.Length ?? 0}; InputLength={inputLength}; TargetLanguageId={resolvedTargetLanguage.Id}; TargetLanguageCode={resolvedTargetLanguage.Bcp47Code}.");

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                endpointUri,
                new AudioSpeechBackendRequest
                {
                    Text = text,
                    Purpose = purpose,
                    Model = resolvedModel,
                    Instructions = instructionsToSend,
                    SpeechSpeed = speechSpeed,
                    TargetLanguageId = resolvedTargetLanguage.Id,
                    TargetLanguageName = resolvedTargetLanguage.EnglishName,
                    TargetLanguageNativeName = resolvedTargetLanguage.NativeName,
                    TargetLanguageCode = resolvedTargetLanguage.Bcp47Code
                },
                JsonOptions,
                cancellationToken);

            await ThrowFreeLimitExceededExceptionIfNeededAsync(response, AppConstants.BotVoiceFreeLimitMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            if (audioBytes.Length == 0)
            {
                throw new InvalidOperationException(BackendConstants.BackendInvalidSpeechResponseMessage);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? BackendConstants.SpeechResponseContentType;
            Debug.WriteLine($"Bot voice backend TTS request completed: Endpoint={BackendConstants.AudioSpeechEndpoint}; Purpose={purpose}; Model={resolvedModel}; SpeechSpeed={speechSpeed?.ToString(CultureInfo.InvariantCulture) ?? "default"}; HasInstructions={!string.IsNullOrWhiteSpace(instructionsToSend)}; InstructionsLength={instructionsToSend?.Length ?? 0}; InputLength={inputLength}; TargetLanguageId={resolvedTargetLanguage.Id}; TargetLanguageCode={resolvedTargetLanguage.Bcp47Code}; ElapsedMilliseconds={stopwatch.ElapsedMilliseconds}; ContentType={contentType}; AudioBytes={audioBytes.Length}.");
            return new BotSpeechBackendResponse(audioBytes, contentType, GetAudioFileExtension(contentType));
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Bot voice backend TTS request failed: {exception}");
            throw;
        }
    }



    // Stable TTS pipeline: used by normal Lesson Chat voice playback and default TTS Conversation Mode.
    public async Task<BotSpeechStreamMetrics> StreamBotSpeechAsync(
        string text,
        Func<Stream, string, CancellationToken, Task> consumeStreamAsync,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidSpeechResponseMessage);
        }

        using var httpClient = CreateHttpClient(BackendConstants.BotVoiceStreamOverallTimeoutSeconds);
        var stopwatch = Stopwatch.StartNew();
        var endpointUri = CreateEndpointUri(BackendConstants.AudioSpeechStreamEndpoint);
        var inputLength = text.Trim().Length;

        Debug.WriteLine($"Bot voice stream request starting: Endpoint={BackendConstants.AudioSpeechStreamEndpoint}; InputLength={inputLength}.");

        var requestJson = JsonSerializer.Serialize(
            new AudioSpeechBackendRequest
            {
                Text = text,
                Purpose = BackendConstants.LessonChatTtsPurpose
            },
            JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUri)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var backendHeaderMs = stopwatch.ElapsedMilliseconds;
        var contentType = response.Content.Headers.ContentType?.MediaType ?? BackendConstants.PcmContentType;
        Debug.WriteLine($"Bot voice stream backend response headers received: Endpoint={BackendConstants.AudioSpeechStreamEndpoint}; InputLength={inputLength}; ElapsedMilliseconds={backendHeaderMs}; StatusCode={response.StatusCode}; ContentType={contentType}.");

        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await consumeStreamAsync(responseStream, contentType, cancellationToken);

        var totalMs = stopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"Bot voice stream completed: Endpoint={BackendConstants.AudioSpeechStreamEndpoint}; InputLength={inputLength}; BackendHeaderMs={backendHeaderMs}; TotalStreamMs={totalMs}; ContentType={contentType}.");
        return new BotSpeechStreamMetrics(backendHeaderMs, 0, totalMs);
    }

    public async Task<string> TranslateTextAsync(
        string text,
        string targetLanguage,
        StudyLanguageDefinition? sourceLanguage = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(targetLanguage))
        {
            throw new InvalidOperationException(BackendConstants.BackendInvalidTranslationResponseMessage);
        }

        using var httpClient = CreateHttpClient();
        var resolvedSourceLanguage = sourceLanguage ?? StudyLanguageCatalog.English;

        using var response = await httpClient.PostAsJsonAsync(
            CreateEndpointUri(BackendConstants.TranslationEndpoint),
            new TranslationBackendRequest
            {
                Text = text,
                TargetLanguage = targetLanguage,
                SourceLanguageId = resolvedSourceLanguage.Id,
                SourceLanguageName = resolvedSourceLanguage.EnglishName,
                SourceLanguageNativeName = resolvedSourceLanguage.NativeName,
                SourceLanguageCode = resolvedSourceLanguage.Bcp47Code
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

    public Uri CreateRealtimeVoiceWebSocketUri()
    {
        var endpoint = CreateEndpointUri(BackendConstants.RealtimeVoiceEndpoint);
        var builder = new UriBuilder(endpoint)
        {
            Scheme = endpoint.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws"
        };

        return builder.Uri;
    }

    private Uri CreateEndpointUri(string endpointPath)
    {
        return CreateEndpointUri(backendBaseUrl, endpointPath);
    }

    private static Uri CreateEndpointUri(string? backendBaseUrl, string endpointPath)
    {
        return BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpointPath);
    }

    private static async Task ThrowFreeLimitExceededExceptionIfNeededAsync(
        HttpResponseMessage response,
        string defaultUserMessage,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.TooManyRequests)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<FreeLimitExceededResponse>(JsonOptions, cancellationToken);
        var operation = payload?.Operation ?? string.Empty;
        var limitType = payload?.LimitType ?? string.Empty;
        var used = payload?.Used ?? 0;
        var limit = payload?.Limit ?? 0;
        var remaining = payload?.Remaining ?? 0;
        var studyLanguage = payload?.StudyLanguage ?? string.Empty;
        var userMessage = used > 0 && limit > 0
            ? $"{defaultUserMessage.TrimEnd('.')} ({used}/{limit})."
            : defaultUserMessage;

        throw new FreeLimitExceededException(operation, limitType, used, limit, remaining, studyLanguage, userMessage);
    }
}
