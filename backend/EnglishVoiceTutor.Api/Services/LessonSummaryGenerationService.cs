using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services.Cms;
using EnglishVoiceTutor.Api.Services.Usage;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services;

/// <summary>Creates one learner-safe summary from server-persisted lesson state.</summary>
public sealed class LessonSummaryGenerationService(
    AppDbContext dbContext,
    OpenAiOptionsProvider optionsProvider,
    IHttpClientFactory httpClientFactory,
    ICmsRuntimeLessonContentService runtimeContentService,
    IUsageEventService usageEventService,
    ILogger<LessonSummaryGenerationService> logger) : ILessonSummaryGenerationService
{
    private const int MaxTranscriptCharacters = 24000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>("""
    {"type":"object","additionalProperties":false,"properties":{"summary":{"type":"string"},"strengths":{"type":"array","items":{"type":"string"}},"improvements":{"type":"array","items":{"type":"string"}},"vocabulary":{"type":"array","items":{"type":"string"}},"grammar":{"type":"array","items":{"type":"string"}},"nextSteps":{"type":"array","items":{"type":"string"}}},"required":["summary","strengths","improvements","vocabulary","grammar","nextSteps"]}
    """);

    public async Task TryGenerateForFinishedSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.LessonSessions
            .Include(item => item.Summary)
            .SingleOrDefaultAsync(item => item.Id == sessionId, cancellationToken);
        if (session is null || session.Summary is not null || !string.Equals(session.Status, LessonSessionConstants.FinishedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var messages = await dbContext.LessonMessages.Where(item => item.SessionId == sessionId)
            .OrderBy(item => item.TurnNumber).ThenBy(item => item.CreatedAt).ToListAsync(cancellationToken);
        if (messages.Count == 0)
        {
            await RecordUsageAsync(session, UsageConstants.Statuses.Skipped, null, null, cancellationToken);
            logger.LogInformation("Lesson summary unavailable because no persisted messages exist. SessionId={SessionId}.", sessionId);
            return;
        }

        var options = optionsProvider.GetOptions();
        var selectedModel = options.LessonTutorChatModel;
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            await RecordUsageAsync(session, UsageConstants.Statuses.Skipped, selectedModel, null, cancellationToken);
            logger.LogInformation("Lesson summary unavailable because summary generation is not configured. SessionId={SessionId}.", sessionId);
            return;
        }

        try
        {
            var runtimeGoal = await GetSafeRuntimeGoalAsync(session.LessonContentId, cancellationToken);
            var request = new OpenAiResponsesRequest
            {
                Model = selectedModel,
                Instructions = "Create a concise, encouraging learner lesson summary. Use only the supplied lesson metadata and transcript. Do not mention prompts, providers, internal systems, or unsupported facts. Give specific but gentle feedback in the lesson study language when possible. Return only the required JSON.",
                Input = BuildInput(session, messages, runtimeGoal),
                Text = new OpenAiTextOptions { Format = new OpenAiTextFormat { Type = OpenAiConstants.JsonSchemaFormatType, Name = "lesson_summary_response", Strict = true, Schema = Schema } },
                Temperature = AiTextModelTemperaturePolicy.Resolve(AiTextModelRole.LessonTutorChat, selectedModel, options.LessonTutorChatOmitTemperature)
            };
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiConstants.ResponsesEndpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(OpenAiConstants.AuthorizationScheme, options.ApiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, OpenAiConstants.ContentTypeJson);
            using var response = await httpClientFactory.CreateClient().SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new HttpRequestException("Lesson summary provider request failed.", null, response.StatusCode);

            var providerResponse = JsonSerializer.Deserialize<OpenAiResponsesResponse>(await response.Content.ReadAsStringAsync(cancellationToken), JsonOptions)
                ?? throw new InvalidOperationException("Lesson summary provider response is empty.");
            var outputText = ExtractOutputText(providerResponse);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                await RecordUsageAsync(session, UsageConstants.Statuses.Failed, selectedModel, null, cancellationToken);
                logger.LogWarning("Lesson summary generation unavailable. SessionId={SessionId}; Category={Category}.", sessionId, "empty_provider_output");
                return;
            }

            var generated = JsonSerializer.Deserialize<GeneratedLessonSummary>(outputText, JsonOptions)
                ?? throw new InvalidOperationException("Lesson summary provider response is invalid.");
            if (string.IsNullOrWhiteSpace(generated.Summary)) throw new InvalidOperationException("Lesson summary provider response is incomplete.");

            var now = DateTimeOffset.UtcNow;
            dbContext.LessonSummaries.Add(new LessonSummaryEntity
            {
                Id = Guid.NewGuid(), SessionId = session.Id, Summary = generated.Summary.Trim(),
                Strengths = Join(generated.Strengths), Improvements = Join(generated.Improvements), Vocabulary = Join(generated.Vocabulary),
                Grammar = Join(generated.Grammar), NextSteps = Join(generated.NextSteps), CreatedAt = now, UpdatedAt = now
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await RecordUsageAsync(session, UsageConstants.Statuses.Success, selectedModel, providerResponse.Usage, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            await RecordUsageAsync(session, UsageConstants.Statuses.Failed, selectedModel, null, CancellationToken.None);
            logger.LogWarning(exception, "Lesson summary generation failed. SessionId={SessionId}; ErrorType={ErrorType}.", sessionId, exception.GetType().Name);
        }
    }

    private async Task<string?> GetSafeRuntimeGoalAsync(string lessonContentId, CancellationToken cancellationToken)
    {
        try
        {
            var runtime = await runtimeContentService.ReadRuntimeLessonContentAsync(cancellationToken);
            return runtime.Content?.Scenarios.FirstOrDefault(s => string.Equals(s.StableScenarioKey, lessonContentId, StringComparison.Ordinal))?.Lesson.LearningGoal.Goal?.Trim();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { logger.LogWarning(exception, "Lesson runtime metadata was unavailable for summary generation."); return null; }
    }

    private static string BuildInput(LessonSessionEntity session, IReadOnlyList<LessonMessageEntity> messages, string? runtimeGoal)
    {
        var transcript = string.Join("\n", messages.Select(message => $"{message.Role}: {message.Text.Trim()}"));
        if (transcript.Length > MaxTranscriptCharacters) transcript = transcript[^MaxTranscriptCharacters..];
        return $"Lesson metadata:\nStudy language: {session.StudyLanguage}\nTopic: {session.TopicTitle}\nSubtopic: {session.SubtopicTitle}\nLevel: {session.Level}\nContext: {session.SelectedContextTitle}\nGoal: {runtimeGoal}\n\nPersisted transcript:\n{transcript}";
    }

    private async Task RecordUsageAsync(LessonSessionEntity session, string status, string? model, OpenAiResponseUsage? usage, CancellationToken cancellationToken) =>
        await usageEventService.TryRecordAsync(new UsageEventRecord { UserId = session.UserId, SessionId = session.Id, Operation = UsageConstants.Operations.LessonSummary, Model = model, StudyLanguage = session.StudyLanguage, Status = status, InputTokens = usage?.InputTokens, OutputTokens = usage?.OutputTokens }, cancellationToken);

    private static string ExtractOutputText(OpenAiResponsesResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.OutputText))
        {
            return response.OutputText.Trim();
        }

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

    private static string? Join(IReadOnlyList<string>? values) => values is null ? null : string.Join("\n", values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
    private sealed class GeneratedLessonSummary { public string Summary { get; init; } = string.Empty; public IReadOnlyList<string> Strengths { get; init; } = []; public IReadOnlyList<string> Improvements { get; init; } = []; public IReadOnlyList<string> Vocabulary { get; init; } = []; public IReadOnlyList<string> Grammar { get; init; } = []; public IReadOnlyList<string> NextSteps { get; init; } = []; }
}
