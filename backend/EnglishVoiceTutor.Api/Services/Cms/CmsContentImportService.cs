using System.Text.Json;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Desktop.Models.LessonContent;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed class CmsContentImportService(
    AppDbContext dbContext,
    ICmsContentValidationService validationService) : ICmsContentImportService
{
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

    public async Task<CmsContentImportResult> ImportStaticContentAsync(Guid? actorUserId, CancellationToken cancellationToken)
    {
        var result = new CmsContentImportResult
        {
            ContentPackSlug = CmsContentConstants.StaticImport.ContentPackSlug,
            ContentPackName = CmsContentConstants.StaticImport.ContentPackName
        };

        CmsStaticContentImportDraft draft;
        try
        {
            draft = await LoadDraftAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            result.Errors.Add(ex.Message);
            return result;
        }

        result.Warnings.AddRange(draft.Warnings);
        result.Counts.TopicsRead = draft.Topics.Count;
        result.Counts.ScenariosRead = draft.Scenarios.Count;
        result.Counts.PromptTemplatesRead = draft.PromptTemplates.Count;
        result.Counts.TutorBehaviorProfilesRead = draft.TutorBehaviorProfiles.Count;

        var validation = validationService.Validate(draft);
        result.Warnings.AddRange(validation.Warnings);
        if (!validation.Success)
        {
            result.Errors.AddRange(validation.Errors);
            return result;
        }

        var snapshotJson = BuildSnapshotJson(draft);
        var snapshotHash = CmsContentJson.Sha256Hex(snapshotJson);
        result.SnapshotHash = snapshotHash;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var auditEntries = new List<ContentAuditLogEntity>();
        var pack = await dbContext.ContentPacks.SingleOrDefaultAsync(
            candidate => candidate.Slug == CmsContentConstants.StaticImport.ContentPackSlug,
            cancellationToken);

        if (pack is null)
        {
            pack = new ContentPackEntity
            {
                Id = Guid.NewGuid(),
                Slug = CmsContentConstants.StaticImport.ContentPackSlug,
                Name = CmsContentConstants.StaticImport.ContentPackName,
                Description = CmsContentConstants.StaticImport.ContentPackDescription,
                Status = CmsContentConstants.ContentPackStatuses.Published,
                BaseStaticContentVersion = CmsContentConstants.StaticImport.BaseStaticContentVersion,
                CreatedByUserId = actorUserId,
                UpdatedByUserId = actorUserId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.ContentPacks.Add(pack);
            AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportCreated, nameof(ContentPackEntity), pack.Id, pack.Id, null, HashPack(pack), ["Slug", "Name", "Description", "Status"]);
        }
        else
        {
            result.ContentPackId = pack.Id;
            var beforeHash = HashPack(pack);
            var changedFields = new List<string>();
            SetIfChanged(pack.Name, CmsContentConstants.StaticImport.ContentPackName, changedFields, value => pack.Name = value, nameof(pack.Name));
            SetIfChanged(pack.Description, CmsContentConstants.StaticImport.ContentPackDescription, changedFields, value => pack.Description = value, nameof(pack.Description));
            SetIfChanged(pack.Status, CmsContentConstants.ContentPackStatuses.Published, changedFields, value => pack.Status = value, nameof(pack.Status));
            SetIfChanged(pack.BaseStaticContentVersion, CmsContentConstants.StaticImport.BaseStaticContentVersion, changedFields, value => pack.BaseStaticContentVersion = value, nameof(pack.BaseStaticContentVersion));
            if (changedFields.Count > 0)
            {
                pack.UpdatedByUserId = actorUserId;
                pack.UpdatedAtUtc = now;
                AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportUpdated, nameof(ContentPackEntity), pack.Id, pack.Id, beforeHash, HashPack(pack), changedFields);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        result.ContentPackId = pack.Id;

        await UpsertTopicsAsync(pack.Id, actorUserId, draft, result, auditEntries, now, cancellationToken);
        await UpsertScenariosAsync(pack.Id, actorUserId, draft, result, auditEntries, now, cancellationToken);
        await UpsertPromptTemplatesAsync(pack.Id, actorUserId, draft, result, auditEntries, now, cancellationToken);
        await UpsertTutorProfilesAsync(pack.Id, actorUserId, draft, result, auditEntries, now, cancellationToken);
        await PublishSnapshotIfChangedAsync(pack.Id, actorUserId, snapshotJson, snapshotHash, validation, result, auditEntries, now, cancellationToken);

        dbContext.ContentAuditLogs.AddRange(auditEntries);
        result.Counts.AuditLogEntriesCreated = auditEntries.Count;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        result.IdempotentNoChanges = result.Counts.TopicsCreated == 0
            && result.Counts.TopicsUpdated == 0
            && result.Counts.ScenariosCreated == 0
            && result.Counts.ScenariosUpdated == 0
            && result.Counts.PromptTemplatesCreated == 0
            && result.Counts.PromptTemplatesUpdated == 0
            && result.Counts.TutorBehaviorProfilesCreated == 0
            && result.Counts.TutorBehaviorProfilesUpdated == 0
            && result.Counts.ContentVersionsCreated == 0
            && result.Counts.PublishedSnapshotsCreated == 0;
        result.Success = true;
        return result;
    }

    public async Task<CmsContentImportResult> InitializeStaticJsonV1DraftAsync(Guid? actorUserId, CancellationToken cancellationToken)
    {
        var result = new CmsContentImportResult
        {
            ContentPackSlug = CmsContentConstants.StaticImport.ContentPackSlug,
            ContentPackName = CmsContentConstants.StaticImport.ContentPackName,
            RuntimeUnchanged = true
        };

        CmsStaticContentImportDraft draft;
        try
        {
            draft = await LoadDraftAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            result.Errors.Add(ex.Message);
            return result;
        }

        result.Warnings.AddRange(draft.Warnings);
        result.Counts.TopicsRead = draft.Topics.Count;
        result.Counts.ScenariosRead = draft.Scenarios.Count;
        result.Counts.PromptTemplatesRead = draft.PromptTemplates.Count;
        result.Counts.TutorBehaviorProfilesRead = draft.TutorBehaviorProfiles.Count;

        var validation = validationService.Validate(draft);
        result.Warnings.AddRange(validation.Warnings);
        if (!validation.Success)
        {
            result.Errors.AddRange(validation.Errors);
            return result;
        }

        result.SnapshotHash = CmsContentJson.Sha256Hex(BuildSnapshotJson(draft));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var auditEntries = new List<ContentAuditLogEntity>();
        var pack = await dbContext.ContentPacks.SingleOrDefaultAsync(
            candidate => candidate.Slug == CmsContentConstants.StaticImport.ContentPackSlug,
            cancellationToken);

        if (pack is null)
        {
            pack = new ContentPackEntity
            {
                Id = Guid.NewGuid(),
                Slug = CmsContentConstants.StaticImport.ContentPackSlug,
                Name = CmsContentConstants.StaticImport.ContentPackName,
                Description = CmsContentConstants.StaticImport.ContentPackDescription,
                Status = CmsContentConstants.ContentPackStatuses.Draft,
                BaseStaticContentVersion = CmsContentConstants.StaticImport.BaseStaticContentVersion,
                CreatedByUserId = actorUserId,
                UpdatedByUserId = actorUserId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.ContentPacks.Add(pack);
            result.ContentPackCreated = true;
            result.Messages.Add("Content pack initialized.");
            AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportCreated, nameof(ContentPackEntity), pack.Id, pack.Id, null, HashPack(pack), ["Slug", "Name", "Description", "Status"]);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            result.ContentPackAlreadyExisted = true;
            result.Messages.Add("Content pack already exists.");
        }

        result.ContentPackId = pack.Id;

        var hasDraftContent = await HasAnyDraftContentAsync(pack.Id, cancellationToken);
        if (hasDraftContent)
        {
            result.DraftPreserved = true;
            result.Counts.TopicsSkipped = result.Counts.TopicsRead;
            result.Counts.ScenariosSkipped = result.Counts.ScenariosRead;
            result.Counts.PromptTemplatesSkipped = result.Counts.PromptTemplatesRead;
            result.Counts.TutorBehaviorProfilesSkipped = result.Counts.TutorBehaviorProfilesRead;
            result.Messages.Add("Draft preserved.");
            result.Messages.Add("Replacing an existing draft requires an explicit future confirmation flow; no CMS draft rows were overwritten.");
        }
        else
        {
            await UpsertTopicsAsync(pack.Id, actorUserId, draft, result, auditEntries, now, cancellationToken);
            await UpsertScenariosAsync(pack.Id, actorUserId, draft, result, auditEntries, now, cancellationToken);
            await UpsertPromptTemplatesAsync(pack.Id, actorUserId, draft, result, auditEntries, now, cancellationToken);
            await UpsertTutorProfilesAsync(pack.Id, actorUserId, draft, result, auditEntries, now, cancellationToken);
            result.DraftInitialized = true;
            result.Messages.Add("CMS draft content initialized from static JSON.");
        }

        result.Messages.Add("Learner runtime was not changed; static JSON remains the default until CmsContent__UsePublishedSnapshotForRuntime=true is intentionally enabled.");
        result.PublishedSnapshotCreated = false;
        result.Counts.AuditLogEntriesCreated = auditEntries.Count;
        dbContext.ContentAuditLogs.AddRange(auditEntries);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        result.IdempotentNoChanges = !result.ContentPackCreated && !result.DraftInitialized;
        result.Success = true;
        return result;
    }

    private async Task<bool> HasAnyDraftContentAsync(Guid contentPackId, CancellationToken cancellationToken)
    {
        return await dbContext.CmsLessonTopics.AnyAsync(topic => topic.ContentPackId == contentPackId, cancellationToken)
            || await dbContext.CmsLessonScenarios.AnyAsync(scenario => scenario.ContentPackId == contentPackId, cancellationToken)
            || await dbContext.PromptTemplates.AnyAsync(template => template.ContentPackId == contentPackId, cancellationToken)
            || await dbContext.TutorBehaviorProfiles.AnyAsync(tutor => tutor.ContentPackId == contentPackId, cancellationToken);
    }

    private static async Task<CmsStaticContentImportDraft> LoadDraftAsync(CancellationToken cancellationToken)
    {
        var contentRoot = Path.Combine(AppContext.BaseDirectory, CmsContentConstants.StaticImport.ContentRootFolder);
        var lessonsRoot = Path.Combine(contentRoot, CmsContentConstants.StaticImport.LessonsFolder);
        var promptsRoot = Path.Combine(contentRoot, CmsContentConstants.StaticImport.PromptsFolder);
        var tutorsRoot = Path.Combine(contentRoot, CmsContentConstants.StaticImport.TutorsFolder);
        var studyLanguagesPath = Path.Combine(
            contentRoot,
            CmsContentConstants.StaticImport.StudyLanguagesFolder,
            CmsContentConstants.StaticImport.StudyLanguagesFileName);

        if (!Directory.Exists(lessonsRoot))
        {
            throw new DirectoryNotFoundException($"Lesson content folder was not found: {lessonsRoot}");
        }

        var draft = new CmsStaticContentImportDraft
        {
            ContentRootPath = contentRoot
        };

        var scenarioFiles = Directory.EnumerateFiles(lessonsRoot, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetRelativePath(lessonsRoot, path), StringComparer.Ordinal)
            .ToArray();

        var topicOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var scenarioFile in scenarioFiles)
        {
            var scenario = await ReadJsonFileAsync<LessonScenario>(scenarioFile, cancellationToken);
            var topicTitle = scenario.Metadata.Topic.Trim();
            var topicKey = Slugify(topicTitle);
            if (!topicOrder.ContainsKey(topicKey))
            {
                topicOrder[topicKey] = topicOrder.Count + 1;
                draft.Topics.Add(new CmsStaticTopicDraft
                {
                    StableTopicKey = topicKey,
                    Title = topicTitle,
                    Description = string.Empty,
                    SortOrder = topicOrder[topicKey],
                    IsActive = true
                });
            }

            draft.Scenarios.Add(new CmsStaticScenarioDraft
            {
                StableScenarioKey = scenario.Id.Trim(),
                TopicKey = topicKey,
                Title = scenario.Metadata.Subtopic.Trim(),
                Description = scenario.Situation.Description.Trim(),
                LessonType = scenario.Metadata.LessonType.Trim(),
                Scenario = scenario,
                DefinitionJson = CmsScenarioDefinitionJson.SerializeDefinition(scenario)
            });
        }

        foreach (var (templateKey, fileName) in PromptTemplateFiles.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var promptPath = Path.Combine(promptsRoot, fileName);
            var body = await File.ReadAllTextAsync(promptPath, cancellationToken);
            draft.PromptTemplates.Add(new CmsStaticPromptTemplateDraft
            {
                TemplateKey = templateKey,
                Body = body,
                AllowedPlaceholdersJson = CmsContentJson.EmptyArrayJson,
                RequiredPlaceholdersJson = CmsContentJson.EmptyArrayJson,
                IsActive = true
            });
        }

        foreach (var tutorPath in Directory.EnumerateFiles(tutorsRoot, "*.json", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.Ordinal))
        {
            var tutor = await ReadJsonFileAsync<TutorProfile>(tutorPath, cancellationToken);
            draft.TutorBehaviorProfiles.Add(new CmsStaticTutorProfileDraft
            {
                TutorId = tutor.Id.Trim(),
                DisplayName = tutor.DisplayName.Trim(),
                TutorProfile = tutor,
                IsActive = true
            });
        }

        if (File.Exists(studyLanguagesPath))
        {
            using var stream = File.OpenRead(studyLanguagesPath);
            var studyLanguageDocuments = await JsonSerializer.DeserializeAsync<List<StudyLanguageDocument>>(stream, ReadJsonOptions, cancellationToken) ?? [];
            draft.StudyLanguageIds.AddRange(studyLanguageDocuments.Select(language => language.Id));
        }
        else
        {
            draft.Warnings.Add($"Static study language reference file was not found: {studyLanguagesPath}");
        }

        draft.Warnings.Add("Topic descriptions are not available in current lesson JSON, so imported CMS topics have empty descriptions.");
        draft.Warnings.Add("Hint, feedback, summary, translation, and core safety prompt behavior remains code-owned unless backed by current prompt files.");
        return draft;
    }

    private async Task UpsertTopicsAsync(Guid contentPackId, Guid? actorUserId, CmsStaticContentImportDraft draft, CmsContentImportResult result, List<ContentAuditLogEntity> auditEntries, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await dbContext.CmsLessonTopics
            .Where(topic => topic.ContentPackId == contentPackId)
            .ToDictionaryAsync(topic => topic.StableTopicKey, StringComparer.Ordinal, cancellationToken);

        foreach (var topicDraft in draft.Topics.OrderBy(topic => topic.SortOrder))
        {
            if (!existing.TryGetValue(topicDraft.StableTopicKey, out var topic))
            {
                topic = new CmsLessonTopicEntity
                {
                    Id = Guid.NewGuid(),
                    ContentPackId = contentPackId,
                    StableTopicKey = topicDraft.StableTopicKey,
                    Title = topicDraft.Title,
                    Description = topicDraft.Description,
                    SortOrder = topicDraft.SortOrder,
                    IsActive = topicDraft.IsActive,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                dbContext.CmsLessonTopics.Add(topic);
                result.Counts.TopicsCreated++;
                AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportCreated, nameof(CmsLessonTopicEntity), topic.Id, contentPackId, null, HashTopic(topic), ["StableTopicKey", "Title", "Description", "SortOrder", "IsActive"]);
                continue;
            }

            var beforeHash = HashTopic(topic);
            var changedFields = new List<string>();
            SetIfChanged(topic.Title, topicDraft.Title, changedFields, value => topic.Title = value, nameof(topic.Title));
            SetIfChanged(topic.Description, topicDraft.Description, changedFields, value => topic.Description = value, nameof(topic.Description));
            SetIfChanged(topic.SortOrder, topicDraft.SortOrder, changedFields, value => topic.SortOrder = value, nameof(topic.SortOrder));
            SetIfChanged(topic.IsActive, topicDraft.IsActive, changedFields, value => topic.IsActive = value, nameof(topic.IsActive));
            if (changedFields.Count == 0)
            {
                result.Counts.TopicsSkipped++;
            }
            else
            {
                topic.UpdatedAtUtc = now;
                result.Counts.TopicsUpdated++;
                AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportUpdated, nameof(CmsLessonTopicEntity), topic.Id, contentPackId, beforeHash, HashTopic(topic), changedFields);
            }
        }
    }

    private async Task UpsertScenariosAsync(Guid contentPackId, Guid? actorUserId, CmsStaticContentImportDraft draft, CmsContentImportResult result, List<ContentAuditLogEntity> auditEntries, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        var topicsByKey = await dbContext.CmsLessonTopics
            .Where(topic => topic.ContentPackId == contentPackId)
            .ToDictionaryAsync(topic => topic.StableTopicKey, StringComparer.Ordinal, cancellationToken);
        var existing = await dbContext.CmsLessonScenarios
            .Where(scenario => scenario.ContentPackId == contentPackId)
            .ToDictionaryAsync(scenario => scenario.StableScenarioKey, StringComparer.Ordinal, cancellationToken);

        foreach (var scenarioDraft in draft.Scenarios.OrderBy(scenario => scenario.StableScenarioKey, StringComparer.Ordinal))
        {
            var mapped = MapScenario(contentPackId, topicsByKey[scenarioDraft.TopicKey].Id, scenarioDraft, now);
            if (!existing.TryGetValue(scenarioDraft.StableScenarioKey, out var scenario))
            {
                dbContext.CmsLessonScenarios.Add(mapped);
                result.Counts.ScenariosCreated++;
                AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportCreated, nameof(CmsLessonScenarioEntity), mapped.Id, contentPackId, null, HashScenario(mapped), ["StableScenarioKey", "TopicId", "Title", "LessonType", "LessonContent"]);
                continue;
            }

            var beforeHash = HashScenario(scenario);
            var changedFields = CopyScenarioIfChanged(scenario, mapped);
            if (changedFields.Count == 0)
            {
                result.Counts.ScenariosSkipped++;
            }
            else
            {
                scenario.UpdatedAtUtc = now;
                result.Counts.ScenariosUpdated++;
                AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportUpdated, nameof(CmsLessonScenarioEntity), scenario.Id, contentPackId, beforeHash, HashScenario(scenario), changedFields);
            }
        }
    }

    private async Task UpsertPromptTemplatesAsync(Guid contentPackId, Guid? actorUserId, CmsStaticContentImportDraft draft, CmsContentImportResult result, List<ContentAuditLogEntity> auditEntries, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await dbContext.PromptTemplates
            .Where(template => template.ContentPackId == contentPackId)
            .ToDictionaryAsync(template => template.TemplateKey, StringComparer.Ordinal, cancellationToken);

        foreach (var templateDraft in draft.PromptTemplates.OrderBy(template => template.TemplateKey, StringComparer.Ordinal))
        {
            if (!existing.TryGetValue(templateDraft.TemplateKey, out var template))
            {
                template = new PromptTemplateEntity
                {
                    Id = Guid.NewGuid(),
                    ContentPackId = contentPackId,
                    TemplateKey = templateDraft.TemplateKey,
                    Body = templateDraft.Body,
                    AllowedPlaceholdersJson = templateDraft.AllowedPlaceholdersJson,
                    RequiredPlaceholdersJson = templateDraft.RequiredPlaceholdersJson,
                    MaxLength = templateDraft.MaxLength,
                    IsActive = templateDraft.IsActive,
                    UpdatedByUserId = actorUserId,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                dbContext.PromptTemplates.Add(template);
                result.Counts.PromptTemplatesCreated++;
                AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportCreated, nameof(PromptTemplateEntity), template.Id, contentPackId, null, HashPromptTemplate(template), ["TemplateKey", "Body", "IsActive"]);
                continue;
            }

            var beforeHash = HashPromptTemplate(template);
            var changedFields = new List<string>();
            SetIfChanged(template.Body, templateDraft.Body, changedFields, value => template.Body = value, nameof(template.Body));
            SetIfChanged(template.AllowedPlaceholdersJson, templateDraft.AllowedPlaceholdersJson, changedFields, value => template.AllowedPlaceholdersJson = value, nameof(template.AllowedPlaceholdersJson));
            SetIfChanged(template.RequiredPlaceholdersJson, templateDraft.RequiredPlaceholdersJson, changedFields, value => template.RequiredPlaceholdersJson = value, nameof(template.RequiredPlaceholdersJson));
            SetIfChanged(template.MaxLength, templateDraft.MaxLength, changedFields, value => template.MaxLength = value, nameof(template.MaxLength));
            SetIfChanged(template.IsActive, templateDraft.IsActive, changedFields, value => template.IsActive = value, nameof(template.IsActive));
            if (changedFields.Count == 0)
            {
                result.Counts.PromptTemplatesSkipped++;
            }
            else
            {
                template.UpdatedByUserId = actorUserId;
                template.UpdatedAtUtc = now;
                result.Counts.PromptTemplatesUpdated++;
                AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportUpdated, nameof(PromptTemplateEntity), template.Id, contentPackId, beforeHash, HashPromptTemplate(template), changedFields);
            }
        }
    }

    private async Task UpsertTutorProfilesAsync(Guid contentPackId, Guid? actorUserId, CmsStaticContentImportDraft draft, CmsContentImportResult result, List<ContentAuditLogEntity> auditEntries, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await dbContext.TutorBehaviorProfiles
            .Where(tutor => tutor.ContentPackId == contentPackId)
            .ToDictionaryAsync(tutor => tutor.TutorId, StringComparer.Ordinal, cancellationToken);

        foreach (var tutorDraft in draft.TutorBehaviorProfiles.OrderBy(tutor => tutor.TutorId, StringComparer.Ordinal))
        {
            var communicationStyleJson = CmsContentJson.SerializeDeterministic(new
            {
                tutorDraft.TutorProfile.CommunicationStyle,
                tutorDraft.TutorProfile.SpeakingRules
            });
            var safetyNotesJson = CmsContentJson.SerializeDeterministic(new
            {
                tutorDraft.TutorProfile.IdentityRules
            });

            if (!existing.TryGetValue(tutorDraft.TutorId, out var tutor))
            {
                tutor = new TutorBehaviorProfileEntity
                {
                    Id = Guid.NewGuid(),
                    ContentPackId = contentPackId,
                    TutorId = tutorDraft.TutorId,
                    DisplayName = tutorDraft.DisplayName,
                    CommunicationStyleJson = communicationStyleJson,
                    SafetyNotesJson = safetyNotesJson,
                    IsActive = tutorDraft.IsActive,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                dbContext.TutorBehaviorProfiles.Add(tutor);
                result.Counts.TutorBehaviorProfilesCreated++;
                AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportCreated, nameof(TutorBehaviorProfileEntity), tutor.Id, contentPackId, null, HashTutorProfile(tutor), ["TutorId", "DisplayName", "CommunicationStyleJson", "SafetyNotesJson", "IsActive"]);
                continue;
            }

            var beforeHash = HashTutorProfile(tutor);
            var changedFields = new List<string>();
            SetIfChanged(tutor.DisplayName, tutorDraft.DisplayName, changedFields, value => tutor.DisplayName = value, nameof(tutor.DisplayName));
            SetIfChanged(tutor.CommunicationStyleJson, communicationStyleJson, changedFields, value => tutor.CommunicationStyleJson = value, nameof(tutor.CommunicationStyleJson));
            SetIfChanged(tutor.SafetyNotesJson, safetyNotesJson, changedFields, value => tutor.SafetyNotesJson = value, nameof(tutor.SafetyNotesJson));
            SetIfChanged(tutor.IsActive, tutorDraft.IsActive, changedFields, value => tutor.IsActive = value, nameof(tutor.IsActive));
            if (changedFields.Count == 0)
            {
                result.Counts.TutorBehaviorProfilesSkipped++;
            }
            else
            {
                tutor.UpdatedAtUtc = now;
                result.Counts.TutorBehaviorProfilesUpdated++;
                AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportUpdated, nameof(TutorBehaviorProfileEntity), tutor.Id, contentPackId, beforeHash, HashTutorProfile(tutor), changedFields);
            }
        }
    }

    private async Task PublishSnapshotIfChangedAsync(Guid contentPackId, Guid? actorUserId, string snapshotJson, string snapshotHash, CmsContentValidationResult validation, CmsContentImportResult result, List<ContentAuditLogEntity> auditEntries, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var latestVersion = await dbContext.ContentVersions
            .Where(version => version.ContentPackId == contentPackId)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestVersion is not null && string.Equals(latestVersion.SnapshotHash, snapshotHash, StringComparison.Ordinal))
        {
            result.PublishedVersionNumber = latestVersion.VersionNumber;
            result.Counts.ContentVersionsSkipped++;
            result.Counts.PublishedSnapshotsSkipped++;
            return;
        }

        var version = new ContentVersionEntity
        {
            Id = Guid.NewGuid(),
            ContentPackId = contentPackId,
            VersionNumber = latestVersion is null ? 1 : latestVersion.VersionNumber + 1,
            SnapshotHash = snapshotHash,
            PublishStatus = CmsContentConstants.ContentVersionPublishStatuses.Published,
            PublishedByUserId = actorUserId,
            PublishedAtUtc = now,
            ValidationSummaryJson = CmsContentJson.SerializeDeterministic(new
            {
                validation.Success,
                validation.Counts,
                Errors = validation.Errors.OrderBy(error => error, StringComparer.Ordinal).ToArray(),
                Warnings = validation.Warnings.OrderBy(warning => warning, StringComparer.Ordinal).ToArray()
            }),
            ChangeSummary = CmsContentConstants.StaticImport.ImportReason,
            CreatedAtUtc = now
        };
        dbContext.ContentVersions.Add(version);
        dbContext.PublishedContentSnapshots.Add(new PublishedContentSnapshotEntity
        {
            Id = Guid.NewGuid(),
            ContentVersionId = version.Id,
            SnapshotJson = snapshotJson,
            SnapshotHash = snapshotHash,
            CreatedAtUtc = now
        });

        result.PublishedVersionNumber = version.VersionNumber;
        result.PublishedSnapshotCreated = true;
        result.Counts.ContentVersionsCreated++;
        result.Counts.PublishedSnapshotsCreated++;
        AddAudit(auditEntries, actorUserId, CmsContentConstants.ContentAuditActions.ImportPublished, nameof(ContentVersionEntity), version.Id, contentPackId, latestVersion?.SnapshotHash, snapshotHash, ["SnapshotHash", "VersionNumber", "PublishStatus"]);
    }

    private static CmsLessonScenarioEntity MapScenario(Guid contentPackId, Guid topicId, CmsStaticScenarioDraft draft, DateTimeOffset now)
    {
        var scenario = draft.Scenario;
        return new CmsLessonScenarioEntity
        {
            Id = Guid.NewGuid(),
            ContentPackId = contentPackId,
            TopicId = topicId,
            StableScenarioKey = draft.StableScenarioKey,
            Title = draft.Title,
            Description = draft.Description,
            LessonType = draft.LessonType,
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
            DefinitionJson = draft.DefinitionJson,
            SoftWrapUpAfterUserTurn = scenario.Metadata.SoftWrapUpAfterUserTurn,
            FinalMessageAtUserTurn = scenario.Metadata.FinalMessageAtUserTurn,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private static List<string> CopyScenarioIfChanged(CmsLessonScenarioEntity target, CmsLessonScenarioEntity source)
    {
        var changedFields = new List<string>();
        SetIfChanged(target.TopicId, source.TopicId, changedFields, value => target.TopicId = value, nameof(target.TopicId));
        SetIfChanged(target.Title, source.Title, changedFields, value => target.Title = value, nameof(target.Title));
        SetIfChanged(target.Description, source.Description, changedFields, value => target.Description = value, nameof(target.Description));
        SetIfChanged(target.LessonType, source.LessonType, changedFields, value => target.LessonType = value, nameof(target.LessonType));
        SetIfChanged(target.SupportedLevelIdsJson, source.SupportedLevelIdsJson, changedFields, value => target.SupportedLevelIdsJson = value, nameof(target.SupportedLevelIdsJson));
        SetIfChanged(target.SetupMessage, source.SetupMessage, changedFields, value => target.SetupMessage = value, nameof(target.SetupMessage));
        SetIfChanged(target.ContextSelectionJson, source.ContextSelectionJson, changedFields, value => target.ContextSelectionJson = value, nameof(target.ContextSelectionJson));
        SetIfChanged(target.LearningGoalJson, source.LearningGoalJson, changedFields, value => target.LearningGoalJson = value, nameof(target.LearningGoalJson));
        SetIfChanged(target.SituationJson, source.SituationJson, changedFields, value => target.SituationJson = value, nameof(target.SituationJson));
        SetIfChanged(target.RolesJson, source.RolesJson, changedFields, value => target.RolesJson = value, nameof(target.RolesJson));
        SetIfChanged(target.TargetLanguageJson, source.TargetLanguageJson, changedFields, value => target.TargetLanguageJson = value, nameof(target.TargetLanguageJson));
        SetIfChanged(target.LevelProfilesJson, source.LevelProfilesJson, changedFields, value => target.LevelProfilesJson = value, nameof(target.LevelProfilesJson));
        SetIfChanged(target.ConversationFlowJson, source.ConversationFlowJson, changedFields, value => target.ConversationFlowJson = value, nameof(target.ConversationFlowJson));
        SetIfChanged(target.RoleplayBeatsJson, source.RoleplayBeatsJson, changedFields, value => target.RoleplayBeatsJson = value, nameof(target.RoleplayBeatsJson));
        SetIfChanged(target.ReciprocalQuestionHandlingJson, source.ReciprocalQuestionHandlingJson, changedFields, value => target.ReciprocalQuestionHandlingJson = value, nameof(target.ReciprocalQuestionHandlingJson));
        SetIfChanged(target.ExpectedScenarioProgressionJson, source.ExpectedScenarioProgressionJson, changedFields, value => target.ExpectedScenarioProgressionJson = value, nameof(target.ExpectedScenarioProgressionJson));
        SetIfChanged(target.ControlledVariationJson, source.ControlledVariationJson, changedFields, value => target.ControlledVariationJson = value, nameof(target.ControlledVariationJson));
        SetIfChanged(target.OffTopicHandlingJson, source.OffTopicHandlingJson, changedFields, value => target.OffTopicHandlingJson = value, nameof(target.OffTopicHandlingJson));
        SetIfChanged(target.FeedbackRulesJson, source.FeedbackRulesJson, changedFields, value => target.FeedbackRulesJson = value, nameof(target.FeedbackRulesJson));
        SetIfChanged(target.HintRulesJson, source.HintRulesJson, changedFields, value => target.HintRulesJson = value, nameof(target.HintRulesJson));
        SetIfChanged(target.RepetitionLogicJson, source.RepetitionLogicJson, changedFields, value => target.RepetitionLogicJson = value, nameof(target.RepetitionLogicJson));
        SetIfChanged(target.AiTutorPromptInstructionsJson, source.AiTutorPromptInstructionsJson, changedFields, value => target.AiTutorPromptInstructionsJson = value, nameof(target.AiTutorPromptInstructionsJson));
        SetIfChanged(target.DefinitionJson, source.DefinitionJson, changedFields, value => target.DefinitionJson = value, nameof(target.DefinitionJson));
        SetIfChanged(target.SoftWrapUpAfterUserTurn, source.SoftWrapUpAfterUserTurn, changedFields, value => target.SoftWrapUpAfterUserTurn = value, nameof(target.SoftWrapUpAfterUserTurn));
        SetIfChanged(target.FinalMessageAtUserTurn, source.FinalMessageAtUserTurn, changedFields, value => target.FinalMessageAtUserTurn = value, nameof(target.FinalMessageAtUserTurn));
        SetIfChanged(target.IsActive, source.IsActive, changedFields, value => target.IsActive = value, nameof(target.IsActive));
        return changedFields;
    }

    private static string BuildSnapshotJson(CmsStaticContentImportDraft draft)
    {
        return CmsContentJson.SerializeDeterministic(new
        {
            ContentPack = new
            {
                Slug = CmsContentConstants.StaticImport.ContentPackSlug,
                Name = CmsContentConstants.StaticImport.ContentPackName,
                BaseStaticContentVersion = CmsContentConstants.StaticImport.BaseStaticContentVersion
            },
            Topics = draft.Topics.OrderBy(topic => topic.SortOrder).ThenBy(topic => topic.StableTopicKey, StringComparer.Ordinal).Select(topic => new
            {
                topic.StableTopicKey,
                topic.Title,
                topic.Description,
                topic.SortOrder,
                topic.IsActive
            }),
            Scenarios = draft.Scenarios.OrderBy(scenario => scenario.StableScenarioKey, StringComparer.Ordinal).Select(scenario => new
            {
                scenario.StableScenarioKey,
                scenario.TopicKey,
                scenario.Title,
                scenario.Description,
                scenario.LessonType,
                scenario.DefinitionJson,
                Lesson = scenario.Scenario
            }),
            PromptTemplates = draft.PromptTemplates.OrderBy(template => template.TemplateKey, StringComparer.Ordinal).Select(template => new
            {
                template.TemplateKey,
                template.AllowedPlaceholdersJson,
                template.RequiredPlaceholdersJson,
                template.MaxLength,
                template.IsActive,
                template.Body
            }),
            TutorBehaviorProfiles = draft.TutorBehaviorProfiles.OrderBy(tutor => tutor.TutorId, StringComparer.Ordinal).Select(tutor => new
            {
                tutor.TutorId,
                tutor.DisplayName,
                tutor.IsActive,
                TutorProfile = tutor.TutorProfile
            })
        });
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

    private static void AddAudit(List<ContentAuditLogEntity> auditEntries, Guid? actorUserId, string action, string entityType, Guid entityId, Guid? contentPackId, string? beforeHash, string? afterHash, IReadOnlyList<string> changedFields)
    {
        auditEntries.Add(new ContentAuditLogEntity
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
            Reason = CmsContentConstants.StaticImport.ImportReason,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RequestMetadataJson = CmsContentJson.SerializeDeterministic(new { Source = CmsContentConstants.StaticImport.ContentPackSlug })
        });
    }

    private static void SetIfChanged<T>(T current, T next, List<string> changedFields, Action<T> setter, string fieldName)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return;
        }

        setter(next);
        changedFields.Add(fieldName);
    }

    private static string Slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        var slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return string.IsNullOrWhiteSpace(slug) ? "topic" : slug;
    }

    private static string HashPack(ContentPackEntity pack) => CmsContentJson.Sha256Hex(CmsContentJson.SerializeDeterministic(new { pack.Slug, pack.Name, pack.Description, pack.Status, pack.BaseStaticContentVersion }));
    private static string HashTopic(CmsLessonTopicEntity topic) => CmsContentJson.Sha256Hex(CmsContentJson.SerializeDeterministic(new { topic.StableTopicKey, topic.Title, topic.Description, topic.SortOrder, topic.IsActive }));
    private static string HashScenario(CmsLessonScenarioEntity scenario) => CmsContentJson.Sha256Hex(CmsContentJson.SerializeDeterministic(new { scenario.StableScenarioKey, scenario.TopicId, scenario.Title, scenario.Description, scenario.LessonType, scenario.SupportedLevelIdsJson, scenario.SetupMessage, scenario.ContextSelectionJson, scenario.LearningGoalJson, scenario.SituationJson, scenario.RolesJson, scenario.TargetLanguageJson, scenario.LevelProfilesJson, scenario.ConversationFlowJson, scenario.RoleplayBeatsJson, scenario.ReciprocalQuestionHandlingJson, scenario.ExpectedScenarioProgressionJson, scenario.ControlledVariationJson, scenario.OffTopicHandlingJson, scenario.FeedbackRulesJson, scenario.HintRulesJson, scenario.RepetitionLogicJson, scenario.AiTutorPromptInstructionsJson, scenario.DefinitionJson, scenario.SoftWrapUpAfterUserTurn, scenario.FinalMessageAtUserTurn, scenario.IsActive }));
    private static string HashPromptTemplate(PromptTemplateEntity template) => CmsContentJson.Sha256Hex(CmsContentJson.SerializeDeterministic(new { template.TemplateKey, template.TargetStudyLanguageId, template.Body, template.AllowedPlaceholdersJson, template.RequiredPlaceholdersJson, template.MaxLength, template.IsActive }));
    private static string HashTutorProfile(TutorBehaviorProfileEntity tutor) => CmsContentJson.Sha256Hex(CmsContentJson.SerializeDeterministic(new { tutor.TutorId, tutor.DisplayName, tutor.CommunicationStyleJson, tutor.SafetyNotesJson, tutor.IsActive }));

    private sealed class StudyLanguageDocument
    {
        public string Id { get; set; } = string.Empty;
    }
}
