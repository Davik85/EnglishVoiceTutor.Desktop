using System.Data.Common;
using System.Text.Json;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed class CmsPublishedContentService : ICmsPublishedContentService
{
    private static readonly JsonSerializerOptions ReadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppDbContext dbContext;
    private readonly CmsContentOptions options;
    private readonly ILogger<CmsPublishedContentService> logger;

    public CmsPublishedContentService(
        AppDbContext dbContext,
        IOptions<CmsContentOptions> options,
        ILogger<CmsPublishedContentService> logger)
    {
        this.dbContext = dbContext;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<CmsPublishedContentReadResult> ReadLatestPublishedContentAsync(CancellationToken cancellationToken)
    {
        var contentPackSlug = ResolveContentPackSlug();
        var fallbackToStaticJson = options.FallbackToStaticJson;
        var result = CreateBaseResult(contentPackSlug, fallbackToStaticJson);

        if (!options.ReadPublishedSnapshotEnabled)
        {
            return UseFallback(result, CmsContentConstants.ErrorCodes.ReadPathDisabled, "CMS published snapshot read path is disabled by configuration.");
        }

        try
        {
            var latest = await dbContext.ContentVersions
                .AsNoTracking()
                .Include(version => version.ContentPack)
                .Include(version => version.PublishedSnapshot)
                .Where(version => version.ContentPack.Slug == contentPackSlug)
                .Where(version => version.PublishStatus == CmsContentConstants.ContentVersionPublishStatuses.Published)
                .Where(version => version.PublishedSnapshot != null)
                .OrderByDescending(version => version.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (latest?.PublishedSnapshot is null)
            {
                return UseFallback(result, CmsContentConstants.ErrorCodes.NoPublishedSnapshot, $"No published CMS content snapshot exists for content pack '{contentPackSlug}'.");
            }

            result.VersionNumber = latest.VersionNumber;
            result.SnapshotHash = latest.PublishedSnapshot.SnapshotHash;
            result.ComputedSnapshotHash = CmsContentJson.Sha256Hex(latest.PublishedSnapshot.SnapshotJson);
            result.Summary.HashValid = string.Equals(result.SnapshotHash, result.ComputedSnapshotHash, StringComparison.Ordinal)
                && string.Equals(latest.SnapshotHash, result.ComputedSnapshotHash, StringComparison.Ordinal);

            if (!result.Summary.HashValid)
            {
                return UseFallback(result, CmsContentConstants.ErrorCodes.HashMismatch, "Published CMS content snapshot hash does not match the stored version hash.");
            }

            CmsPublishedLessonContent? content;
            try
            {
                content = JsonSerializer.Deserialize<CmsPublishedLessonContent>(latest.PublishedSnapshot.SnapshotJson, ReadJsonOptions);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Published CMS content snapshot could not be deserialized. ContentPackSlug={ContentPackSlug}; VersionNumber={VersionNumber}", contentPackSlug, latest.VersionNumber);
                return UseFallback(result, CmsContentConstants.ErrorCodes.DeserializationFailed, $"Published CMS content snapshot could not be deserialized: {ex.Message}");
            }

            if (content is null)
            {
                return UseFallback(result, CmsContentConstants.ErrorCodes.DeserializationFailed, "Published CMS content snapshot deserialized to an empty content object.");
            }

            result.Content = content;
            PopulateSummary(result, content);
            ValidateMappedContent(result, content);

            if (result.Errors.Count > 0)
            {
                result.Summary.ValidationPassed = false;
                return UseFallback(result, CmsContentConstants.ErrorCodes.ValidationFailed, "Published CMS content snapshot failed read-path validation.");
            }

            result.Success = true;
            result.ReadPublishedSnapshotEnabled = options.ReadPublishedSnapshotEnabled;
            result.Source = CmsContentConstants.Sources.CmsPublishedSnapshot;
            result.FallbackUsed = false;
            result.Summary.ValidationPassed = true;
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or NotSupportedException or JsonException)
        {
            logger.LogWarning(ex, "Published CMS content snapshot read failed. ContentPackSlug={ContentPackSlug}", contentPackSlug);
            return UseFallback(result, CmsContentConstants.ErrorCodes.ReadError, $"Published CMS content snapshot read failed: {ex.Message}");
        }
    }

    private static CmsPublishedContentReadResult CreateBaseResult(string contentPackSlug, bool fallbackToStaticJson)
    {
        return new CmsPublishedContentReadResult
        {
            Success = false,
            Source = CmsContentConstants.Sources.StaticJsonFallback,
            ReadPublishedSnapshotEnabled = false,
            FallbackToStaticJson = fallbackToStaticJson,
            FallbackUsed = fallbackToStaticJson,
            ContentPackSlug = contentPackSlug
        };
    }

    private CmsPublishedContentReadResult UseFallback(CmsPublishedContentReadResult result, string errorCode, string message)
    {
        result.ReadPublishedSnapshotEnabled = options.ReadPublishedSnapshotEnabled;
        result.Source = CmsContentConstants.Sources.StaticJsonFallback;
        result.FallbackUsed = options.FallbackToStaticJson;
        result.Success = options.FallbackToStaticJson;
        result.Content = null;
        result.Errors.Add($"{errorCode}: {message}");
        if (options.FallbackToStaticJson)
        {
            result.Warnings.Add(CmsContentConstants.WarningCodes.StaticJsonFallbackUsed);
        }

        return result;
    }

    private static void PopulateSummary(CmsPublishedContentReadResult result, CmsPublishedLessonContent content)
    {
        result.Summary.TopicCount = content.Topics.Count;
        result.Summary.ScenarioCount = content.Scenarios.Count;
        result.Summary.PromptTemplateCount = content.PromptTemplates.Count;
        result.Summary.TutorBehaviorProfileCount = content.TutorBehaviorProfiles.Count;
    }

    private static void ValidateMappedContent(CmsPublishedContentReadResult result, CmsPublishedLessonContent content)
    {
        if (content.Topics.Count == 0)
        {
            result.Errors.Add($"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published CMS content snapshot has no topics.");
        }

        if (content.Scenarios.Count == 0)
        {
            result.Errors.Add($"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published CMS content snapshot has no scenarios.");
        }

        if (content.PromptTemplates.Count == 0)
        {
            result.Errors.Add($"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published CMS content snapshot has no prompt templates.");
        }

        if (content.TutorBehaviorProfiles.Count == 0)
        {
            result.Errors.Add($"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published CMS content snapshot has no tutor behavior profiles.");
        }

        var topicKeys = content.Topics.Select(topic => topic.StableTopicKey).ToHashSet(StringComparer.Ordinal);
        foreach (var topic in content.Topics)
        {
            Require(topic.StableTopicKey, $"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: A published topic is missing its stable topic key.", result);
            Require(topic.Title, $"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published topic '{topic.StableTopicKey}' is missing its title.", result);
        }

        foreach (var scenario in content.Scenarios)
        {
            Require(scenario.StableScenarioKey, $"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: A published scenario is missing its stable scenario key.", result);
            Require(scenario.TopicKey, $"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published scenario '{scenario.StableScenarioKey}' is missing its topic key.", result);
            Require(scenario.Title, $"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published scenario '{scenario.StableScenarioKey}' is missing its title.", result);
            Require(scenario.Lesson.Id, $"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published scenario '{scenario.StableScenarioKey}' is missing its lesson runtime id.", result);
            Require(scenario.Lesson.LessonSetup.SetupMessage, $"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published scenario '{scenario.StableScenarioKey}' is missing its setup message.", result);

            if (!string.IsNullOrWhiteSpace(scenario.TopicKey) && !topicKeys.Contains(scenario.TopicKey))
            {
                result.Errors.Add($"{CmsContentConstants.ErrorCodes.ValidationFailed}: Published scenario '{scenario.StableScenarioKey}' references missing topic '{scenario.TopicKey}'.");
            }

            if (scenario.Lesson.Metadata.SupportedLevels.Count == 0)
            {
                result.Errors.Add($"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published scenario '{scenario.StableScenarioKey}' has no supported levels.");
            }
        }

        foreach (var template in content.PromptTemplates)
        {
            Require(template.TemplateKey, $"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: A published prompt template is missing its template key.", result);
            Require(template.Body, $"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published prompt template '{template.TemplateKey}' is empty.", result);
        }

        foreach (var tutor in content.TutorBehaviorProfiles)
        {
            Require(tutor.TutorId, $"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: A published tutor behavior profile is missing its tutor id.", result);
            Require(tutor.DisplayName, $"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published tutor behavior profile '{tutor.TutorId}' is missing display name.", result);
            if (tutor.TutorProfile.CommunicationStyle.Count == 0)
            {
                result.Errors.Add($"{CmsContentConstants.ErrorCodes.RequiredContentMissing}: Published tutor behavior profile '{tutor.TutorId}' has no communication style rules.");
            }
        }
    }

    private static void Require(string? value, string errorMessage, CmsPublishedContentReadResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result.Errors.Add(errorMessage);
        }
    }

    private string ResolveContentPackSlug()
    {
        return string.IsNullOrWhiteSpace(options.ContentPackSlug)
            ? CmsContentConstants.StaticImport.ContentPackSlug
            : options.ContentPackSlug.Trim();
    }
}
