using System.Net.Http.Headers;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using Microsoft.AspNetCore.Http;
using EnglishVoiceTutor.Shared.StudyLanguages;
using EnglishVoiceTutor.Api.Services.Usage;

namespace EnglishVoiceTutor.Api.Services;

public sealed class AudioTranscriptionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string OpenAiRequestFailedMessage = "OpenAI audio transcription request failed.";
    private const string OpenAiResponseMissingMessage = "OpenAI audio transcription response is empty.";

    private readonly OpenAiOptionsProvider _optionsProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AudioTranscriptionService> _logger;
    private readonly DevUserProvider _devUserProvider;
    private readonly IUsageEventService _usageEventService;
    private readonly TranslationService _translationService;

    public AudioTranscriptionService(
        OpenAiOptionsProvider optionsProvider,
        IHttpClientFactory httpClientFactory,
        DevUserProvider devUserProvider,
        IUsageEventService usageEventService,
        TranslationService translationService,
        ILogger<AudioTranscriptionService> logger)
    {
        _optionsProvider = optionsProvider;
        _httpClientFactory = httpClientFactory;
        _devUserProvider = devUserProvider;
        _usageEventService = usageEventService;
        _translationService = translationService;
        _logger = logger;
    }

    public async Task<AudioTranscriptionResponse> TranscribeAsync(
        IFormFile audioFile,
        StudyLanguageDefinition? targetLanguage = null,
        CancellationToken cancellationToken = default)
    {
        var options = _optionsProvider.GetOptions();
        var resolvedTargetLanguage = targetLanguage ?? StudyLanguageCatalog.English;

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return new AudioTranscriptionResponse
            {
                Text = ApiConstants.AudioTranscriptionFallbackText
            };
        }

        var openAiResponse = await SendAudioTranscriptionRequestAsync(audioFile, options.ApiKey, resolvedTargetLanguage, cancellationToken);
        var rawTranscript = openAiResponse.Text.Trim();
        var normalizationResult = await NormalizeTranscriptToTargetLanguageAsync(rawTranscript, resolvedTargetLanguage, cancellationToken);

        var durationSeconds = EstimatePcmWavDurationSeconds(audioFile.Length);
        await _usageEventService.TryRecordAsync(new UsageEventRecord
        {
            UserId = _devUserProvider.GetDevUserId(),
            Operation = UsageConstants.Operations.AudioTranscription,
            Model = OpenAiConstants.DefaultTranscriptionModel,
            StudyLanguage = resolvedTargetLanguage.Id,
            Status = UsageConstants.Statuses.Success,
            EstimatedCost = 0m,
            InputCharacters = normalizationResult.Text.Length,
            OutputBytes = null,
            EstimatedDurationSeconds = (decimal)durationSeconds
        }, cancellationToken);

        _logger.LogInformation(
            "Audio transcription completed. TargetLanguageId={TargetLanguageId}; TargetLanguageName={TargetLanguageName}; TranscriptionLanguageCode={TranscriptionLanguageCode}; TranscriptLength={TranscriptLength}; TargetLanguageNormalizationUsed={TargetLanguageNormalizationUsed}.",
            resolvedTargetLanguage.Id,
            resolvedTargetLanguage.EnglishName,
            resolvedTargetLanguage.TranscriptionLanguageCode,
            normalizationResult.Text.Length,
            normalizationResult.NormalizationUsed);

        _logger.LogInformation("Developer usage summary: Operation=audio_transcription; Model={Model}; Language={Language}; InputAudioBytes={InputAudioBytes}; EstimatedDurationSeconds={EstimatedDurationSeconds}; TranscriptCharacters={TranscriptCharacters}; Status=success; CostEstimateApproximate=True; MissingCostFields={MissingCostFields}.", OpenAiConstants.DefaultTranscriptionModel, resolvedTargetLanguage.TranscriptionLanguageCode, audioFile.Length, durationSeconds, normalizationResult.Text.Length, PricingConstants.OpenAi.TranscriptionPerMinuteUsd == 0m ? "transcription_pricing" : string.Empty);

        return new AudioTranscriptionResponse
        {
            Text = normalizationResult.Text
        };
    }

    private async Task<TranscriptNormalizationResult> NormalizeTranscriptToTargetLanguageAsync(
        string rawTranscript,
        StudyLanguageDefinition targetLanguage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawTranscript))
        {
            return new TranscriptNormalizationResult(rawTranscript, false);
        }

        var translation = await _translationService.TranslateAsync(new TranslationRequest
        {
            Text = rawTranscript,
            SourceLanguageName = OpenAiConstants.TranscriptionNormalizationSourceLanguage,
            TargetLanguage = targetLanguage.EnglishName
        }, cancellationToken);

        var normalizedText = translation.TranslatedText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return new TranscriptNormalizationResult(rawTranscript, false);
        }

        var normalizationUsed = !string.Equals(rawTranscript, normalizedText, StringComparison.Ordinal);
        return new TranscriptNormalizationResult(normalizedText, normalizationUsed);
    }

    private async Task<OpenAiAudioTranscriptionResponse> SendAudioTranscriptionRequestAsync(
        IFormFile audioFile,
        string apiKey,
        StudyLanguageDefinition targetLanguage,
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
            new StringContent(targetLanguage.TranscriptionLanguageCode),
            OpenAiConstants.MultipartLanguageFieldName);
        formContent.Add(
            new StringContent(string.Format(OpenAiConstants.TranscriptionPromptTemplate, targetLanguage.EnglishName)),
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

    private readonly record struct TranscriptNormalizationResult(string Text, bool NormalizationUsed);
}
