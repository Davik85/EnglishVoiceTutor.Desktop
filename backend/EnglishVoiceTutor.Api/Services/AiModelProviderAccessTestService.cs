using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class AiModelProviderAccessTestService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonElement TinySchema = JsonSerializer.Deserialize<JsonElement>("""
{
  "type": "object",
  "additionalProperties": false,
  "properties": { "ok": { "type": "boolean" } },
  "required": [ "ok" ]
}
""");
    private readonly IAiModelSettingsService _settingsService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiModelProviderAccessTestService(IAiModelSettingsService settingsService, IHttpClientFactory httpClientFactory)
    { _settingsService = settingsService; _httpClientFactory = httpClientFactory; }

    public async Task<AiModelProviderTestResponse> TestDraftAsync(AiModelSettings draft, CancellationToken cancellationToken)
    {
        var validation = _settingsService.Validate(draft);
        var apiKey = Environment.GetEnvironmentVariable(OpenAiConstants.ApiKeyEnvironmentVariableName) ?? string.Empty;
        var results = new List<AiModelProviderTestResult>
        {
            await TestTextRoleAsync("lesson_tutor_chat", "Lesson tutor chat model", draft.LessonTutorChatModel, validation, apiKey, cancellationToken),
            await TestTextRoleAsync("feedback_correction", "Feedback / correction model", draft.FeedbackCorrectionModel, validation, apiKey, cancellationToken),
            await TestTextRoleAsync("lesson_hint", "Lesson hint model", draft.LessonHintModel, validation, apiKey, cancellationToken),
            await TestTextRoleAsync("translation", "Translation model", draft.TranslationModel, validation, apiKey, cancellationToken),
            NotTested("speech_to_text", "Speech-to-text model", draft.SpeechToTextModel, validation),
            NotTested("lesson_chat_text_to_speech", "Lesson chat text-to-speech model", draft.LessonChatTextToSpeechModel, validation),
            NotTested("conversation_mode_text_to_speech", "Conversation mode text-to-speech model", draft.ConversationModeTextToSpeechModel, validation),
            NotTested("realtime_voice", "Realtime voice model", draft.RealtimeVoiceModel, validation)
        };
        var tested = results.Where(result => result.ProviderTested).ToArray();
        var overall = tested.Length > 0 && tested.All(result => result.ProviderOk == true) && results.All(result => result.SyntaxValid) ? "success" : tested.Any(result => result.ProviderOk == false) || results.Any(result => !result.SyntaxValid) ? "failed" : "partial";
        if (results.Any(result => !result.ProviderTested) && overall == "success") overall = "partial";
        var diagnostics = await RunLessonTutorChatCompatibilityDiagnosticsAsync(draft, validation, apiKey, cancellationToken);
        return new AiModelProviderTestResponse(overall, results, diagnostics);
    }

    private async Task<IReadOnlyList<AiModelProviderCompatibilityDiagnosticResult>> RunLessonTutorChatCompatibilityDiagnosticsAsync(AiModelSettings draft, AiModelSettingsValidationResponse validation, string apiKey, CancellationToken cancellationToken)
    {
        if (!IsRoleSyntaxValid(validation, "Lesson tutor chat model") || string.IsNullOrWhiteSpace(apiKey)) return Array.Empty<AiModelProviderCompatibilityDiagnosticResult>();
        var model = draft.LessonTutorChatModel.Trim();
        var tests = new (string Name, OpenAiResponsesRequest Request)[]
        {
            ("minimal_responses_text", new OpenAiResponsesRequest { Model = model, Instructions = "Reply with ok.", Input = "ok" }),
            ("current_provider_test_shape", new OpenAiResponsesRequest { Model = model, Instructions = "Return the word ok.", Input = "ok", Temperature = 0 }),
            ("minimal_structured_output", new OpenAiResponsesRequest { Model = model, Instructions = "Return JSON with ok true.", Input = "ok", Text = new OpenAiTextOptions { Format = new OpenAiTextFormat { Type = OpenAiConstants.JsonSchemaFormatType, Name = "tiny_safe_schema", Strict = true, Schema = TinySchema } } }),
            ("lesson_chat_runtime_shape_without_user_content", CreateSafeLessonRuntimeShape(model))
        };
        var results = new List<AiModelProviderCompatibilityDiagnosticResult>();
        foreach (var test in tests) results.Add(await SendDiagnosticAsync(test.Name, test.Request, apiKey, cancellationToken));
        return results;
    }

    private static OpenAiResponsesRequest CreateSafeLessonRuntimeShape(string model) => new()
    {
        Model = model,
        Instructions = OpenAiConstants.LessonReplySystemInstructions,
        Input = "Safe diagnostic lesson input. No user lesson content is included.",
        Temperature = OpenAiLessonChatService.ResolveTemperature(model),
        Text = new OpenAiTextOptions { Format = new OpenAiTextFormat { Type = OpenAiConstants.JsonSchemaFormatType, Name = OpenAiConstants.LessonChatResponseSchemaName, Strict = true, Schema = JsonSerializer.Deserialize<JsonElement>("""
{
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "botReply": { "type": "string" },
    "feedback": { "type": "object", "additionalProperties": false, "properties": { "shortText": { "type": "string" }, "correctedVersion": { "type": "string" }, "grammarTip": { "type": "string" }, "vocabularyTip": { "type": "string" }, "cultureTip": { "type": "string" }, "naturalVersion": { "type": "string" } }, "required": [ "shortText", "correctedVersion", "grammarTip", "vocabularyTip", "cultureTip", "naturalVersion" ] },
    "isLessonComplete": { "type": "boolean" }
  },
  "required": [ "botReply", "feedback", "isLessonComplete" ]
}
""") } }
    };

    private async Task<AiModelProviderTestResult> TestTextRoleAsync(string roleId, string roleLabel, string modelId, AiModelSettingsValidationResponse validation, string apiKey, CancellationToken cancellationToken)
    {
        var syntaxValid = IsRoleSyntaxValid(validation, roleLabel);
        if (!syntaxValid) return new(roleId, roleLabel, modelId, false, false, false, AiModelProviderTestCategories.InvalidRequest, "Model ID failed format validation; provider access was not tested.", null, null);
        if (string.IsNullOrWhiteSpace(apiKey)) return new(roleId, roleLabel, modelId, true, false, null, AiModelProviderTestCategories.NotTested, "OpenAI API key is not configured on the server; provider access was not tested.", null, null);
        var diagnostic = await SendDiagnosticAsync("provider_access", new OpenAiResponsesRequest { Model = modelId.Trim(), Instructions = "Return the word ok.", Input = "ok", Temperature = 0 }, apiKey, cancellationToken);
        return new(roleId, roleLabel, modelId, true, true, diagnostic.ProviderOk, diagnostic.SafeCategory, diagnostic.ProviderOk ? "Provider accepted a minimal safe Responses API request for this model." : ToSafeMessage(diagnostic.SafeCategory), diagnostic.StatusCode, diagnostic.DurationMs, diagnostic.ProviderErrorType, diagnostic.ProviderErrorCode, diagnostic.ProviderErrorParam, diagnostic.SanitizedProviderMessage);
    }

    private async Task<AiModelProviderCompatibilityDiagnosticResult> SendDiagnosticAsync(string testName, OpenAiResponsesRequest request, string apiKey, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var httpClient = _httpClientFactory.CreateClient(); using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); timeout.CancelAfter(TimeSpan.FromSeconds(12));
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiConstants.ResponsesEndpoint); httpRequest.Headers.Authorization = new AuthenticationHeaderValue(OpenAiConstants.AuthorizationScheme, apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, OpenAiConstants.ContentTypeJson);
            using var response = await httpClient.SendAsync(httpRequest, timeout.Token); stopwatch.Stop();
            if (response.IsSuccessStatusCode) return new(testName, true, (int)response.StatusCode, AiModelProviderTestCategories.Ok, null, null, null, null, stopwatch.ElapsedMilliseconds);
            var body = await response.Content.ReadAsStringAsync(timeout.Token); var details = OpenAiProviderErrorMapper.MapProviderError(response.StatusCode, body);
            return new(testName, false, details.StatusCode, details.SafeCategory, details.ProviderErrorType, details.ProviderErrorCode, details.ProviderErrorParam, details.SanitizedProviderMessage, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TimeoutException or HttpRequestException)
        { stopwatch.Stop(); var category = OpenAiProviderErrorMapper.MapException(ex); return new(testName, false, null, category, null, null, null, OpenAiProviderErrorMapper.SanitizeProviderMessage(ex.Message), stopwatch.ElapsedMilliseconds); }
    }

    private static AiModelProviderTestResult NotTested(string roleId, string roleLabel, string modelId, AiModelSettingsValidationResponse validation) =>
        new(roleId, roleLabel, modelId, IsRoleSyntaxValid(validation, roleLabel), false, null, AiModelProviderTestCategories.NotTested, "This action performs syntax validation only for this audio/realtime role; no realtime session or audio provider request was opened.", null, null);
    private static bool IsRoleSyntaxValid(AiModelSettingsValidationResponse validation, string roleLabel) => validation.Errors.All(error => !error.StartsWith(roleLabel, StringComparison.OrdinalIgnoreCase));
    private static string ToSafeMessage(string category) => category switch
    {
        AiModelProviderTestCategories.UnauthorizedOrForbidden => "Provider rejected server credentials or project access for this model.",
        AiModelProviderTestCategories.UnavailableOrNotFound => "Provider reported this model is unavailable or not found for the current project.",
        AiModelProviderTestCategories.RateLimited => "Provider rate limited the test request.",
        AiModelProviderTestCategories.QuotaOrBilling => "Provider reported quota or billing access is unavailable.",
        AiModelProviderTestCategories.InvalidRequest => "Provider rejected the model/request shape.",
        AiModelProviderTestCategories.Timeout => "Provider access test timed out.",
        AiModelProviderTestCategories.ProviderError => "Provider returned a server error.",
        _ => "Provider access test failed for an unknown safe category."
    };
}
