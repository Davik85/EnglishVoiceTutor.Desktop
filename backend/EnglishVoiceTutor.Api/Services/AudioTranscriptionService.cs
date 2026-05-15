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
    private readonly ILogger<AudioTranscriptionService> _logger;

    public AudioTranscriptionService(
        OpenAiOptionsProvider optionsProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<AudioTranscriptionService> logger)
    {
        _optionsProvider = optionsProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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
        var durationSeconds = EstimatePcmWavDurationSeconds(audioFile.Length);
        _logger.LogInformation("Developer usage summary: Operation=audio_transcription; Model={Model}; Language={Language}; InputAudioBytes={InputAudioBytes}; EstimatedDurationSeconds={EstimatedDurationSeconds}; TranscriptCharacters={TranscriptCharacters}; Status=success; CostEstimateApproximate=True; MissingCostFields={MissingCostFields}.", OpenAiConstants.DefaultTranscriptionModel, OpenAiConstants.TranscriptionLanguage, audioFile.Length, durationSeconds, openAiResponse.Text.Trim().Length, PricingConstants.OpenAi.TranscriptionPerMinuteUsd == 0m ? "transcription_pricing" : string.Empty);

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
        formContent.Add(
            new StringContent(OpenAiConstants.TranscriptionLanguage),
            OpenAiConstants.MultipartLanguageFieldName);
        formContent.Add(
            new StringContent(OpenAiConstants.TranscriptionPrompt),
            OpenAiConstants.MultipartPromptFieldName);

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

    private static double EstimatePcmWavDurationSeconds(long bytes)
    {
        const int wavHeaderBytes = 44;
        const int sampleRate = 16000;
        const int channels = 1;
        const int bytesPerSample = 2;
        var audioBytes = Math.Max(0, bytes - wavHeaderBytes);
        return audioBytes / (double)(sampleRate * channels * bytesPerSample);
    }
}
