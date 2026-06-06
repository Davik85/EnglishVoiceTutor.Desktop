using System.Text.Json;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Desktop.Models.LessonContent;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed class CmsRuntimeLessonContentService : ICmsRuntimeLessonContentService
{
    private const int RequiredTopicCount = 6;
    private const int RequiredScenarioCount = 26;
    private const int RequiredPromptTemplateCount = 3;
    private const int RequiredTutorBehaviorProfileCount = 2;

    private static readonly JsonSerializerOptions ReadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlyDictionary<string, string> PromptTemplateFiles = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [CmsContentConstants.PromptTemplateKeys.LessonTutorBase] = CmsContentConstants.StaticImport.LessonTutorBasePromptFileName,
        [CmsContentConstants.PromptTemplateKeys.LessonSetupRules] = CmsContentConstants.StaticImport.LessonSetupRulesPromptFileName,
        [CmsContentConstants.PromptTemplateKeys.LessonResponseRules] = CmsContentConstants.StaticImport.LessonResponseRulesPromptFileName
    };

    private readonly ICmsPublishedContentService publishedContentService;
    private readonly CmsContentOptions options;
    private readonly ILogger<CmsRuntimeLessonContentService> logger;

    public CmsRuntimeLessonContentService(
        ICmsPublishedContentService publishedContentService,
        IOptions<CmsContentOptions> options,
        ILogger<CmsRuntimeLessonContentService> logger)
    {
        this.publishedContentService = publishedContentService;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<CmsRuntimeLessonContentReadResult> ReadRuntimeLessonContentAsync(CancellationToken cancellationToken)
    {
        var contentPackSlug = ResolveContentPackSlug();
        CmsRuntimeLessonContentReadResult result;
        if (!options.UsePublishedSnapshotForRuntime)
        {
            result = await ReadStaticJsonAsync(contentPackSlug, fallbackUsed: false, cancellationToken);
            LogRuntimeContentLoad(result);
            return result;
        }

        var publishedResult = await publishedContentService.ReadLatestPublishedContentAsync(cancellationToken);
        if (publishedResult.Success && publishedResult.Content is not null)
        {
            result = MapPublishedResult(publishedResult);
            ValidateRuntimeContent(result);
            if (result.Errors.Count == 0)
            {
                result.Success = true;
                result.Summary.ValidationPassed = true;
                LogRuntimeContentLoad(result);
                return result;
            }
        }
        else
        {
            result = MapPublishedResult(publishedResult);
        }

        result.Success = false;
        result.Summary.ValidationPassed = false;
        if (options.FallbackToStaticJson)
        {
            var fallback = await ReadStaticJsonAsync(contentPackSlug, fallbackUsed: true, cancellationToken);
            fallback.Warnings.AddRange(result.Warnings);
            fallback.Warnings.AddRange(result.Errors.Select(error => $"CMS runtime source error before fallback: {error}"));
            fallback.Warnings.Add("CMS runtime content was unavailable or invalid, so static JSON fallback was used.");
            logger.LogWarning(
                "CMS runtime content fallback used. ContentPackSlug={ContentPackSlug}; Source={Source}; VersionNumber={VersionNumber}; SnapshotHash={SnapshotHash}; ValidationPassed={ValidationPassed}; ErrorCount={ErrorCount}",
                contentPackSlug,
                result.Source,
                result.VersionNumber,
                result.SnapshotHash,
                result.Summary.ValidationPassed,
                result.Errors.Count);
            LogRuntimeContentLoad(fallback);
            return fallback;
        }

        result.FallbackUsed = false;
        result.FallbackToStaticJson = false;
        result.Errors.Add("CMS runtime content mode is enabled, but the published snapshot could not be served and static JSON fallback is disabled.");
        LogRuntimeContentLoad(result);
        return result;
    }

    private async Task<CmsRuntimeLessonContentReadResult> ReadStaticJsonAsync(string contentPackSlug, bool fallbackUsed, CancellationToken cancellationToken)
    {
        var content = await LoadStaticContentAsync(cancellationToken);
        var result = new CmsRuntimeLessonContentReadResult
        {
            Success = true,
            Source = CmsContentConstants.Sources.StaticJson,
            UsePublishedSnapshotForRuntime = options.UsePublishedSnapshotForRuntime,
            ReadPublishedSnapshotEnabled = options.ReadPublishedSnapshotEnabled,
            FallbackToStaticJson = options.FallbackToStaticJson,
            FallbackUsed = fallbackUsed,
            ContentPackSlug = contentPackSlug,
            Content = content,
            Summary = CreateSummary(content, hashValid: true)
        };
        ValidateRuntimeContent(result);
        result.Success = result.Errors.Count == 0;
        result.Summary.ValidationPassed = result.Success;
        return result;
    }

    private static CmsRuntimeLessonContentReadResult MapPublishedResult(CmsPublishedContentReadResult publishedResult)
    {
        var content = publishedResult.Content is null
            ? null
            : new CmsRuntimeLessonContent
            {
                Topics = publishedResult.Content.Topics,
                Scenarios = publishedResult.Content.Scenarios,
                PromptTemplates = publishedResult.Content.PromptTemplates,
                TutorBehaviorProfiles = publishedResult.Content.TutorBehaviorProfiles
            };

        return new CmsRuntimeLessonContentReadResult
        {
            Success = publishedResult.Success,
            Source = publishedResult.Source,
            UsePublishedSnapshotForRuntime = true,
            ReadPublishedSnapshotEnabled = publishedResult.ReadPublishedSnapshotEnabled,
            FallbackToStaticJson = publishedResult.FallbackToStaticJson,
            FallbackUsed = false,
            ContentPackSlug = publishedResult.ContentPackSlug,
            VersionNumber = publishedResult.VersionNumber,
            SnapshotHash = publishedResult.SnapshotHash,
            Content = content,
            Summary = new CmsRuntimeLessonContentSummary
            {
                TopicCount = publishedResult.Summary.TopicCount,
                ScenarioCount = publishedResult.Summary.ScenarioCount,
                PromptTemplateCount = publishedResult.Summary.PromptTemplateCount,
                TutorBehaviorProfileCount = publishedResult.Summary.TutorBehaviorProfileCount,
                HashValid = publishedResult.Summary.HashValid,
                ValidationPassed = publishedResult.Summary.ValidationPassed
            },
            Errors = [.. publishedResult.Errors],
            Warnings = [.. publishedResult.Warnings]
        };
    }

    private static CmsRuntimeLessonContentSummary CreateSummary(CmsRuntimeLessonContent content, bool hashValid)
    {
        return new CmsRuntimeLessonContentSummary
        {
            TopicCount = content.Topics.Count,
            ScenarioCount = content.Scenarios.Count,
            PromptTemplateCount = content.PromptTemplates.Count,
            TutorBehaviorProfileCount = content.TutorBehaviorProfiles.Count,
            HashValid = hashValid
        };
    }

    private static void ValidateRuntimeContent(CmsRuntimeLessonContentReadResult result)
    {
        if (result.Content is null)
        {
            result.Errors.Add("Runtime lesson content is empty.");
            return;
        }

        RequireCount(result.Summary.TopicCount, RequiredTopicCount, "topics", result);
        RequireCount(result.Summary.ScenarioCount, RequiredScenarioCount, "scenarios", result);
        RequireCount(result.Summary.PromptTemplateCount, RequiredPromptTemplateCount, "prompt templates", result);
        RequireCount(result.Summary.TutorBehaviorProfileCount, RequiredTutorBehaviorProfileCount, "tutor behavior profiles", result);

        foreach (var topic in result.Content.Topics)
        {
            Require(topic.StableTopicKey, "Runtime topic is missing StableTopicKey.", result);
            Require(topic.Title, $"Runtime topic '{topic.StableTopicKey}' is missing Title.", result);
        }

        foreach (var scenario in result.Content.Scenarios)
        {
            Require(scenario.StableScenarioKey, "Runtime scenario is missing StableScenarioKey.", result);
            Require(scenario.TopicKey, $"Runtime scenario '{scenario.StableScenarioKey}' is missing TopicKey.", result);
            Require(scenario.Title, $"Runtime scenario '{scenario.StableScenarioKey}' is missing Title.", result);
            Require(scenario.LessonType, $"Runtime scenario '{scenario.StableScenarioKey}' is missing LessonType.", result);
            Require(scenario.DefinitionJson, $"Runtime scenario '{scenario.StableScenarioKey}' is missing DefinitionJson.", result);
            Require(scenario.Lesson.Id, $"Runtime scenario '{scenario.StableScenarioKey}' is missing mapped lesson Id.", result);
            Require(scenario.Lesson.LessonSetup.SetupMessage, $"Runtime scenario '{scenario.StableScenarioKey}' is missing setup message.", result);
            if (scenario.Lesson.LessonSetup.FirstBotMessageShouldExplain.Count == 0)
            {
                result.Errors.Add($"Runtime scenario '{scenario.StableScenarioKey}' is missing first bot setup message rules.");
            }

            Require(scenario.Lesson.LearningGoal.Goal, $"Runtime scenario '{scenario.StableScenarioKey}' is missing learning goal text.", result);
        }

        foreach (var promptTemplate in result.Content.PromptTemplates)
        {
            Require(promptTemplate.TemplateKey, "Runtime prompt template is missing TemplateKey.", result);
            Require(promptTemplate.Body, $"Runtime prompt template '{promptTemplate.TemplateKey}' is missing Body.", result);
        }

        foreach (var tutor in result.Content.TutorBehaviorProfiles)
        {
            Require(tutor.TutorId, "Runtime tutor behavior profile is missing TutorId.", result);
            Require(tutor.DisplayName, $"Runtime tutor behavior profile '{tutor.TutorId}' is missing DisplayName.", result);
            if (tutor.TutorProfile.CommunicationStyle.Count == 0)
            {
                result.Errors.Add($"Runtime tutor behavior profile '{tutor.TutorId}' has no communication style rules.");
            }
        }
    }

    private static void RequireCount(int actual, int expected, string label, CmsRuntimeLessonContentReadResult result)
    {
        if (actual != expected)
        {
            result.Errors.Add($"Runtime content expected {expected} {label}, but found {actual}.");
        }
    }

    private static void Require(string? value, string errorMessage, CmsRuntimeLessonContentReadResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result.Errors.Add(errorMessage);
        }
    }

    private static async Task<CmsRuntimeLessonContent> LoadStaticContentAsync(CancellationToken cancellationToken)
    {
        var contentRoot = Path.Combine(AppContext.BaseDirectory, CmsContentConstants.StaticImport.ContentRootFolder);
        var lessonsRoot = Path.Combine(contentRoot, CmsContentConstants.StaticImport.LessonsFolder);
        var promptsRoot = Path.Combine(contentRoot, CmsContentConstants.StaticImport.PromptsFolder);
        var tutorsRoot = Path.Combine(contentRoot, CmsContentConstants.StaticImport.TutorsFolder);
        var content = new CmsRuntimeLessonContent();
        var topicOrder = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var scenarioFile in Directory.EnumerateFiles(lessonsRoot, "*.json", SearchOption.AllDirectories).OrderBy(path => Path.GetRelativePath(lessonsRoot, path), StringComparer.Ordinal))
        {
            var scenario = await ReadJsonFileAsync<LessonScenario>(scenarioFile, cancellationToken);
            var topicTitle = scenario.Metadata.Topic.Trim();
            var topicKey = Slugify(topicTitle);
            if (!topicOrder.ContainsKey(topicKey))
            {
                topicOrder[topicKey] = topicOrder.Count + 1;
                content.Topics.Add(new CmsPublishedLessonTopic
                {
                    StableTopicKey = topicKey,
                    Title = topicTitle,
                    Description = string.Empty,
                    SortOrder = topicOrder[topicKey],
                    IsActive = true
                });
            }

            content.Scenarios.Add(new CmsPublishedLessonScenario
            {
                StableScenarioKey = scenario.Id.Trim(),
                TopicKey = topicKey,
                Title = scenario.Metadata.Subtopic.Trim(),
                Description = scenario.Situation.Description.Trim(),
                LessonType = scenario.Metadata.LessonType.Trim(),
                DefinitionJson = CmsScenarioDefinitionJson.SerializeDefinition(scenario),
                Lesson = scenario
            });
        }

        foreach (var (templateKey, fileName) in PromptTemplateFiles.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            content.PromptTemplates.Add(new CmsPublishedPromptTemplate
            {
                TemplateKey = templateKey,
                Body = await File.ReadAllTextAsync(Path.Combine(promptsRoot, fileName), cancellationToken),
                AllowedPlaceholdersJson = CmsContentJson.EmptyArrayJson,
                RequiredPlaceholdersJson = CmsContentJson.EmptyArrayJson,
                IsActive = true
            });
        }

        foreach (var tutorPath in Directory.EnumerateFiles(tutorsRoot, "*.json", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.Ordinal))
        {
            var tutor = await ReadJsonFileAsync<TutorProfile>(tutorPath, cancellationToken);
            content.TutorBehaviorProfiles.Add(new CmsPublishedTutorBehaviorProfile
            {
                TutorId = tutor.Id.Trim(),
                DisplayName = tutor.DisplayName.Trim(),
                IsActive = true,
                TutorProfile = tutor
            });
        }

        return content;
    }

    private static async Task<T> ReadJsonFileAsync<T>(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var value = await JsonSerializer.DeserializeAsync<T>(stream, ReadJsonOptions, cancellationToken);
        if (value is null)
        {
            throw new JsonException($"Static JSON content could not be deserialized: {filePath}");
        }

        return value;
    }

    private void LogRuntimeContentLoad(CmsRuntimeLessonContentReadResult result)
    {
        logger.LogInformation(
            "Runtime lesson content loaded. Source={Source}; ContentPackSlug={ContentPackSlug}; VersionNumber={VersionNumber}; SnapshotHash={SnapshotHash}; FallbackUsed={FallbackUsed}; ValidationPassed={ValidationPassed}; TopicCount={TopicCount}; ScenarioCount={ScenarioCount}; PromptTemplateCount={PromptTemplateCount}; TutorBehaviorProfileCount={TutorBehaviorProfileCount}; ErrorCount={ErrorCount}; WarningCount={WarningCount}",
            result.Source,
            result.ContentPackSlug,
            result.VersionNumber,
            result.SnapshotHash,
            result.FallbackUsed,
            result.Summary.ValidationPassed,
            result.Summary.TopicCount,
            result.Summary.ScenarioCount,
            result.Summary.PromptTemplateCount,
            result.Summary.TutorBehaviorProfileCount,
            result.Errors.Count,
            result.Warnings.Count);
    }

    private string ResolveContentPackSlug()
    {
        return string.IsNullOrWhiteSpace(options.ContentPackSlug)
            ? CmsContentConstants.StaticImport.ContentPackSlug
            : options.ContentPackSlug.Trim();
    }

    private static string Slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        var slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return string.IsNullOrWhiteSpace(slug) ? "topic" : slug;
    }
}
