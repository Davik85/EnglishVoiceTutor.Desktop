using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishVoiceTutor.Api.Contracts.Cms;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Desktop.Models.LessonContent;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed partial class CmsContentPublishingService(
    AppDbContext dbContext,
    ICmsContentValidationService validationService) : ICmsContentPublishingService
{
    public async Task<CmsContentVersionListResponse?> ListVersionsAsync(string slug, CancellationToken cancellationToken)
    {
        var pack = await FindContentPackAsync(slug, cancellationToken, asNoTracking: true);
        if (pack is null)
        {
            return null;
        }

        var versions = await dbContext.ContentVersions
            .AsNoTracking()
            .Include(version => version.ContentPack)
            .Include(version => version.PublishedSnapshot)
            .Include(version => version.RestoredFromVersion)
            .Where(version => version.ContentPackId == pack.Id)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);

        return new CmsContentVersionListResponse
        {
            Success = true,
            ContentPackSlug = pack.Slug,
            Count = versions.Count,
            Versions = versions.Select(MapVersion).ToList()
        };
    }

    public async Task<CmsContentVersionResponse?> GetVersionAsync(string slug, int versionNumber, CancellationToken cancellationToken)
    {
        var normalizedSlug = NormalizeRouteValue(slug);
        var version = await dbContext.ContentVersions
            .AsNoTracking()
            .Include(candidate => candidate.ContentPack)
            .Include(candidate => candidate.PublishedSnapshot)
            .Include(candidate => candidate.RestoredFromVersion)
            .Where(candidate => candidate.ContentPack.Slug == normalizedSlug)
            .Where(candidate => candidate.VersionNumber == versionNumber)
            .SingleOrDefaultAsync(cancellationToken);

        return version is null ? null : MapVersion(version);
    }

    public async Task<PublishCmsContentResponse?> PublishDraftAsync(string slug, PublishCmsContentRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pack = await FindContentPackAsync(slug, cancellationToken, asNoTracking: true);
        if (pack is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var validation = await validationService.ValidateDraftRowsForPublicationAsync(pack.Id, cancellationToken);
        var validationResponse = MapValidation(pack.Slug, validation, now);
        var response = new PublishCmsContentResponse
        {
            Success = validation.Success,
            ContentPackSlug = pack.Slug,
            Validation = validationResponse,
            CompletedAtUtc = now
        };

        if (!validation.Success)
        {
            response.Errors.Add("Draft validation failed. Resolve validation errors before publishing.");
            return response;
        }

        string snapshotJson;
        try
        {
            var latestBaseContent = await ReadLatestPublishedContentAsync(pack.Id, cancellationToken);
            snapshotJson = await CmsContentSnapshotBuilder.BuildSnapshotJsonFromDraftRowsAsync(dbContext, pack.Id, latestBaseContent, cancellationToken);
        }
        catch (JsonException ex)
        {
            response.Success = false;
            response.Errors.Add($"Draft snapshot could not be generated: {ex.Message}");
            return response;
        }

        var snapshotHash = CmsContentJson.Sha256Hex(snapshotJson);
        var latestVersion = await GetLatestPublishedVersionAsync(pack.Id, cancellationToken);
        response.VersionNumber = latestVersion?.VersionNumber;
        response.SnapshotHash = snapshotHash;
        response.PreviousSnapshotHash = latestVersion?.SnapshotHash;

        if (latestVersion is not null && string.Equals(latestVersion.SnapshotHash, snapshotHash, StringComparison.Ordinal))
        {
            response.Success = true;
            response.VersionNumber = latestVersion.VersionNumber;
            response.Skipped = true;
            response.NoChanges = true;
            response.Warnings.Add("No changes to publish. Draft snapshot hash matches the latest published version.");
            return response;
        }

        var changeSummary = SanitizeReason(request.ChangeSummary);
        if (string.IsNullOrWhiteSpace(changeSummary))
        {
            response.Success = false;
            response.Errors.Add("A changeSummary is required when publishing changed draft content.");
            return response;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var nextVersionNumber = await GetNextVersionNumberAsync(pack.Id, cancellationToken);
        var version = CreatePublishedVersion(pack.Id, nextVersionNumber, snapshotHash, validation, changeSummary, actorUserId, now, restoredFromVersionId: null);
        dbContext.ContentVersions.Add(version);
        dbContext.PublishedContentSnapshots.Add(CreateSnapshot(version.Id, snapshotJson, snapshotHash, now));
        dbContext.ContentAuditLogs.Add(CreateAudit(
            actorUserId,
            CmsContentConstants.ContentAuditActions.Published,
            nameof(ContentVersionEntity),
            version.Id,
            pack.Id,
            latestVersion?.SnapshotHash,
            snapshotHash,
            ["SnapshotHash", "VersionNumber", "PublishStatus"],
            changeSummary,
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.Success = true;
        response.Created = true;
        response.VersionNumber = version.VersionNumber;
        response.SnapshotHash = snapshotHash;
        return response;
    }

    public async Task<RestoreCmsContentVersionResponse?> RestoreVersionAsync(string slug, int versionNumber, RestoreCmsContentVersionRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedSlug = NormalizeRouteValue(slug);
        var sourceVersion = await dbContext.ContentVersions
            .AsNoTracking()
            .Include(version => version.ContentPack)
            .Include(version => version.PublishedSnapshot)
            .Where(version => version.ContentPack.Slug == normalizedSlug)
            .Where(version => version.VersionNumber == versionNumber)
            .SingleOrDefaultAsync(cancellationToken);

        if (sourceVersion is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var response = new RestoreCmsContentVersionResponse
        {
            Success = true,
            ContentPackSlug = sourceVersion.ContentPack.Slug,
            RestoredFromVersionNumber = sourceVersion.VersionNumber,
            RestoredFromVersionId = sourceVersion.Id,
            RestoredSnapshotHash = sourceVersion.SnapshotHash,
            CompletedAtUtc = now
        };

        if (sourceVersion.PublishedSnapshot is null)
        {
            response.Success = false;
            response.Errors.Add("Selected version does not have a published content snapshot.");
            return response;
        }

        var computedHash = CmsContentJson.Sha256Hex(sourceVersion.PublishedSnapshot.SnapshotJson);
        if (!string.Equals(sourceVersion.SnapshotHash, computedHash, StringComparison.Ordinal) ||
            !string.Equals(sourceVersion.PublishedSnapshot.SnapshotHash, computedHash, StringComparison.Ordinal))
        {
            response.Success = false;
            response.Errors.Add("Selected version snapshot hash is invalid.");
            return response;
        }

        CmsPublishedLessonContent content;
        try
        {
            content = CmsContentSnapshotBuilder.DeserializePublishedContent(sourceVersion.PublishedSnapshot.SnapshotJson);
        }
        catch (JsonException ex)
        {
            response.Success = false;
            response.Errors.Add($"Selected version snapshot could not be deserialized: {ex.Message}");
            return response;
        }

        var snapshotErrors = ValidateSnapshotContent(content);
        if (snapshotErrors.Count > 0)
        {
            response.Success = false;
            response.Errors.AddRange(snapshotErrors);
            return response;
        }

        var reason = SanitizeReason(request.Reason);
        if (string.IsNullOrWhiteSpace(reason))
        {
            response.Success = false;
            response.Errors.Add("A reason is required when restoring a content version.");
            return response;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await ReplaceDraftRowsFromSnapshotAsync(sourceVersion.ContentPack, content, now, cancellationToken);
        var validation = await validationService.ValidateDraftRowsAsync(sourceVersion.ContentPackId, cancellationToken);
        response.Validation = MapValidation(sourceVersion.ContentPack.Slug, validation, now);

        if (!validation.Success)
        {
            response.Success = false;
            response.Errors.Add("Restored draft validation failed. No restore operation was committed.");
            await transaction.RollbackAsync(cancellationToken);
            return response;
        }

        response.DraftRestored = true;
        var latestVersion = await GetLatestPublishedVersionAsync(sourceVersion.ContentPackId, cancellationToken);
        if (!request.PublishRestoredVersion)
        {
            dbContext.ContentAuditLogs.Add(CreateAudit(
                actorUserId,
                CmsContentConstants.ContentAuditActions.RestoreDraft,
                nameof(ContentVersionEntity),
                sourceVersion.Id,
                sourceVersion.ContentPackId,
                latestVersion?.SnapshotHash,
                sourceVersion.SnapshotHash,
                ["DraftRows", "RestoredFromVersionId"],
                reason,
                now));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response;
        }

        if (latestVersion is not null && string.Equals(latestVersion.SnapshotHash, sourceVersion.SnapshotHash, StringComparison.Ordinal))
        {
            response.Skipped = true;
            response.NoChanges = true;
            response.NewVersionNumber = latestVersion.VersionNumber;
            response.NewSnapshotHash = latestVersion.SnapshotHash;
            response.Warnings.Add("No rollback version was created because the selected snapshot already matches the latest published version.");
            dbContext.ContentAuditLogs.Add(CreateAudit(
                actorUserId,
                CmsContentConstants.ContentAuditActions.RestoreDraft,
                nameof(ContentVersionEntity),
                sourceVersion.Id,
                sourceVersion.ContentPackId,
                latestVersion.SnapshotHash,
                sourceVersion.SnapshotHash,
                ["DraftRows", "RestoredFromVersionId"],
                reason,
                now));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response;
        }

        var newVersionNumber = await GetNextVersionNumberAsync(sourceVersion.ContentPackId, cancellationToken);
        var version = CreatePublishedVersion(sourceVersion.ContentPackId, newVersionNumber, sourceVersion.SnapshotHash, validation, reason, actorUserId, now, sourceVersion.Id);
        dbContext.ContentVersions.Add(version);
        dbContext.PublishedContentSnapshots.Add(CreateSnapshot(version.Id, sourceVersion.PublishedSnapshot.SnapshotJson, sourceVersion.SnapshotHash, now));
        dbContext.ContentAuditLogs.Add(CreateAudit(
            actorUserId,
            CmsContentConstants.ContentAuditActions.RollbackPublished,
            nameof(ContentVersionEntity),
            version.Id,
            sourceVersion.ContentPackId,
            latestVersion?.SnapshotHash,
            sourceVersion.SnapshotHash,
            ["SnapshotHash", "VersionNumber", "PublishStatus", "RestoredFromVersionId"],
            reason,
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        response.PublishedNewVersion = true;
        response.NewVersionNumber = version.VersionNumber;
        response.NewSnapshotHash = version.SnapshotHash;
        return response;
    }


    private static List<string> ValidateSnapshotContent(CmsPublishedLessonContent content)
    {
        var errors = new List<string>();
        if (content.Topics.Count == 0)
        {
            errors.Add("Selected version snapshot has no topics.");
        }

        if (content.Scenarios.Count == 0)
        {
            errors.Add("Selected version snapshot has no scenarios.");
        }

        if (content.PromptTemplates.Count == 0)
        {
            errors.Add("Selected version snapshot has no prompt templates.");
        }

        if (content.TutorBehaviorProfiles.Count == 0)
        {
            errors.Add("Selected version snapshot has no tutor behavior profiles.");
        }

        var topicKeys = content.Topics.Select(topic => topic.StableTopicKey).ToHashSet(StringComparer.Ordinal);
        foreach (var scenario in content.Scenarios)
        {
            if (!topicKeys.Contains(scenario.TopicKey))
            {
                errors.Add($"Selected version snapshot scenario '{scenario.StableScenarioKey}' references missing topic '{scenario.TopicKey}'.");
            }
        }

        return errors;
    }

    private async Task ReplaceDraftRowsFromSnapshotAsync(ContentPackEntity pack, CmsPublishedLessonContent content, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var scenarios = await dbContext.CmsLessonScenarios.Where(scenario => scenario.ContentPackId == pack.Id).ToListAsync(cancellationToken);
        var topics = await dbContext.CmsLessonTopics.Where(topic => topic.ContentPackId == pack.Id).ToListAsync(cancellationToken);
        var promptTemplates = await dbContext.PromptTemplates.Where(template => template.ContentPackId == pack.Id).ToListAsync(cancellationToken);
        var tutorProfiles = await dbContext.TutorBehaviorProfiles.Where(profile => profile.ContentPackId == pack.Id).ToListAsync(cancellationToken);

        dbContext.CmsLessonScenarios.RemoveRange(scenarios);
        dbContext.CmsLessonTopics.RemoveRange(topics);
        dbContext.PromptTemplates.RemoveRange(promptTemplates);
        dbContext.TutorBehaviorProfiles.RemoveRange(tutorProfiles);
        await dbContext.SaveChangesAsync(cancellationToken);

        var topicEntities = content.Topics
            .OrderBy(topic => topic.SortOrder)
            .ThenBy(topic => topic.StableTopicKey, StringComparer.Ordinal)
            .Select(topic => new CmsLessonTopicEntity
            {
                Id = Guid.NewGuid(),
                ContentPackId = pack.Id,
                StableTopicKey = topic.StableTopicKey,
                Title = topic.Title,
                Description = topic.Description,
                SortOrder = topic.SortOrder,
                IsActive = topic.IsActive,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            })
            .ToList();

        dbContext.CmsLessonTopics.AddRange(topicEntities);
        await dbContext.SaveChangesAsync(cancellationToken);

        var topicIds = topicEntities.ToDictionary(topic => topic.StableTopicKey, topic => topic.Id, StringComparer.Ordinal);
        dbContext.CmsLessonScenarios.AddRange(content.Scenarios.OrderBy(scenario => scenario.StableScenarioKey, StringComparer.Ordinal).Select(scenario => MapScenario(pack.Id, topicIds[scenario.TopicKey], scenario, now)));
        dbContext.PromptTemplates.AddRange(content.PromptTemplates.OrderBy(template => template.TemplateKey, StringComparer.Ordinal).Select(template => new PromptTemplateEntity
        {
            Id = Guid.NewGuid(),
            ContentPackId = pack.Id,
            TemplateKey = template.TemplateKey,
            Body = template.Body,
            AllowedPlaceholdersJson = template.AllowedPlaceholdersJson,
            RequiredPlaceholdersJson = template.RequiredPlaceholdersJson,
            MaxLength = template.MaxLength,
            IsActive = template.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }));
        dbContext.TutorBehaviorProfiles.AddRange(content.TutorBehaviorProfiles.OrderBy(profile => profile.TutorId, StringComparer.Ordinal).Select(profile => new TutorBehaviorProfileEntity
        {
            Id = Guid.NewGuid(),
            ContentPackId = pack.Id,
            TutorId = profile.TutorId,
            DisplayName = profile.DisplayName,
            CommunicationStyleJson = CmsContentJson.SerializeDeterministic(new
            {
                profile.TutorProfile.CommunicationStyle,
                profile.TutorProfile.SpeakingRules
            }),
            SafetyNotesJson = CmsContentJson.SerializeDeterministic(new
            {
                profile.TutorProfile.IdentityRules
            }),
            IsActive = profile.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static CmsLessonScenarioEntity MapScenario(Guid contentPackId, Guid topicId, CmsPublishedLessonScenario publishedScenario, DateTimeOffset now)
    {
        var scenario = publishedScenario.Lesson;
        return new CmsLessonScenarioEntity
        {
            Id = Guid.NewGuid(),
            ContentPackId = contentPackId,
            TopicId = topicId,
            StableScenarioKey = publishedScenario.StableScenarioKey,
            Title = publishedScenario.Title,
            Description = publishedScenario.Description,
            LessonType = publishedScenario.LessonType,
            SupportedLevelIdsJson = CmsContentJson.SerializeDeterministic(scenario.Metadata.SupportedLevels),
            SetupMessage = scenario.LessonSetup.SetupMessage,
            ContextSelectionJson = CmsContentJson.SerializeDeterministic(scenario.LessonSetup.ContextSelection),
            LearningGoalJson = CmsContentJson.SerializeDeterministic(scenario.LearningGoal),
            SituationJson = CmsContentJson.SerializeDeterministic(scenario.Situation),
            RolesJson = CmsContentJson.SerializeDeterministic(scenario.Roles),
            TargetLanguageJson = CmsContentJson.SerializeDeterministic(scenario.TargetLanguage),
            LevelProfilesJson = CmsContentJson.SerializeDeterministic(scenario.LevelProfiles.OrderBy(profile => profile.Key, StringComparer.Ordinal).ToDictionary()),
            ConversationFlowJson = CmsContentJson.SerializeDeterministic(scenario.ConversationFlow),
            RoleplayBeatsJson = CmsContentJson.SerializeDeterministic(scenario.RoleplayBeats),
            ReciprocalQuestionHandlingJson = CmsContentJson.SerializeDeterministic(scenario.ReciprocalQuestionHandling),
            ExpectedScenarioProgressionJson = CmsContentJson.SerializeDeterministic(scenario.ExpectedScenarioProgression),
            ControlledVariationJson = CmsContentJson.SerializeDeterministic(scenario.ControlledVariation),
            OffTopicHandlingJson = CmsContentJson.SerializeDeterministic(scenario.OffTopicHandling),
            FeedbackRulesJson = CmsContentJson.SerializeDeterministic(scenario.FeedbackRules),
            HintRulesJson = CmsContentJson.SerializeDeterministic(scenario.HintRules),
            RepetitionLogicJson = CmsContentJson.SerializeDeterministic(scenario.RepetitionLogic),
            AiTutorPromptInstructionsJson = CmsContentJson.SerializeDeterministic(scenario.AiTutorPromptInstructions),
            DefinitionJson = string.IsNullOrWhiteSpace(publishedScenario.DefinitionJson)
                ? CmsScenarioDefinitionJson.SerializeDefinition(scenario)
                : publishedScenario.DefinitionJson.Trim(),
            SoftWrapUpAfterUserTurn = scenario.Metadata.SoftWrapUpAfterUserTurn,
            FinalMessageAtUserTurn = scenario.Metadata.FinalMessageAtUserTurn,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private static CmsContentVersionResponse MapVersion(ContentVersionEntity version)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var hashValid = false;
        var summary = new CmsContentVersionSnapshotSummaryResponse();

        if (version.PublishedSnapshot is null)
        {
            errors.Add("Version does not have a published content snapshot.");
        }
        else
        {
            var computedHash = CmsContentJson.Sha256Hex(version.PublishedSnapshot.SnapshotJson);
            hashValid = string.Equals(version.SnapshotHash, computedHash, StringComparison.Ordinal)
                && string.Equals(version.PublishedSnapshot.SnapshotHash, computedHash, StringComparison.Ordinal);

            if (!hashValid)
            {
                errors.Add("Version snapshot hash does not match stored snapshot content.");
            }

            try
            {
                var content = CmsContentSnapshotBuilder.DeserializePublishedContent(version.PublishedSnapshot.SnapshotJson);
                summary.Topics = content.Topics.Count;
                summary.Scenarios = content.Scenarios.Count;
                summary.PromptTemplates = content.PromptTemplates.Count;
                summary.TutorBehaviorProfiles = content.TutorBehaviorProfiles.Count;
            }
            catch (JsonException ex)
            {
                errors.Add($"Version snapshot could not be summarized: {ex.Message}");
            }
        }

        var validation = ParseValidation(version.ContentPack.Slug, version.ValidationSummaryJson, version.CreatedAtUtc, warnings);
        return new CmsContentVersionResponse
        {
            Id = version.Id,
            ContentPackId = version.ContentPackId,
            ContentPackSlug = version.ContentPack.Slug,
            VersionNumber = version.VersionNumber,
            SnapshotHash = version.SnapshotHash,
            PublishStatus = version.PublishStatus,
            PublishedByUserId = version.PublishedByUserId,
            PublishedAtUtc = version.PublishedAtUtc,
            ChangeSummary = version.ChangeSummary,
            RestoredFromVersionId = version.RestoredFromVersionId,
            RestoredFromVersionNumber = version.RestoredFromVersion?.VersionNumber,
            CreatedAtUtc = version.CreatedAtUtc,
            SnapshotHashValid = hashValid,
            Validation = validation,
            SnapshotSummary = summary,
            Errors = errors,
            Warnings = warnings
        };
    }


    private async Task<CmsPublishedLessonContent?> ReadLatestPublishedContentAsync(Guid contentPackId, CancellationToken cancellationToken)
    {
        var latest = await dbContext.ContentVersions
            .AsNoTracking()
            .Include(version => version.PublishedSnapshot)
            .Where(version => version.ContentPackId == contentPackId)
            .Where(version => version.PublishStatus == CmsContentConstants.ContentVersionPublishStatuses.Published)
            .Where(version => version.PublishedSnapshot != null)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest?.PublishedSnapshot is null)
        {
            return null;
        }

        var computedHash = CmsContentJson.Sha256Hex(latest.PublishedSnapshot.SnapshotJson);
        return string.Equals(latest.SnapshotHash, computedHash, StringComparison.Ordinal) &&
            string.Equals(latest.PublishedSnapshot.SnapshotHash, computedHash, StringComparison.Ordinal)
                ? CmsContentSnapshotBuilder.DeserializePublishedContent(latest.PublishedSnapshot.SnapshotJson)
                : null;
    }

    private async Task<ContentPackEntity?> FindContentPackAsync(string slug, CancellationToken cancellationToken, bool asNoTracking)
    {
        var normalizedSlug = NormalizeRouteValue(slug);
        var query = dbContext.ContentPacks.Where(pack => pack.Slug == normalizedSlug);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<ContentVersionEntity?> GetLatestPublishedVersionAsync(Guid contentPackId, CancellationToken cancellationToken)
    {
        return await dbContext.ContentVersions
            .AsNoTracking()
            .Where(version => version.ContentPackId == contentPackId)
            .Where(version => version.PublishStatus == CmsContentConstants.ContentVersionPublishStatuses.Published)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int> GetNextVersionNumberAsync(Guid contentPackId, CancellationToken cancellationToken)
    {
        var latestVersionNumber = await dbContext.ContentVersions
            .Where(version => version.ContentPackId == contentPackId)
            .MaxAsync(version => (int?)version.VersionNumber, cancellationToken);

        return latestVersionNumber.GetValueOrDefault() + 1;
    }

    private static ContentVersionEntity CreatePublishedVersion(Guid contentPackId, int versionNumber, string snapshotHash, CmsContentValidationResult validation, string changeSummary, Guid actorUserId, DateTimeOffset now, Guid? restoredFromVersionId)
    {
        return new ContentVersionEntity
        {
            Id = Guid.NewGuid(),
            ContentPackId = contentPackId,
            VersionNumber = versionNumber,
            SnapshotHash = snapshotHash,
            PublishStatus = CmsContentConstants.ContentVersionPublishStatuses.Published,
            PublishedByUserId = actorUserId,
            PublishedAtUtc = now,
            ValidationSummaryJson = SerializeValidation(validation),
            ChangeSummary = changeSummary,
            RestoredFromVersionId = restoredFromVersionId,
            CreatedAtUtc = now
        };
    }

    private static PublishedContentSnapshotEntity CreateSnapshot(Guid versionId, string snapshotJson, string snapshotHash, DateTimeOffset now)
    {
        return new PublishedContentSnapshotEntity
        {
            Id = Guid.NewGuid(),
            ContentVersionId = versionId,
            SnapshotJson = snapshotJson,
            SnapshotHash = snapshotHash,
            CreatedAtUtc = now
        };
    }

    private static ContentAuditLogEntity CreateAudit(Guid actorUserId, string action, string entityType, Guid entityId, Guid contentPackId, string? beforeHash, string? afterHash, IReadOnlyList<string> changedFields, string reason, DateTimeOffset now)
    {
        return new ContentAuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ContentPackId = contentPackId,
            BeforeHash = beforeHash,
            AfterHash = afterHash,
            ChangedFieldsJson = CmsContentJson.SerializeDeterministic(changedFields.OrderBy(field => field, StringComparer.Ordinal).ToArray()),
            Reason = reason,
            CreatedAtUtc = now
        };
    }

    private static CmsContentValidationResponse MapValidation(string contentPackSlug, CmsContentValidationResult validation, DateTimeOffset checkedAtUtc)
    {
        return new CmsContentValidationResponse
        {
            Success = validation.Success,
            ContentPackSlug = contentPackSlug,
            Counts = new CmsContentValidationCountsResponse
            {
                Topics = validation.Counts.Topics,
                Scenarios = validation.Counts.Scenarios,
                PromptTemplates = validation.Counts.PromptTemplates,
                TutorBehaviorProfiles = validation.Counts.TutorBehaviorProfiles
            },
            Errors = validation.Errors,
            Warnings = validation.Warnings,
            CheckedAtUtc = checkedAtUtc
        };
    }

    private static string SerializeValidation(CmsContentValidationResult validation)
    {
        return CmsContentJson.SerializeDeterministic(new
        {
            validation.Success,
            validation.Counts,
            Errors = validation.Errors.OrderBy(error => error, StringComparer.Ordinal).ToArray(),
            Warnings = validation.Warnings.OrderBy(warning => warning, StringComparer.Ordinal).ToArray()
        });
    }

    private static CmsContentValidationResponse ParseValidation(string contentPackSlug, string validationSummaryJson, DateTimeOffset checkedAtUtc, List<string> warnings)
    {
        try
        {
            var summary = JsonSerializer.Deserialize<CmsContentVersionValidationSummary>(validationSummaryJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (summary is null)
            {
                warnings.Add("Validation summary was empty.");
                return new CmsContentValidationResponse { ContentPackSlug = contentPackSlug, CheckedAtUtc = checkedAtUtc };
            }

            return new CmsContentValidationResponse
            {
                Success = summary.Success,
                ContentPackSlug = contentPackSlug,
                Counts = new CmsContentValidationCountsResponse
                {
                    Topics = summary.Counts.Topics,
                    Scenarios = summary.Counts.Scenarios,
                    PromptTemplates = summary.Counts.PromptTemplates,
                    TutorBehaviorProfiles = summary.Counts.TutorBehaviorProfiles
                },
                Errors = summary.Errors,
                Warnings = summary.Warnings,
                CheckedAtUtc = checkedAtUtc
            };
        }
        catch (JsonException ex)
        {
            warnings.Add($"Validation summary could not be parsed: {ex.Message}");
            return new CmsContentValidationResponse { ContentPackSlug = contentPackSlug, CheckedAtUtc = checkedAtUtc };
        }
    }

    private static string NormalizeRouteValue(string value) => value.Trim();

    private static string SanitizeReason(string? reason)
    {
        var trimmed = (reason ?? string.Empty).Trim();
        if (SecretLikeReasonPattern().IsMatch(trimmed))
        {
            return "[redacted]";
        }

        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }

    [GeneratedRegex("(?i)(sk-[a-z0-9_-]{20,}|api[_-]?key|bearer\\s+[a-z0-9._-]{20,}|password|token|secret)", RegexOptions.CultureInvariant)]
    private static partial Regex SecretLikeReasonPattern();

    private sealed class CmsContentVersionValidationSummary
    {
        public bool Success { get; set; }
        public CmsContentValidationCounts Counts { get; set; } = new();
        public List<string> Errors { get; set; } = [];
        public List<string> Warnings { get; set; } = [];
    }
}
