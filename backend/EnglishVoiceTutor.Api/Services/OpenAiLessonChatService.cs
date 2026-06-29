using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services.Usage;

using EnglishVoiceTutor.Api.Services.Auth;

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
    private const int LessonChatProviderMaxAttempts = 2;
    private const string LessonChatFallbackBotReply = "Sorry, I had trouble creating the next reply. Let's continue. Can you answer that again in one short sentence?";
    private const string LessonChatFinalFallbackBotReply = "Sorry, I had trouble creating the final reply. Great work today—let's finish here.";
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
    private readonly IRequestUserResolver _requestUserResolver;
    private readonly IUsageEventService _usageEventService;

    public OpenAiLessonChatService(
        OpenAiOptionsProvider optionsProvider,
        LessonPromptBuilder lessonPromptBuilder,
        TutorAvatarProfileProvider avatarProfileProvider,
        TutorIdentityGuard tutorIdentityGuard,
        IHttpClientFactory httpClientFactory,
        IRequestUserResolver requestUserResolver,
        IUsageEventService usageEventService,
        ILogger<OpenAiLessonChatService> logger)
    {
        _optionsProvider = optionsProvider;
        _lessonPromptBuilder = lessonPromptBuilder;
        _avatarProfileProvider = avatarProfileProvider;
        _tutorIdentityGuard = tutorIdentityGuard;
        _httpClientFactory = httpClientFactory;
        _requestUserResolver = requestUserResolver;
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

        var operation = ResolveOperation(request.RequestPurpose);
        LessonChatResponse? lessonReply = null;
        string? validationReason = null;

        for (var attempt = 1; attempt <= LessonChatProviderMaxAttempts; attempt++)
        {
            OpenAiResponsesResponse openAiResponse;
            try
            {
                openAiResponse = await SendResponsesApiRequestAsync(request, options, validationReason, cancellationToken);
            }
            catch (Exception ex)
            {
                LogProviderCallFailure(ex, operation, request, options.Model);
                throw;
            }

            await TryRecordUsageAsync(operation, request, options.Model, openAiResponse, cancellationToken);
            LogResponsesUsage(operation, request, options.Model, openAiResponse);

            if (TryParseLessonReply(openAiResponse, out lessonReply, out validationReason))
            {
                break;
            }

            _logger.LogWarning(
                "OpenAI lesson chat response invalid. Operation={Operation}; Model={Model}; ResponseId={ResponseId}; LessonId={LessonId}; UserTurnNumber={UserTurnNumber}; Attempt={Attempt}; MaxAttempts={MaxAttempts}; ValidationReason={ValidationReason}.",
                operation,
                options.Model,
                openAiResponse.Id,
                request.LessonScenarioId,
                request.UserTurnNumber,
                attempt,
                LessonChatProviderMaxAttempts,
                validationReason);
        }

        if (lessonReply is null)
        {
            _logger.LogWarning(
                "OpenAI lesson chat response invalid after retry. Operation={Operation}; Model={Model}; LessonId={LessonId}; UserTurnNumber={UserTurnNumber}; ValidationReason={ValidationReason}; SafeFallbackReturned=True.",
                operation,
                options.Model,
                request.LessonScenarioId,
                request.UserTurnNumber,
                validationReason ?? OpenAiResponseInvalidMessage);

            lessonReply = CreateSafeFallbackLessonReply(request);
        }

        var guardTutorProfile = ResolveGuardTutorProfile(request, _avatarProfileProvider.GetById(request.TutorAvatarId));
        var guardedReply = _tutorIdentityGuard.PreventWrongTutorSelfIntroduction(lessonReply, guardTutorProfile, operation);
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
        string? previousValidationReason,
        CancellationToken cancellationToken)
    {
        var input = _lessonPromptBuilder.BuildInput(request);
        if (!string.IsNullOrWhiteSpace(previousValidationReason))
        {
            input = $"{input}\nRepair instruction:\nThe previous provider reply was rejected for this safe schema reason: {previousValidationReason}. Return only a valid JSON object that exactly matches the lesson_chat_response schema. Do not use markdown fences.\n";
        }

        var apiRequest = new OpenAiResponsesRequest
        {
            Model = options.Model,
            Instructions = OpenAiConstants.LessonReplySystemInstructions,
            Input = input,
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
            var safeCategory = OpenAiProviderErrorMapper.MapStatusCode(response.StatusCode);
            throw new OpenAiProviderRequestException(OpenAiRequestFailedMessage, response.StatusCode, safeCategory, ToSafeProviderFailureMessage(safeCategory));
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsedResponse = JsonSerializer.Deserialize<OpenAiResponsesResponse>(responseJson, JsonOptions);

        if (parsedResponse is null)
        {
            throw new InvalidOperationException(OpenAiResponseMissingMessage);
        }

        return parsedResponse;
    }


    private void LogProviderCallFailure(Exception exception, string operation, LessonChatRequest request, string configuredModelId)
    {
        var statusCode = exception is OpenAiProviderRequestException providerException ? (int?)providerException.StatusCode : null;
        var safeCategory = exception is OpenAiProviderRequestException providerRequestException
            ? providerRequestException.SafeCategory
            : OpenAiProviderErrorMapper.MapException(exception);
        var safeMessage = exception is OpenAiProviderRequestException requestException
            ? requestException.SafeProviderMessage
            : exception.Message;

        _logger.LogError(
            exception,
            "Lesson chat provider call failed. operation={Operation}; modelRole=lesson_tutor_chat; configuredModelId={ConfiguredModelId}; lessonScenarioId={LessonScenarioId}; targetLanguageId={TargetLanguageId}; tutorProfileId={TutorProfileId}; providerStatusCode={ProviderStatusCode}; safeProviderCategory={SafeProviderCategory}; exceptionType={ExceptionType}; safeMessage={SafeMessage}.",
            operation,
            configuredModelId,
            request.LessonScenarioId,
            request.TargetLanguageId,
            request.TutorProfileId,
            statusCode,
            safeCategory,
            exception.GetType().Name,
            safeMessage);
    }

    private static string ToSafeProviderFailureMessage(string category) => category switch
    {
        AiModelProviderTestCategories.UnauthorizedOrForbidden => "OpenAI rejected credentials or project access.",
        AiModelProviderTestCategories.UnavailableOrNotFound => "OpenAI model is unavailable or not found for this project.",
        AiModelProviderTestCategories.RateLimited => "OpenAI rate limited the request.",
        AiModelProviderTestCategories.QuotaOrBilling => "OpenAI quota or billing is unavailable.",
        AiModelProviderTestCategories.InvalidRequest => "OpenAI rejected the model/request shape.",
        AiModelProviderTestCategories.ProviderError => "OpenAI returned a provider error.",
        _ => OpenAiRequestFailedMessage
    };

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
            UserId = _requestUserResolver.ResolveCurrentUser().UserId,
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

    internal static bool TryParseLessonReply(OpenAiResponsesResponse response, out LessonChatResponse? lessonReply, out string validationReason)
    {
        lessonReply = null;
        validationReason = string.Empty;

        if (!TryExtractOutputText(response, out var outputText, out validationReason))
        {
            return false;
        }

        var normalizedOutputText = NormalizeJsonOutputText(outputText);
        if (string.IsNullOrWhiteSpace(normalizedOutputText))
        {
            validationReason = "empty_output_text";
            return false;
        }

        try
        {
            lessonReply = JsonSerializer.Deserialize<LessonChatResponse>(normalizedOutputText, JsonOptions);
        }
        catch (JsonException)
        {
            validationReason = "malformed_json";
            return false;
        }

        if (lessonReply is not null && TryValidateLessonReply(lessonReply, out validationReason))
        {
            return true;
        }

        var directValidationReason = validationReason;
        if (TryDeserializeWrappedLessonReply(normalizedOutputText, out lessonReply, out validationReason))
        {
            return true;
        }

        if (TryDeserializeAliasLessonReply(normalizedOutputText, out lessonReply, out validationReason))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(directValidationReason))
        {
            validationReason = directValidationReason;
        }
        else if (lessonReply is null && string.IsNullOrWhiteSpace(validationReason))
        {
            validationReason = "deserialized_null";
        }

        return false;
    }

    private static bool TryExtractOutputText(OpenAiResponsesResponse response, out string outputText, out string validationReason)
    {
        if (!string.IsNullOrWhiteSpace(response.OutputText))
        {
            outputText = response.OutputText.Trim();
            validationReason = string.Empty;
            return true;
        }

        foreach (var outputItem in response.Output)
        {
            foreach (var contentItem in outputItem.Content)
            {
                if (!string.IsNullOrWhiteSpace(contentItem.Text))
                {
                    outputText = contentItem.Text.Trim();
                    validationReason = string.Empty;
                    return true;
                }
            }
        }

        outputText = string.Empty;
        validationReason = OpenAiResponseTextMissingMessage;
        return false;
    }

    private static bool TryDeserializeWrappedLessonReply(string outputText, out LessonChatResponse? lessonReply, out string validationReason)
    {
        lessonReply = null;
        validationReason = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(outputText);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                validationReason = "root_not_object";
                return false;
            }

            foreach (var wrapperName in new[] { "lessonChatResponse", "lesson_chat_response", "response", "data" })
            {
                if (document.RootElement.TryGetProperty(wrapperName, out var wrappedElement))
                {
                    lessonReply = wrappedElement.Deserialize<LessonChatResponse>(JsonOptions);
                    if (lessonReply is not null && TryValidateLessonReply(lessonReply, out validationReason))
                    {
                        return true;
                    }

                    validationReason = string.IsNullOrWhiteSpace(validationReason) ? $"invalid_wrapper_{wrapperName}" : validationReason;
                    return false;
                }
            }
        }
        catch (JsonException)
        {
            validationReason = "malformed_json";
            return false;
        }

        return false;
    }

    private static bool TryDeserializeAliasLessonReply(string outputText, out LessonChatResponse? lessonReply, out string validationReason)
    {
        lessonReply = null;
        validationReason = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(outputText);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("botMessage", out var botMessageElement))
            {
                return false;
            }

            var botMessage = botMessageElement.GetString();
            var feedback = root.TryGetProperty("feedback", out var feedbackElement)
                ? feedbackElement.Deserialize<FeedbackDto>(JsonOptions)
                : null;
            var isLessonComplete = root.TryGetProperty("isLessonComplete", out var completeElement)
                && completeElement.ValueKind is JsonValueKind.True or JsonValueKind.False
                && completeElement.GetBoolean();

            lessonReply = new LessonChatResponse
            {
                BotReply = botMessage ?? string.Empty,
                Feedback = feedback ?? new FeedbackDto(),
                IsLessonComplete = isLessonComplete
            };

            return TryValidateLessonReply(lessonReply, out validationReason);
        }
        catch (JsonException)
        {
            validationReason = "malformed_json";
            return false;
        }
        catch (InvalidOperationException)
        {
            validationReason = "type_mismatch";
            return false;
        }
    }

    private static string NormalizeJsonOutputText(string outputText)
    {
        var trimmed = outputText.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstLineBreak = trimmed.IndexOf('\n');
        if (firstLineBreak < 0)
        {
            return trimmed;
        }

        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFence <= firstLineBreak)
        {
            return trimmed;
        }

        return trimmed[(firstLineBreak + 1)..lastFence].Trim();
    }

    private static bool TryValidateLessonReply(LessonChatResponse reply, out string validationReason)
    {
        if (string.IsNullOrWhiteSpace(reply.BotReply))
        {
            validationReason = "missing_botReply";
            return false;
        }

        if (reply.Feedback is null)
        {
            validationReason = "missing_feedback";
            return false;
        }

        if (string.IsNullOrWhiteSpace(reply.Feedback.ShortText))
        {
            validationReason = "missing_feedback.shortText";
            return false;
        }

        if (string.IsNullOrWhiteSpace(reply.Feedback.CorrectedVersion))
        {
            validationReason = "missing_feedback.correctedVersion";
            return false;
        }

        if (string.IsNullOrWhiteSpace(reply.Feedback.GrammarTip))
        {
            validationReason = "missing_feedback.grammarTip";
            return false;
        }

        if (string.IsNullOrWhiteSpace(reply.Feedback.VocabularyTip))
        {
            validationReason = "missing_feedback.vocabularyTip";
            return false;
        }

        if (string.IsNullOrWhiteSpace(reply.Feedback.CultureTip))
        {
            validationReason = "missing_feedback.cultureTip";
            return false;
        }

        if (string.IsNullOrWhiteSpace(reply.Feedback.NaturalVersion))
        {
            validationReason = "missing_feedback.naturalVersion";
            return false;
        }

        validationReason = string.Empty;
        return true;
    }

    private static TutorAvatarProfile ResolveGuardTutorProfile(LessonChatRequest request, TutorAvatarProfile fallbackProfile)
    {
        if (string.IsNullOrWhiteSpace(request.TutorDisplayName))
        {
            return fallbackProfile;
        }

        return new TutorAvatarProfile
        {
            Id = fallbackProfile.Id,
            DisplayName = request.TutorDisplayName.Trim(),
            Age = fallbackProfile.Age,
            HomeCity = fallbackProfile.HomeCity,
            CountryOrRegion = fallbackProfile.CountryOrRegion,
            Studies = fallbackProfile.Studies,
            Hobbies = fallbackProfile.Hobbies,
            CommunicationStyle = fallbackProfile.CommunicationStyle,
            SpeakingRules = fallbackProfile.SpeakingRules,
            IdentityRules = fallbackProfile.IdentityRules
        };
    }

    private static LessonChatResponse CreateSafeFallbackLessonReply(LessonChatRequest request)
    {
        var shouldEndLessonNow = LessonLimitHelper.ShouldEndLessonNow(request);
        return new LessonChatResponse
        {
            BotReply = shouldEndLessonNow ? LessonChatFinalFallbackBotReply : LessonChatFallbackBotReply,
            Feedback = new FeedbackDto
            {
                ShortText = "I could not create detailed feedback for this turn.",
                CorrectedVersion = "Please answer again in one short sentence.",
                GrammarTip = "Keep your sentence simple and clear.",
                VocabularyTip = "Use words you already know from this lesson.",
                CultureTip = "It is okay to ask for a repeat when something is unclear.",
                NaturalVersion = "Could you answer that again in one short sentence?"
            },
            IsLessonComplete = shouldEndLessonNow
        };
    }
}
