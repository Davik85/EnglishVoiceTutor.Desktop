using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class AudioSpeechService
{
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

    public async Task<byte[]> CreateSpeechAsync(string text, CancellationToken clientCancellationToken = default)
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

        var request = new OpenAiAudioSpeechRequest
        {
            Model = OpenAiConstants.DefaultSpeechModel,
            Input = text.Trim(),
            Voice = OpenAiConstants.DefaultSpeechVoice,
            Speed = OpenAiConstants.DefaultSpeechSpeed,
            ResponseFormat = OpenAiConstants.DefaultSpeechResponseFormat
        };

        return await SendAudioSpeechRequestAsync(request, options.ApiKey, clientCancellationToken);
    }

    private async Task<byte[]> SendAudioSpeechRequestAsync(
        OpenAiAudioSpeechRequest request,
        string apiKey,
        CancellationToken clientCancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(OpenAiConstants.OpenAiSpeechTimeoutSeconds);
        using var timeoutCancellationTokenSource = new CancellationTokenSource(timeout);
        var stopwatch = Stopwatch.StartNew();
        int? statusCode = null;

        _logger.LogInformation(
            "Starting OpenAI speech request. Model={Model}; Voice={Voice}; ResponseFormat={ResponseFormat}; SpeechSpeed={SpeechSpeed}; InputLength={InputLength}; TimeoutSeconds={TimeoutSeconds}; ClientCancellationRequested={ClientCancellationRequested}.",
            request.Model,
            request.Voice,
            request.ResponseFormat,
            request.Speed,
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
                timeoutCancellationTokenSource.Token);

            statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(timeoutCancellationTokenSource.Token);
                _logger.LogWarning(
                    "OpenAI speech generation failed. StatusCode={StatusCode}; ElapsedMilliseconds={ElapsedMilliseconds}; ResponseBodyLength={ResponseBodyLength}; ClientCancellationRequested={ClientCancellationRequested}; InternalTimeoutReached={InternalTimeoutReached}.",
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    errorBody.Length,
                    clientCancellationToken.IsCancellationRequested,
                    timeoutCancellationTokenSource.IsCancellationRequested);
                throw new HttpRequestException(OpenAiRequestFailedMessage, null, response.StatusCode);
            }

            var audioBytes = await response.Content.ReadAsByteArrayAsync(timeoutCancellationTokenSource.Token);

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
                "Completed OpenAI speech request. Model={Model}; Voice={Voice}; ResponseFormat={ResponseFormat}; SpeechSpeed={SpeechSpeed}; InputLength={InputLength}; TimeoutSeconds={TimeoutSeconds}; ElapsedMilliseconds={ElapsedMilliseconds}; StatusCode={StatusCode}; Canceled={Canceled}; ClientCancellationRequested={ClientCancellationRequested}; InternalTimeoutReached={InternalTimeoutReached}; AudioBytes={AudioBytes}.",
                request.Model,
                request.Voice,
                request.ResponseFormat,
                request.Speed,
                request.Input.Length,
                OpenAiConstants.OpenAiSpeechTimeoutSeconds,
                stopwatch.ElapsedMilliseconds,
                response.StatusCode,
                false,
                clientCancellationToken.IsCancellationRequested,
                timeoutCancellationTokenSource.IsCancellationRequested,
                audioBytes.Length);

            return audioBytes;
        }
        catch (TaskCanceledException exception)
        {
            var internalTimeoutReached = timeoutCancellationTokenSource.IsCancellationRequested;
            var clientCancellationRequested = clientCancellationToken.IsCancellationRequested;

            _logger.LogWarning(
                exception,
                "OpenAI speech request canceled. Model={Model}; Voice={Voice}; ResponseFormat={ResponseFormat}; SpeechSpeed={SpeechSpeed}; InputLength={InputLength}; TimeoutSeconds={TimeoutSeconds}; ElapsedMilliseconds={ElapsedMilliseconds}; StatusCode={StatusCode}; Canceled={Canceled}; ClientCancellationRequested={ClientCancellationRequested}; InternalTimeoutReached={InternalTimeoutReached}.",
                request.Model,
                request.Voice,
                request.ResponseFormat,
                request.Speed,
                request.Input.Length,
                OpenAiConstants.OpenAiSpeechTimeoutSeconds,
                stopwatch.ElapsedMilliseconds,
                statusCode,
                true,
                clientCancellationRequested,
                internalTimeoutReached);

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
}
