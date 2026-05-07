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

    private readonly OpenAiOptionsProvider _optionsProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    public AudioSpeechService(OpenAiOptionsProvider optionsProvider, IHttpClientFactory httpClientFactory)
    {
        _optionsProvider = optionsProvider;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<byte[]> CreateSpeechAsync(string text, CancellationToken cancellationToken = default)
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
            Voice = OpenAiConstants.DefaultSpeechVoice
        };

        return await SendAudioSpeechRequestAsync(request, options.ApiKey, cancellationToken);
    }

    private async Task<byte[]> SendAudioSpeechRequestAsync(
        OpenAiAudioSpeechRequest request,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiConstants.AudioSpeechEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(OpenAiConstants.AuthorizationScheme, apiKey);

        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, OpenAiConstants.ContentTypeJson);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(OpenAiRequestFailedMessage);
        }

        var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (audioBytes.Length == 0)
        {
            throw new InvalidOperationException(OpenAiResponseMissingMessage);
        }

        return audioBytes;
    }
}
