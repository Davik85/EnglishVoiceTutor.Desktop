using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

// ChainedVoiceFallback: Not for realtime conversation mode. Used only when Realtime is unavailable or voice mode disabled.
public sealed class AudioSpeechService
{
    public const string DefaultPurpose = "lesson_chat_tts";
    public const string RealtimePreStartOpeningPurpose = "realtime_pre_start_opening";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string MissingApiKeyMessage = "OpenAI speech generation is not configured.";
    private const string OpenAiRequestFailedMessage = "OpenAI speech generation request failed.";
    private const string OpenAiResponseMissingMessage = "OpenAI speech generation response is empty.";
    private const string OpenAiRequestCanceledMessage = "OpenAI speech generation request was canceled.";

    private readonly OpenAiOptionsProvider _optionsProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AudioSpeechService> _logger;

    public AudioSpeechService(
        OpenAiOptionsProvider optionsProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<AudioSpeechService> logger)
    {
        _optionsProvider = optionsProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<byte[]> CreateSpeechAsync(string text, string? purpose = null, CancellationToken clientCancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(ApiConstants.EmptySpeechTextError);
        }

        var options = _optionsProvider.GetOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(MissingApiKeyMessage);
        }

        var normalizedPurpose = NormalizePurpose(purpose);

        var request = new OpenAiAudioSpeechRequest
        {
            Model = OpenAiConstants.DefaultBotVoiceSpeechModel,
            Input = text.Trim(),
            Voice = OpenAiConstants.DefaultSpeechVoice,
            Speed = OpenAiConstants.DefaultSpeechSpeed,
            ResponseFormat = OpenAiConstants.DefaultSpeechResponseFormat
        };

