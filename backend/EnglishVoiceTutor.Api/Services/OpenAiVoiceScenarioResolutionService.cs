using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class OpenAiVoiceScenarioResolutionService : IVoiceScenarioResolutionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonElement ResponseSchema = JsonSerializer.Deserialize<JsonElement>(SchemaJson);
    private const string SchemaJson = """
{
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "decision": { "type": "string", "enum": ["published_context", "free_context", "clarify", "unsafe"] },
    "matchedContextId": { "type": ["string", "null"] },
    "confidence": { "type": "number", "minimum": 0, "maximum": 1 },
    "candidateContextIds": { "type": "array", "maxItems": 2, "items": { "type": "string" } },
    "normalizedFreeContext": { "type": ["string", "null"] },
    "clarificationText": { "type": ["string", "null"] }
  },
  "required": ["decision", "matchedContextId", "confidence", "candidateContextIds", "normalizedFreeContext", "clarificationText"]
}
""";

    private const string Instructions = """
Classify an initial spoken lesson-scenario selection against only the supplied finite candidate list.
Use the recognized phrase, study language, learner level, topic, subtopic, and learner-facing candidate titles/descriptions.
Tolerate speech-recognition errors, missing or incorrect words, reordered words, weak grammar, short meaningful phrases, and natural paraphrases.
Return published_context when exactly one supplied candidate best represents the intended situation.
Return free_context when the learner describes a concrete situation materially different from every candidate.
Return clarify only when the request is generic or multiple supplied candidates are genuinely plausible.
Return unsafe when the text is unusable or unsafe.
Never invent or alter a candidate ID. matchedContextId and candidateContextIds may contain only IDs supplied in the input.
For free_context, preserve the learner's specific intended situation in normalizedFreeContext without adding tutor instructions.
Return only JSON matching the strict schema.
""";

    private readonly OpenAiOptionsProvider _optionsProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenAiVoiceScenarioResolutionService(
        OpenAiOptionsProvider optionsProvider,
        IHttpClientFactory httpClientFactory)
    {
        _optionsProvider = optionsProvider;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<VoiceScenarioResolutionResponse> ResolveAsync(
        VoiceScenarioResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var options = _optionsProvider.GetOptions();
        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.Model))
        {
            throw new InvalidOperationException("Voice scenario resolution is not configured.");
        }

        var providerRequest = new OpenAiResponsesRequest
        {
            Model = options.Model,
            Instructions = Instructions,
            Input = JsonSerializer.Serialize(request, JsonOptions),
            Temperature = OpenAiLessonChatService.ResolveTemperature(options.Model),
            Text = new OpenAiTextOptions
            {
                Format = new OpenAiTextFormat
                {
                    Type = OpenAiConstants.JsonSchemaFormatType,
                    Name = "voice_scenario_resolution",
                    Strict = true,
                    Schema = ResponseSchema
                }
            }
        };

        var client = _httpClientFactory.CreateClient();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiConstants.ResponsesEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(OpenAiConstants.AuthorizationScheme, options.ApiKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(providerRequest, JsonOptions),
            Encoding.UTF8,
            OpenAiConstants.ContentTypeJson);
        using var providerResponse = await client.SendAsync(httpRequest, cancellationToken);
        providerResponse.EnsureSuccessStatusCode();
        var envelope = JsonSerializer.Deserialize<OpenAiResponsesResponse>(
            await providerResponse.Content.ReadAsStringAsync(cancellationToken),
            JsonOptions) ?? throw new InvalidDataException("Voice scenario provider response was empty.");
        var result = JsonSerializer.Deserialize<VoiceScenarioResolutionResponse>(
            ExtractOutputText(envelope), JsonOptions)
            ?? throw new InvalidDataException("Voice scenario provider output was invalid.");
        ValidateResponse(result, request.Candidates.Select(candidate => candidate.Id).ToHashSet(StringComparer.Ordinal));
        return result;
    }

    internal static void ValidateRequest(VoiceScenarioResolutionRequest request)
    {
        if (!request.IsInitialScenarioSelectionTurn || string.IsNullOrWhiteSpace(request.RecognizedText))
            throw new ArgumentException("A usable initial scenario-selection transcript is required.");
        if (request.Candidates.Count > 10 || request.Candidates.Any(candidate =>
                string.IsNullOrWhiteSpace(candidate.Id) || string.IsNullOrWhiteSpace(candidate.Title)))
            throw new ArgumentException("At most ten valid context candidates are allowed.");
        if (request.Candidates.Select(candidate => candidate.Id).Distinct(StringComparer.Ordinal).Count() != request.Candidates.Count)
            throw new ArgumentException("Context candidate IDs must be unique.");
    }

    internal static void ValidateResponse(VoiceScenarioResolutionResponse response, ISet<string> allowedIds)
    {
        if (response.Confidence is < 0 or > 1 ||
            response.CandidateContextIds.Count > 2 ||
            response.CandidateContextIds.Any(id => !allowedIds.Contains(id)) ||
            (response.MatchedContextId is not null && !allowedIds.Contains(response.MatchedContextId)))
            throw new InvalidDataException("Voice scenario provider returned an invalid candidate reference.");

        switch (response.Decision)
        {
            case "published_context" when response.MatchedContextId is not null &&
                                                   response.CandidateContextIds.Count == 0 &&
                                                   response.NormalizedFreeContext is null:
            case "free_context" when response.MatchedContextId is null &&
                                            response.CandidateContextIds.Count == 0 &&
                                            !string.IsNullOrWhiteSpace(response.NormalizedFreeContext):
            case "clarify" when response.MatchedContextId is null &&
                                     response.NormalizedFreeContext is null:
            case "unsafe" when response.MatchedContextId is null &&
                                    response.CandidateContextIds.Count == 0 &&
                                    response.NormalizedFreeContext is null:
                return;
            default:
                throw new InvalidDataException("Voice scenario provider returned an invalid decision shape.");
        }
    }

    private static string ExtractOutputText(OpenAiResponsesResponse response) => response.Output
        .SelectMany(item => item.Content)
        .Select(item => item.Text)
        .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text))?.Trim()
        ?? throw new InvalidDataException("Voice scenario provider output text was empty.");
}
