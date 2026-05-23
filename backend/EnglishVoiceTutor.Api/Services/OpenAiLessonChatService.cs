using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services.Usage;

namespace EnglishVoiceTutor.Api.Services;

public sealed class OpenAiLessonChatService : ILessonChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonElement LessonChatResponseSchema = JsonSerializer.Deserialize<JsonElement>(LessonChatResponseSchemaJson);
    private const string OpenAiRequestFailedMessage = "OpenAI request failed.";
    private const string OpenAiResponseMissingMessage = "OpenAI response is empty.";
    private const string OpenAiResponseTextMissingMessage = "OpenAI response does not contain output text.";
    private const string OpenAiApiKeyMissingMessage = "OpenAI API key is not configured.";
    private const string OpenAiResponseInvalidMessage = "OpenAI lesson chat response is invalid.";
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
    },
    "isLessonComplete": {
      "type": "boolean"
    }
  },
  "required": [
    "botReply",
    "feedback",
    "isLessonComplete"
  ]
}
""";

    private readonly OpenAiOptionsProvider _optionsProvider;
    private readonly LessonPromptBuilder _lessonPromptBuilder;
    private readonly TutorAvatarProfileProvider _avatarProfileProvider;
    private readonly TutorIdentityGuard _tutorIdentityGuard;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiLessonChatService> _logger;
    private readonly DevUserProvider _devUserProvider;
    private readonly IUsageEventService _usageEventService;

    public OpenAiLessonChatService(
        OpenAiOptionsProvider optionsProvider,
        LessonPromptBuilder lessonPromptBuilder,
        TutorAvatarProfileProvider avatarProfileProvider,
        TutorIdentityGuard tutorIdentityGuard,
        IHttpClientFactory httpClientFactory,
        DevUserProvider devUserProvider,
        IUsageEventService usageEventService,
        ILogger<OpenAiLessonChatService> logger)
    {
        _optionsProvider = optionsProvider;
        _lessonPromptBuilder = lessonPromptBuilder;
        _avatarProfileProvider = avatarProfileProvider;
        _tutorIdentityGuard = tutorIdentityGuard;
        _httpClientFactory = httpClientFactory;
        _devUserProvider = devUserProvider;
        _usageEventService = usageEventService;
        _logger = logger;
    }

    public async Task<LessonChatResponse> CreateReplyAsync(
        LessonChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var options = _optionsProvider.GetOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(OpenAiApiKeyMissingMessage);
        }

        var openAiResponse = await SendResponsesApiRequestAsync(request, options, cancellationToken);
        var operation = ResolveOperation(request.RequestPurpose);
        await TryRecordUsageAsync(operation, request, options.Model, openAiResponse, cancellationToken);
        LogResponsesUsage(operation, request, options.Model, openAiResponse);
        var outputText = ExtractOutputText(openAiResponse);
        var lessonReply = JsonSerializer.Deserialize<LessonChatResponse>(outputText, JsonOptions);

        if (lessonReply is null || !IsValidLessonReply(lessonReply))
        {
            throw new InvalidOperationException(OpenAiResponseInvalidMessage);
        }

        var guardedReply = _tutorIdentityGuard.PreventWrongTutorSelfIntroduction(lessonReply, _avatarProfileProvider.GetById(request.TutorAvatarId), operation);
        var isEnglishTargetLanguage = string.IsNullOrWhiteSpace(request.TargetLanguageId)
            || string.Equals(request.TargetLanguageId, "en", StringComparison.OrdinalIgnoreCase);
        if (AssistantOutputLanguageGuard.IsLanguageSwitchRequest(request.UserMessage)
            || (isEnglishTargetLanguage && AssistantOutputLanguageGuard.IsClearlyNonEnglishTutorOutput(guardedReply.BotReply)))
        {
            _logger.LogWarning(
                "AssistantOutputLanguageViolation Model={Model}; LessonId={LessonId}; Level={Level}; Topic={Topic}; Subtopic={Subtopic}; BotReplyLength={BotReplyLength}; LanguageSwitchRequest={LanguageSwitchRequest}.",
                options.Model,
                request.LessonScenarioId,
                string.IsNullOrWhiteSpace(request.Level) ? request.SelectedLevel : request.Level,
                string.IsNullOrWhiteSpace(request.Topic) ? request.TopicTitle : request.Topic,
                string.IsNullOrWhiteSpace(request.Subtopic) ? request.SubtopicTitle : request.Subtopic,
                guardedReply.BotReply.Length,
                AssistantOutputLanguageGuard.IsLanguageSwitchRequest(request.UserMessage));
            guardedReply = AssistantOutputLanguageGuard.CreateSafeTargetLanguageFallback(request, guardedReply);
        }

        if (LessonLimitHelper.ShouldEndLessonNow(request) && !guardedReply.IsLessonComplete)
        {
            return new LessonChatResponse
            {
                BotReply = guardedReply.BotReply,
                Feedback = guardedReply.Feedback,
                IsLessonComplete = true
            };
        }

        return guardedReply;
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

    private void LogResponsesUsage(string operation, LessonChatRequest request, string model, OpenAiResponsesResponse response)
    {
        var usage = response.Usage;
        var metrics = new OpenAiCallUsageMetrics
        {
            Operation = operation,
            Model = model,
            ResponseId = response.Id,
            InputTokens = usage?.InputTokens,
            OutputTokens = usage?.OutputTokens,
            TotalTokens = usage?.TotalTokens,
            CachedInputTokens = usage?.InputTokensDetails?.CachedTokens,
            AudioInputTokens = usage?.InputTokensDetails?.AudioTokens,
            AudioOutputTokens = usage?.OutputTokensDetails?.AudioTokens
        };

        _logger.LogInformation(
            "Developer usage summary: Operation={Operation}; Model={Model}; ResponseId={ResponseId}; LessonId={LessonId}; Topic={Topic}; Subtopic={Subtopic}; Level={Level}; LessonType={LessonType}; SelectedContext={SelectedContext}; TutorProfileId={TutorProfileId}; InputTokens={InputTokens}; OutputTokens={OutputTokens}; TotalTokens={TotalTokens}; CachedInputTokens={CachedInputTokens}; AudioInputTokens={AudioInputTokens}; AudioOutputTokens={AudioOutputTokens}; HasExactUsage={HasExactUsage}; CostEstimateApproximate=True; MissingCostFields={MissingCostFields}.",
            metrics.Operation,
            metrics.Model,
            metrics.ResponseId,
            request.LessonScenarioId,
            string.IsNullOrWhiteSpace(request.Topic) ? request.TopicTitle : request.Topic,
            string.IsNullOrWhiteSpace(request.Subtopic) ? request.SubtopicTitle : request.Subtopic,
            string.IsNullOrWhiteSpace(request.Level) ? request.SelectedLevel : request.Level,
            request.LessonType,
            request.SelectedContextTitle,
            request.TutorProfileId,
            metrics.InputTokens,
            metrics.OutputTokens,
            metrics.TotalTokens,
            metrics.CachedInputTokens,
            metrics.AudioInputTokens,
            metrics.AudioOutputTokens,
            metrics.HasExactUsage,
            PricingConstants.OpenAi.ChatTextInputPerMillionTokensUsd == 0m || PricingConstants.OpenAi.ChatTextOutputPerMillionTokensUsd == 0m ? "chat_pricing" : string.Empty);
    }



    private async Task TryRecordUsageAsync(string operation, LessonChatRequest request, string model, OpenAiResponsesResponse response, CancellationToken cancellationToken)
    {
        await _usageEventService.TryRecordAsync(new UsageEventRecord
        {
            UserId = _devUserProvider.GetDevUserId(),
            SessionId = request.BackendSessionId,
            Operation = operation,
            Model = model,
            StudyLanguage = ResolveStudyLanguage(request.TargetLanguageName, request.TargetLanguageId),
            Status = UsageConstants.Statuses.Success,
            EstimatedCost = 0m,
            InputTokens = response.Usage?.InputTokens,
            OutputTokens = response.Usage?.OutputTokens,
            AudioInputTokens = response.Usage?.InputTokensDetails?.AudioTokens,
            AudioOutputTokens = response.Usage?.OutputTokensDetails?.AudioTokens
        }, cancellationToken);
    }

    private static string ResolveOperation(string purpose)
    {
        if (string.Equals(purpose, "feedback", StringComparison.OrdinalIgnoreCase))
        {
            return UsageConstants.Operations.LessonChatFeedback;
        }

        return UsageConstants.Operations.LessonChatReply;
    }

    private static string? ResolveStudyLanguage(string? targetLanguageName, string? targetLanguageId)
    {
        if (!string.IsNullOrWhiteSpace(targetLanguageName))
        {
            return targetLanguageName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(targetLanguageId))
        {
            return targetLanguageId.Trim();
        }

        return null;
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
