using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services.Usage;

namespace EnglishVoiceTutor.Api.Services;

public sealed class TranslationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonElement TranslationResponseSchema = JsonSerializer.Deserialize<JsonElement>(TranslationResponseSchemaJson);
    private const string OpenAiRequestFailedMessage = "OpenAI translation request failed.";
    private const string OpenAiResponseMissingMessage = "OpenAI translation response is empty.";
    private const string OpenAiResponseTextMissingMessage = "OpenAI translation response does not contain output text.";
    private const string TranslationInputTemplate = "Source language: {0}\nTarget language: {1}\n\nText to translate:\n{2}";
    private const string TranslationResponseSchemaJson = """
{
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "translatedText": {
      "type": "string"
    }
  },
  "required": [
    "translatedText"
  ]
}
""";

    private readonly OpenAiOptionsProvider _optionsProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DevUserProvider _devUserProvider;
    private readonly IUsageEventService _usageEventService;

    public TranslationService(OpenAiOptionsProvider optionsProvider, IHttpClientFactory httpClientFactory, DevUserProvider devUserProvider, IUsageEventService usageEventService)
    {
        _optionsProvider = optionsProvider;
        _httpClientFactory = httpClientFactory;
        _devUserProvider = devUserProvider;
        _usageEventService = usageEventService;
    }

    public async Task<TranslationResponse> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        var trimmedText = request.Text.Trim();
        var targetLanguage = request.TargetLanguage.Trim();
        var sourceLanguage = string.IsNullOrWhiteSpace(request.SourceLanguageName) ? "English" : request.SourceLanguageName.Trim();
        var options = _optionsProvider.GetOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return CreateFallbackResponse(trimmedText);
        }

        try
        {
            var openAiResponse = await SendResponsesApiRequestAsync(trimmedText, sourceLanguage, targetLanguage, options, cancellationToken);
            await _usageEventService.TryRecordAsync(new UsageEventRecord
            {
                UserId = _devUserProvider.GetUserId(),
                Operation = UsageConstants.Operations.Translation,
                Model = options.Model,
                StudyLanguage = request.TargetLanguage,
                Status = UsageConstants.Statuses.Success,
                EstimatedCost = 0m,
                InputTokens = openAiResponse.Usage?.InputTokens,
                OutputTokens = openAiResponse.Usage?.OutputTokens,
                InputCharacters = trimmedText.Length
            }, cancellationToken);

            var outputText = ExtractOutputText(openAiResponse);
            var translation = JsonSerializer.Deserialize<TranslationResponse>(outputText, JsonOptions);

            if (translation is null || string.IsNullOrWhiteSpace(translation.TranslatedText))
            {
                return CreateFallbackResponse(trimmedText);
            }

            return new TranslationResponse
            {
                TranslatedText = translation.TranslatedText.Trim()
            };
        }
        catch
        {
            return CreateFallbackResponse(trimmedText);
        }
    }

    private async Task<OpenAiResponsesResponse> SendResponsesApiRequestAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        OpenAiOptions options,
        CancellationToken cancellationToken)
    {
        var apiRequest = new OpenAiResponsesRequest
        {
            Model = options.Model,
            Instructions = OpenAiConstants.TranslationSystemInstructions,
            Input = string.Format(TranslationInputTemplate, sourceLanguage, targetLanguage, text),
            Text = new OpenAiTextOptions
            {
                Format = new OpenAiTextFormat
                {
                    Type = OpenAiConstants.JsonSchemaFormatType,
                    Name = OpenAiConstants.TranslationResponseSchemaName,
                    Strict = true,
                    Schema = TranslationResponseSchema
                }
            }
        };

        var httpClient = _httpClientFactory.CreateClient();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiConstants.ResponsesEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(OpenAiConstants.AuthorizationScheme, options.ApiKey);

        var requestJson = JsonSerializer.Serialize(apiRequest, JsonOptions);
        httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, OpenAiConstants.ContentTypeJson);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(OpenAiRequestFailedMessage);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsedResponse = JsonSerializer.Deserialize<OpenAiResponsesResponse>(responseJson, JsonOptions);

        if (parsedResponse is null)
        {
            throw new InvalidOperationException(OpenAiResponseMissingMessage);
        }

        return parsedResponse;
    }

    private static string ExtractOutputText(OpenAiResponsesResponse response)
    {
        foreach (var outputItem in response.Output)
        {
            foreach (var contentItem in outputItem.Content)
            {
                if (!string.IsNullOrWhiteSpace(contentItem.Text))
                {
                    return contentItem.Text.Trim();
                }
            }
        }

        throw new InvalidOperationException(OpenAiResponseTextMissingMessage);
    }

    private static TranslationResponse CreateFallbackResponse(string text)
    {
        return new TranslationResponse
        {
            TranslatedText = text
        };
    }
}
