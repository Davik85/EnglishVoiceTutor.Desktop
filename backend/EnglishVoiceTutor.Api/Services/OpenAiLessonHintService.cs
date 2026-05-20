using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services.Usage;

namespace EnglishVoiceTutor.Api.Services;

public sealed class OpenAiLessonHintService : ILessonHintService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonElement LessonHintResponseSchema = JsonSerializer.Deserialize<JsonElement>(LessonHintResponseSchemaJson);
    private const string LessonHintResponseSchemaJson = """
{
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "hintText": {
      "type": "string"
    }
  },
  "required": [
    "hintText"
  ]
}
""";

    private readonly OpenAiOptionsProvider _optionsProvider;
    private readonly MockLessonHintService _mockLessonHintService;
    private readonly LessonPromptBuilder _lessonPromptBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DevUserProvider _devUserProvider;
    private readonly IUsageEventService _usageEventService;

    public OpenAiLessonHintService(
        OpenAiOptionsProvider optionsProvider,
        MockLessonHintService mockLessonHintService,
        LessonPromptBuilder lessonPromptBuilder,
        IHttpClientFactory httpClientFactory,
        DevUserProvider devUserProvider,
        IUsageEventService usageEventService)
    {
        _optionsProvider = optionsProvider;
        _mockLessonHintService = mockLessonHintService;
        _lessonPromptBuilder = lessonPromptBuilder;
        _httpClientFactory = httpClientFactory;
        _devUserProvider = devUserProvider;
        _usageEventService = usageEventService;
    }

    public async Task<LessonHintResponse> CreateHintAsync(LessonChatRequest request, CancellationToken cancellationToken = default)
    {
        var options = _optionsProvider.GetOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return await _mockLessonHintService.CreateHintAsync(request, cancellationToken);
        }

        try
        {
            var apiRequest = new OpenAiResponsesRequest
            {
                Model = options.Model,
                Instructions = OpenAiConstants.LessonHintSystemInstructions,
                Input = _lessonPromptBuilder.BuildHintInput(request),
                Text = new OpenAiTextOptions
                {
                    Format = new OpenAiTextFormat
                    {
                        Type = OpenAiConstants.JsonSchemaFormatType,
                        Name = OpenAiConstants.LessonHintResponseSchemaName,
                        Strict = true,
                        Schema = LessonHintResponseSchema
                    }
                }
            };

            var httpClient = _httpClientFactory.CreateClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiConstants.ResponsesEndpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(OpenAiConstants.AuthorizationScheme, options.ApiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(apiRequest, JsonOptions), Encoding.UTF8, OpenAiConstants.ContentTypeJson);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsedResponse = JsonSerializer.Deserialize<OpenAiResponsesResponse>(responseJson, JsonOptions);
            if (parsedResponse is null)
            {
                return await _mockLessonHintService.CreateHintAsync(request, cancellationToken);
            }

            await _usageEventService.TryRecordAsync(new UsageEventRecord
            {
                UserId = _devUserProvider.GetUserId(),
                Operation = UsageConstants.Operations.LessonChatHint,
                Model = options.Model,
                StudyLanguage = request.TargetLanguageId,
                Status = UsageConstants.Statuses.Success,
                EstimatedCost = 0m,
                InputTokens = parsedResponse.Usage?.InputTokens,
                OutputTokens = parsedResponse.Usage?.OutputTokens
            }, cancellationToken);

            var outputText = ExtractOutputText(parsedResponse);
            var hint = JsonSerializer.Deserialize<LessonHintResponse>(outputText, JsonOptions);

            if (hint is null || string.IsNullOrWhiteSpace(hint.HintText))
            {
                return await _mockLessonHintService.CreateHintAsync(request, cancellationToken);
            }

            return hint;
        }
        catch
        {
            return await _mockLessonHintService.CreateHintAsync(request, cancellationToken);
        }
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

        return string.Empty;
    }
}
