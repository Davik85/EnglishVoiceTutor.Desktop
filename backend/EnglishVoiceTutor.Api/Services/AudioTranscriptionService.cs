using System.Net.Http.Headers;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using Microsoft.AspNetCore.Http;

namespace EnglishVoiceTutor.Api.Services;

public sealed class AudioTranscriptionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string OpenAiRequestFailedMessage = "OpenAI audio transcription request failed.";
    private const string OpenAiResponseMissingMessage = "OpenAI audio transcription response is empty.";

    private readonly OpenAiOptionsProvider _optionsProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    public AudioTranscriptionService(
        OpenAiOptionsProvider optionsProvider,
        IHttpClientFactory httpClientFactory)
    {
        _optionsProvider = optionsProvider;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AudioTranscriptionResponse> TranscribeAsync(
        IFormFile audioFile,
        CancellationToken cancellationToken = default)
    {
        var options = _optionsProvider.GetOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return new AudioTranscriptionResponse
            {
                Text = ApiConstants.AudioTranscriptionFallbackText
            };
        }

        var openAiResponse = await SendAudioTranscriptionRequestAsync(audioFile, options.ApiKey, cancellationToken);

        return new AudioTranscriptionResponse
        {
            Text = openAiResponse.Text.Trim()
        };
    }

    private async Task<OpenAiAudioTranscriptionResponse> SendAudioTranscriptionRequestAsync(
        IFormFile audioFile,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();

        await using var audioStream = audioFile.OpenReadStream();
        using var formContent = new MultipartFormDataContent();
        using var audioContent = new StreamContent(audioStream);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue(OpenAiConstants.WavContentType);

        formContent.Add(
            audioContent,
            OpenAiConstants.MultipartFileFieldName,
            audioFile.FileName);
        formContent.Add(
            new StringContent(OpenAiConstants.DefaultTranscriptionModel),
            OpenAiConstants.MultipartModelFieldName);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiConstants.AudioTranscriptionsEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(OpenAiConstants.AuthorizationScheme, apiKey);
        httpRequest.Content = formContent;

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(OpenAiRequestFailedMessage);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsedResponse = JsonSerializer.Deserialize<OpenAiAudioTranscriptionResponse>(responseJson, JsonOptions);

        if (parsedResponse is null)
        {
            throw new InvalidOperationException(OpenAiResponseMissingMessage);
        }

        return parsedResponse;
    }
}
