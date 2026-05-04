using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class OpenAiLessonChatService : ILessonChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonElement LessonChatResponseSchema = JsonSerializer.Deserialize<JsonElement>(LessonChatResponseSchemaJson);
    private const string OpenAiRequestFailedMessage = "OpenAI request failed.";
    private const string OpenAiResponseMissingMessage = "OpenAI response is empty.";
    private const string OpenAiResponseTextMissingMessage = "OpenAI response does not contain output text.";
    private const string LessonChatResponseSchemaJson = """
{
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "botReply": {
      "type": "string"
    },
    "feedback": {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "shortText": {
          "type": "string"
        },
        "correctedVersion": {
          "type": "string"
        },
        "grammarTip": {
          "type": "string"
        },
        "vocabularyTip": {
          "type": "string"
        },
        "cultureTip": {
          "type": "string"
        },
        "naturalVersion": {
          "type": "string"
        }
      },
      "required": [
        "shortText",
        "correctedVersion",
        "grammarTip",
        "vocabularyTip",
        "cultureTip",
        "naturalVersion"
      ]
    }
  },
  "required": [
    "botReply",
    "feedback"
  ]
}
""";

    private readonly OpenAiOptionsProvider _optionsProvider;
    private readonly MockLessonChatService _mockLessonChatService;
    private readonly LessonPromptBuilder _lessonPromptBuilder;
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenAiLessonChatService(
        OpenAiOptionsProvider optionsProvider,
        MockLessonChatService mockLessonChatService,
        LessonPromptBuilder lessonPromptBuilder,
        IHttpClientFactory httpClientFactory)
    {
        _optionsProvider = optionsProvider;
        _mockLessonChatService = mockLessonChatService;
        _lessonPromptBuilder = lessonPromptBuilder;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<LessonChatResponse> CreateReplyAsync(
        LessonChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var options = _optionsProvider.GetOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return await _mockLessonChatService.CreateReplyAsync(request, cancellationToken);
        }

        try
        {
            var openAiResponse = await SendResponsesApiRequestAsync(request, options, cancellationToken);
            var outputText = ExtractOutputText(openAiResponse);
            var lessonReply = JsonSerializer.Deserialize<LessonChatResponse>(outputText, JsonOptions);

            if (lessonReply is null)
            {
                return await _mockLessonChatService.CreateReplyAsync(request, cancellationToken);
            }

            if (!IsValidLessonReply(lessonReply))
            {
                return await _mockLessonChatService.CreateReplyAsync(request, cancellationToken);
            }

            return lessonReply;
        }
        catch
        {
            return await _mockLessonChatService.CreateReplyAsync(request, cancellationToken);
        }
    }

    private async Task<OpenAiResponsesResponse> SendResponsesApiRequestAsync(
        LessonChatRequest request,
        OpenAiOptions options,
        CancellationToken cancellationToken)
    {
        var apiRequest = new OpenAiResponsesRequest
        {
            Model = options.Model,
            Instructions = OpenAiConstants.LessonReplySystemInstructions,
            Input = _lessonPromptBuilder.BuildInput(request),
            Text = new OpenAiTextOptions
            {
                Format = new OpenAiTextFormat
                {
                    Type = OpenAiConstants.JsonSchemaFormatType,
                    Name = OpenAiConstants.LessonChatResponseSchemaName,
                    Strict = true,
                    Schema = LessonChatResponseSchema
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

    private static bool IsValidLessonReply(LessonChatResponse reply)
    {
        if (string.IsNullOrWhiteSpace(reply.BotReply) || reply.Feedback is null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(reply.Feedback.ShortText)
            && !string.IsNullOrWhiteSpace(reply.Feedback.CorrectedVersion)
            && !string.IsNullOrWhiteSpace(reply.Feedback.GrammarTip)
            && !string.IsNullOrWhiteSpace(reply.Feedback.VocabularyTip)
            && !string.IsNullOrWhiteSpace(reply.Feedback.CultureTip)
            && !string.IsNullOrWhiteSpace(reply.Feedback.NaturalVersion);
    }
}