        return await SendAudioSpeechRequestAsync(request, options.ApiKey, normalizedPurpose, clientCancellationToken);
    }


    public async Task<BotVoiceStreamMetrics> StreamSpeechAsync(
        string text,
        Stream outputStream,
        string? purpose = null,
        CancellationToken clientCancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(ApiConstants.EmptySpeechTextError);
        }

        var options = _optionsProvider.GetOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(MissingApiKeyMessage);
        }

        var normalizedPurpose = NormalizePurpose(purpose);

        var request = new OpenAiAudioSpeechRequest
        {
            Model = OpenAiConstants.DefaultBotVoiceSpeechModel,
            Input = text.Trim(),
            Voice = OpenAiConstants.DefaultSpeechVoice,
            Speed = OpenAiConstants.DefaultSpeechSpeed,
            ResponseFormat = OpenAiConstants.DefaultBotVoiceStreamResponseFormat
        };

        return await StreamAudioSpeechRequestAsync(request, options.ApiKey, outputStream, normalizedPurpose, clientCancellationToken);
    }

    private static string NormalizePurpose(string? purpose)
    {
        if (string.Equals(purpose, RealtimePreStartOpeningPurpose, StringComparison.OrdinalIgnoreCase))
        {
            return RealtimePreStartOpeningPurpose;
        }

        return DefaultPurpose;
    }

    private async Task<byte[]> SendAudioSpeechRequestAsync(
        OpenAiAudioSpeechRequest request,
        string apiKey,
        string purpose,
        CancellationToken clientCancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(OpenAiConstants.OpenAiSpeechTimeoutSeconds);
        using var timeoutCancellationTokenSource = new CancellationTokenSource(timeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCancellationTokenSource.Token,
            clientCancellationToken);
        var stopwatch = Stopwatch.StartNew();
        int? statusCode = null;
        long? firstHeaderMs = null;

        _logger.LogInformation(
            "Starting OpenAI speech request. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; SpeechSpeed={SpeechSpeed}; Purpose={Purpose}; InputLength={InputLength}; TimeoutSeconds={TimeoutSeconds}; ClientCancellationRequested={ClientCancellationRequested}.",
            "audio/speech",
            request.Model,
            request.Voice,
            request.ResponseFormat,
            request.Speed,
            purpose,
            request.Input.Length,
            OpenAiConstants.OpenAiSpeechTimeoutSeconds,
            clientCancellationToken.IsCancellationRequested);

        try
        {
            var httpClient = _httpClientFactory.CreateClient(OpenAiConstants.AudioSpeechHttpClientName);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiConstants.AudioSpeechEndpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(OpenAiConstants.AuthorizationScheme, apiKey);

            var requestJson = JsonSerializer.Serialize(request, JsonOptions);
            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, OpenAiConstants.ContentTypeJson);

            using var response = await httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                linkedCancellationTokenSource.Token);

            firstHeaderMs = stopwatch.ElapsedMilliseconds;
            statusCode = (int)response.StatusCode;

            _logger.LogInformation(
                "OpenAI speech response headers received. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; Purpose={Purpose}; InputLength={InputLength}; StatusCode={StatusCode}; FirstHeaderMs={FirstHeaderMs}.",
                "audio/speech",
                request.Model,
                request.Voice,
                request.ResponseFormat,
                purpose,
                request.Input.Length,
                response.StatusCode,
                firstHeaderMs);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(linkedCancellationTokenSource.Token);
                _logger.LogWarning(
                    "OpenAI speech generation failed. StatusCode={StatusCode}; ElapsedMilliseconds={ElapsedMilliseconds}; ResponseBodyLength={ResponseBodyLength}; ClientCancellationRequested={ClientCancellationRequested}; InternalTimeoutReached={InternalTimeoutReached}.",
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    errorBody.Length,
                    clientCancellationToken.IsCancellationRequested,
                    timeoutCancellationTokenSource.IsCancellationRequested);
                throw new HttpRequestException(OpenAiRequestFailedMessage, null, response.StatusCode);
            }

            var audioBytes = await response.Content.ReadAsByteArrayAsync(linkedCancellationTokenSource.Token);

            if (audioBytes.Length == 0)
            {
                _logger.LogWarning(
                    "OpenAI speech generation returned an empty audio response. StatusCode={StatusCode}; ElapsedMilliseconds={ElapsedMilliseconds}; ClientCancellationRequested={ClientCancellationRequested}; InternalTimeoutReached={InternalTimeoutReached}.",
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    clientCancellationToken.IsCancellationRequested,
                    timeoutCancellationTokenSource.IsCancellationRequested);
                throw new InvalidOperationException(OpenAiResponseMissingMessage);
            }

            _logger.LogInformation(
                "Completed OpenAI speech request. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; SpeechSpeed={SpeechSpeed}; Purpose={Purpose}; InputLength={InputLength}; TimeoutSeconds={TimeoutSeconds}; FirstHeaderMs={FirstHeaderMs}; TotalMs={TotalMs}; StatusCode={StatusCode}; Canceled={Canceled}; ClientCancellationRequested={ClientCancellationRequested}; InternalTimeoutReached={InternalTimeoutReached}; TotalBytes={TotalBytes}.",
                "audio/speech",
                request.Model,
                request.Voice,
                request.ResponseFormat,
                request.Speed,
                purpose,
                request.Input.Length,
                OpenAiConstants.OpenAiSpeechTimeoutSeconds,
                firstHeaderMs,
                stopwatch.ElapsedMilliseconds,
                response.StatusCode,
                false,
                clientCancellationToken.IsCancellationRequested,
                timeoutCancellationTokenSource.IsCancellationRequested,
                audioBytes.Length);

            _logger.LogInformation("Developer usage summary: Operation=tts; Model={Model}; Voice={Voice}; Format={Format}; Purpose={Purpose}; InputCharacters={InputCharacters}; OutputBytes={OutputBytes}; EstimatedDurationSeconds={EstimatedDurationSeconds}; CostEstimateApproximate=True; MissingCostFields={MissingCostFields}.", request.Model, request.Voice, request.ResponseFormat, purpose, request.Input.Length, audioBytes.Length, EstimateWavDurationSeconds(audioBytes.LongLength), PricingConstants.OpenAi.Tts1PerMillionCharactersUsd == 0m ? "tts_pricing" : string.Empty);

            return audioBytes;
        }
        catch (TaskCanceledException exception)
        {
            var internalTimeoutReached = timeoutCancellationTokenSource.IsCancellationRequested;
            var clientCancellationRequested = clientCancellationToken.IsCancellationRequested;

            if (clientCancellationRequested)
            {
                _logger.LogInformation(
                    "Audio speech request canceled by client. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; SpeechSpeed={SpeechSpeed}; InputLength={InputLength}; TimeoutSeconds={TimeoutSeconds}; FirstHeaderMs={FirstHeaderMs}; TotalMs={TotalMs}; StatusCode={StatusCode}; Canceled={Canceled}; ClientCancellationRequested={ClientCancellationRequested}; InternalTimeoutReached={InternalTimeoutReached}.",
                    "audio/speech",
                    request.Model,
                    request.Voice,
                    request.ResponseFormat,
                    request.Speed,
                    request.Input.Length,
                    OpenAiConstants.OpenAiSpeechTimeoutSeconds,
                    firstHeaderMs,
                    stopwatch.ElapsedMilliseconds,
                    statusCode,
                    true,
                    clientCancellationRequested,
                    internalTimeoutReached);
            }
            else
            {
                _logger.LogWarning(
                    exception,
                    "OpenAI speech request canceled unexpectedly. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; SpeechSpeed={SpeechSpeed}; InputLength={InputLength}; TimeoutSeconds={TimeoutSeconds}; FirstHeaderMs={FirstHeaderMs}; TotalMs={TotalMs}; StatusCode={StatusCode}; Canceled={Canceled}; ClientCancellationRequested={ClientCancellationRequested}; InternalTimeoutReached={InternalTimeoutReached}.",
                    "audio/speech",
                    request.Model,
                    request.Voice,
                    request.ResponseFormat,
                    request.Speed,
                    request.Input.Length,
                    OpenAiConstants.OpenAiSpeechTimeoutSeconds,
                    firstHeaderMs,
                    stopwatch.ElapsedMilliseconds,
                    statusCode,
                    true,
                    clientCancellationRequested,
                    internalTimeoutReached);
            }

            throw new AudioSpeechRequestCanceledException(
                OpenAiRequestCanceledMessage,
                exception,
                internalTimeoutReached,
                clientCancellationRequested);
        }
        catch (Exception exception) when (exception is not InvalidOperationException and not HttpRequestException)
        {
            _logger.LogError(
                exception,
                "OpenAI speech request failed unexpectedly. Model={Model}; Voice={Voice}; ResponseFormat={ResponseFormat}; SpeechSpeed={SpeechSpeed}; InputLength={InputLength}; TimeoutSeconds={TimeoutSeconds}; ElapsedMilliseconds={ElapsedMilliseconds}; StatusCode={StatusCode}; Canceled={Canceled}; ClientCancellationRequested={ClientCancellationRequested}; InternalTimeoutReached={InternalTimeoutReached}.",
                request.Model,
                request.Voice,
                request.ResponseFormat,
                request.Speed,
                request.Input.Length,
                OpenAiConstants.OpenAiSpeechTimeoutSeconds,
                stopwatch.ElapsedMilliseconds,
                statusCode,
                false,
                clientCancellationToken.IsCancellationRequested,
                timeoutCancellationTokenSource.IsCancellationRequested);
            throw;
        }
    }

    private async Task<BotVoiceStreamMetrics> StreamAudioSpeechRequestAsync(
        OpenAiAudioSpeechRequest request,
        string apiKey,
        Stream outputStream,
        string purpose,
        CancellationToken clientCancellationToken)
    {
        using var overallCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(OpenAiConstants.BotVoiceStreamOverallTimeoutSeconds));
        using var firstAudioCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(OpenAiConstants.BotVoiceFirstAudioTimeoutSeconds));
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            clientCancellationToken,
            overallCancellationTokenSource.Token,
            firstAudioCancellationTokenSource.Token);

        var stopwatch = Stopwatch.StartNew();
        var httpClient = _httpClientFactory.CreateClient(OpenAiConstants.AudioSpeechHttpClientName);
        var totalBytes = 0L;
        long? firstHeaderMs = null;
        long? firstChunkMs = null;
        long? firstChunkWrittenMs = null;
        int? statusCode = null;

        _logger.LogInformation(
            "TTS stream request started. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; InputLength={InputLength}; FirstAudioTimeoutSeconds={FirstAudioTimeoutSeconds}; OverallTimeoutSeconds={OverallTimeoutSeconds}.",
            "audio/speech-stream",
            request.Model,
            request.Voice,
            request.ResponseFormat,
            request.Input.Length,
            OpenAiConstants.BotVoiceFirstAudioTimeoutSeconds,
            OpenAiConstants.BotVoiceStreamOverallTimeoutSeconds);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiConstants.AudioSpeechEndpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(OpenAiConstants.AuthorizationScheme, apiKey);

            var requestJson = JsonSerializer.Serialize(request, JsonOptions);
            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, OpenAiConstants.ContentTypeJson);

            using var response = await httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                linkedCancellationTokenSource.Token);

            statusCode = (int)response.StatusCode;
            firstHeaderMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "OpenAI TTS stream response headers received. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; InputLength={InputLength}; StatusCode={StatusCode}; FirstHeaderMs={FirstHeaderMs}.",
                "audio/speech-stream",
                request.Model,
                request.Voice,
                request.ResponseFormat,
                request.Input.Length,
                response.StatusCode,
                firstHeaderMs);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(linkedCancellationTokenSource.Token);
                _logger.LogWarning(
                    "OpenAI TTS stream failed before audio. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; InputLength={InputLength}; StatusCode={StatusCode}; FirstHeaderMs={FirstHeaderMs}; ResponseBodyLength={ResponseBodyLength}.",
                    "audio/speech-stream",
                    request.Model,
                    request.Voice,
                    request.ResponseFormat,
                    request.Input.Length,
                    response.StatusCode,
                    firstHeaderMs,
                    errorBody.Length);
                throw new HttpRequestException(OpenAiRequestFailedMessage, null, response.StatusCode);
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(linkedCancellationTokenSource.Token);
            var buffer = new byte[16 * 1024];

            while (true)
            {
                var bytesRead = await responseStream.ReadAsync(buffer, linkedCancellationTokenSource.Token);

                if (bytesRead == 0)
                {
                    break;
                }

                if (firstChunkMs is null)
                {
                    firstChunkMs = stopwatch.ElapsedMilliseconds;
                    firstAudioCancellationTokenSource.CancelAfter(Timeout.InfiniteTimeSpan);
                    _logger.LogInformation(
                        "First OpenAI TTS audio chunk received. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; InputLength={InputLength}; FirstHeaderMs={FirstHeaderMs}; FirstChunkMs={FirstChunkMs}; ChunkBytes={ChunkBytes}.",
                        "audio/speech-stream",
                        request.Model,
                        request.Voice,
                        request.ResponseFormat,
                        request.Input.Length,
                        firstHeaderMs,
                        firstChunkMs,
                        bytesRead);
                }

                await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), linkedCancellationTokenSource.Token);
                await outputStream.FlushAsync(linkedCancellationTokenSource.Token);
                totalBytes += bytesRead;

                if (firstChunkWrittenMs is null)
                {
                    firstChunkWrittenMs = stopwatch.ElapsedMilliseconds;
                    _logger.LogInformation(
                        "First OpenAI TTS audio chunk written to client. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; InputLength={InputLength}; FirstHeaderMs={FirstHeaderMs}; FirstChunkMs={FirstChunkMs}; FirstChunkWrittenMs={FirstChunkWrittenMs}; TotalBytes={TotalBytes}.",
                        "audio/speech-stream",
                        request.Model,
                        request.Voice,
                        request.ResponseFormat,
                        request.Input.Length,
                        firstHeaderMs,
                        firstChunkMs,
                        firstChunkWrittenMs,
                        totalBytes);
                }
            }

            if (totalBytes == 0)
            {
                throw new InvalidOperationException(OpenAiResponseMissingMessage);
            }

            _logger.LogInformation(
                "TTS stream completed. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; InputLength={InputLength}; FirstHeaderMs={FirstHeaderMs}; FirstChunkMs={FirstChunkMs}; FirstChunkWrittenMs={FirstChunkWrittenMs}; TotalMs={TotalMs}; TotalBytes={TotalBytes}; Canceled={Canceled}; ClientCancellationRequested={ClientCancellationRequested}; FirstAudioTimeoutReached={FirstAudioTimeoutReached}; OverallTimeoutReached={OverallTimeoutReached}.",
                "audio/speech-stream",
                request.Model,
                request.Voice,
                request.ResponseFormat,
                request.Input.Length,
                firstHeaderMs,
                firstChunkMs,
                firstChunkWrittenMs,
                stopwatch.ElapsedMilliseconds,
                totalBytes,
                false,
                clientCancellationToken.IsCancellationRequested,
                false,
                false);

            _logger.LogInformation("Developer usage summary: Operation=tts_stream; Model={Model}; Voice={Voice}; Format={Format}; Purpose={Purpose}; InputCharacters={InputCharacters}; OutputBytes={OutputBytes}; EstimatedDurationSeconds={EstimatedDurationSeconds}; CostEstimateApproximate=True; MissingCostFields={MissingCostFields}.", request.Model, request.Voice, request.ResponseFormat, purpose, request.Input.Length, totalBytes, EstimatePcmDurationSeconds(totalBytes), PricingConstants.OpenAi.Tts1PerMillionCharactersUsd == 0m ? "tts_pricing" : string.Empty);

            return new BotVoiceStreamMetrics(firstHeaderMs, firstChunkMs, firstChunkWrittenMs, stopwatch.ElapsedMilliseconds, totalBytes);
        }
        catch (OperationCanceledException exception)
        {
            var firstAudioTimeoutReached = firstAudioCancellationTokenSource.IsCancellationRequested && firstChunkMs is null;
            var overallTimeoutReached = overallCancellationTokenSource.IsCancellationRequested;

            _logger.LogWarning(
                exception,
                "TTS stream canceled/timeout. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; InputLength={InputLength}; StatusCode={StatusCode}; FirstHeaderMs={FirstHeaderMs}; FirstChunkMs={FirstChunkMs}; FirstChunkWrittenMs={FirstChunkWrittenMs}; TotalMs={TotalMs}; TotalBytes={TotalBytes}; Canceled={Canceled}; ClientCancellationRequested={ClientCancellationRequested}; FirstAudioTimeoutReached={FirstAudioTimeoutReached}; OverallTimeoutReached={OverallTimeoutReached}.",
                "audio/speech-stream",
                request.Model,
                request.Voice,
                request.ResponseFormat,
                request.Input.Length,
                statusCode,
                firstHeaderMs,
                firstChunkMs,
                firstChunkWrittenMs,
                stopwatch.ElapsedMilliseconds,
                totalBytes,
                true,
                clientCancellationToken.IsCancellationRequested,
                firstAudioTimeoutReached,
                overallTimeoutReached);

            throw new AudioSpeechRequestCanceledException(
                OpenAiRequestCanceledMessage,
                exception,
                firstAudioTimeoutReached || overallTimeoutReached,
                clientCancellationToken.IsCancellationRequested);
        }
        catch (Exception exception) when (exception is not AudioSpeechRequestCanceledException)
        {
            _logger.LogWarning(
                exception,
                "TTS stream failed. Endpoint={Endpoint}; Model={Model}; Voice={Voice}; Format={Format}; InputLength={InputLength}; StatusCode={StatusCode}; FirstHeaderMs={FirstHeaderMs}; FirstChunkMs={FirstChunkMs}; FirstChunkWrittenMs={FirstChunkWrittenMs}; TotalMs={TotalMs}; TotalBytes={TotalBytes}.",
                "audio/speech-stream",
                request.Model,
                request.Voice,
                request.ResponseFormat,
                request.Input.Length,
                statusCode,
                firstHeaderMs,
                firstChunkMs,
                firstChunkWrittenMs,
                stopwatch.ElapsedMilliseconds,
                totalBytes);
            throw;
        }
    }

    private static double EstimateWavDurationSeconds(long bytes)
    {
        const int wavHeaderBytes = 44;
        const int sampleRate = 24000;
        const int channels = 1;
        const int bytesPerSample = 2;
        var audioBytes = Math.Max(0, bytes - wavHeaderBytes);
        return audioBytes / (double)(sampleRate * channels * bytesPerSample);
    }

    private static double EstimatePcmDurationSeconds(long bytes)
    {
        const int sampleRate = 24000;
        const int channels = 1;
        const int bytesPerSample = 2;
        return Math.Max(0, bytes) / (double)(sampleRate * channels * bytesPerSample);
    }
}

public sealed record BotVoiceStreamMetrics(
    long? FirstHeaderMs,
    long? FirstChunkMs,
    long? FirstChunkWrittenMs,
    long TotalMs,
    long TotalBytes);
