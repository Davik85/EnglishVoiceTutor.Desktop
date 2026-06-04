using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishVoiceTutor.Api.Contracts.Cms;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed partial class CmsContentAdminService(
    AppDbContext dbContext,
    ICmsContentValidationService validationService) : ICmsContentAdminService
{
    private const int PreviewSampleSize = 5;

    public async Task<IReadOnlyList<CmsContentPackSummaryResponse>> ListContentPacksAsync(CancellationToken cancellationToken)
    {
        var packs = await dbContext.ContentPacks
            .AsNoTracking()
            .OrderBy(pack => pack.Slug)
            .Select(pack => new
            {
                Pack = pack,
                TopicCount = pack.LessonTopics.Count,
                ScenarioCount = pack.LessonScenarios.Count,
                PromptTemplateCount = pack.PromptTemplates.Count,
                TutorBehaviorProfileCount = pack.TutorBehaviorProfiles.Count,
                CurrentPublishedVersion = pack.ContentVersions
                    .Where(version => version.PublishStatus == CmsContentConstants.ContentVersionPublishStatuses.Published)
                    .OrderByDescending(version => version.VersionNumber)
                    .Select(version => new { version.VersionNumber, version.PublishedAtUtc })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return packs.Select(item => MapContentPackSummary(
            item.Pack,
            item.TopicCount,
            item.ScenarioCount,
            item.PromptTemplateCount,
            item.TutorBehaviorProfileCount,
            item.CurrentPublishedVersion?.VersionNumber,
            item.CurrentPublishedVersion?.PublishedAtUtc)).ToList();
    }

    public async Task<CmsContentPackSummaryResponse?> GetContentPackSummaryAsync(string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = NormalizeRouteValue(slug);
        var pack = await dbContext.ContentPacks
            .AsNoTracking()
            .Where(candidate => candidate.Slug == normalizedSlug)
            .Select(candidate => new
            {
                Pack = candidate,
                TopicCount = candidate.LessonTopics.Count,
                ScenarioCount = candidate.LessonScenarios.Count,
                PromptTemplateCount = candidate.PromptTemplates.Count,
                TutorBehaviorProfileCount = candidate.TutorBehaviorProfiles.Count,
                CurrentPublishedVersion = candidate.ContentVersions
                    .Where(version => version.PublishStatus == CmsContentConstants.ContentVersionPublishStatuses.Published)
                    .OrderByDescending(version => version.VersionNumber)
                    .Select(version => new { version.VersionNumber, version.PublishedAtUtc })
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return pack is null
            ? null
            : MapContentPackSummary(
                pack.Pack,
                pack.TopicCount,
                pack.ScenarioCount,
                pack.PromptTemplateCount,
                pack.TutorBehaviorProfileCount,
                pack.CurrentPublishedVersion?.VersionNumber,
                pack.CurrentPublishedVersion?.PublishedAtUtc);
    }

    public async Task<IReadOnlyList<CmsContentTopicResponse>> ListTopicsAsync(string slug, CancellationToken cancellationToken)
    {
        var pack = await FindContentPackAsync(slug, cancellationToken);
        if (pack is null)
        {
            return [];
        }

        var topics = await dbContext.CmsLessonTopics
            .AsNoTracking()
            .Where(topic => topic.ContentPackId == pack.Id)
            .OrderBy(topic => topic.SortOrder)
            .ThenBy(topic => topic.StableTopicKey)
            .Select(topic => new
            {
                Topic = topic,
                ScenarioCount = topic.LessonScenarios.Count
            })
            .ToListAsync(cancellationToken);

        return topics.Select(topic => MapTopic(topic.Topic, topic.ScenarioCount)).ToList();
    }

    public async Task<CmsContentTopicResponse?> GetTopicAsync(string slug, string topicIdOrKey, CancellationToken cancellationToken)
    {
        var pack = await FindContentPackAsync(slug, cancellationToken);
        if (pack is null)
        {
            return null;
        }

        var query = dbContext.CmsLessonTopics
            .AsNoTracking()
            .Where(topic => topic.ContentPackId == pack.Id);

        var topic = await MatchTopic(query, topicIdOrKey)
            .Select(candidate => new
            {
                Topic = candidate,
                ScenarioCount = candidate.LessonScenarios.Count
            })
            .SingleOrDefaultAsync(cancellationToken);

        return topic is null ? null : MapTopic(topic.Topic, topic.ScenarioCount);
    }

    public async Task<IReadOnlyList<CmsContentScenarioResponse>> ListScenariosAsync(string slug, string? topicIdOrKey, CancellationToken cancellationToken)
    {
        var pack = await FindContentPackAsync(slug, cancellationToken);
        if (pack is null)
        {
            return [];
        }

        var query = dbContext.CmsLessonScenarios
            .AsNoTracking()
            .Include(scenario => scenario.Topic)
            .Where(scenario => scenario.ContentPackId == pack.Id);

        if (!string.IsNullOrWhiteSpace(topicIdOrKey))
        {
            var topic = await MatchTopic(dbContext.CmsLessonTopics.AsNoTracking().Where(candidate => candidate.ContentPackId == pack.Id), topicIdOrKey)
                .SingleOrDefaultAsync(cancellationToken);
            if (topic is null)
            {
                return [];
            }

            query = query.Where(scenario => scenario.TopicId == topic.Id);
        }

        var scenarios = await query
            .OrderBy(scenario => scenario.Topic.SortOrder)
            .ThenBy(scenario => scenario.StableScenarioKey)
            .ToListAsync(cancellationToken);

        return scenarios.Select(scenario => MapScenario(scenario, includeDefinitionJson: false)).ToList();
    }

    public async Task<CmsContentScenarioResponse?> GetScenarioAsync(string slug, string scenarioIdOrKey, CancellationToken cancellationToken)
    {
        var scenario = await FindScenarioAsync(slug, scenarioIdOrKey, cancellationToken, asNoTracking: true);
        return scenario is null ? null : MapScenario(scenario, includeDefinitionJson: true);
    }

    public async Task<IReadOnlyList<CmsPromptTemplateResponse>> ListPromptTemplatesAsync(string slug, CancellationToken cancellationToken)
    {
        var pack = await FindContentPackAsync(slug, cancellationToken);
        if (pack is null)
        {
            return [];
        }

        var templates = await dbContext.PromptTemplates
            .AsNoTracking()
            .Where(template => template.ContentPackId == pack.Id)
            .OrderBy(template => template.TemplateKey)
            .ThenBy(template => template.TargetStudyLanguageId)
            .ToListAsync(cancellationToken);

        return templates.Select(MapPromptTemplate).ToList();
    }

    public async Task<CmsPromptTemplateResponse?> GetPromptTemplateAsync(string slug, string templateIdOrKey, CancellationToken cancellationToken)
    {
        var template = await FindPromptTemplateAsync(slug, templateIdOrKey, cancellationToken, asNoTracking: true);
        return template is null ? null : MapPromptTemplate(template);
    }

    public async Task<IReadOnlyList<CmsTutorBehaviorProfileResponse>> ListTutorBehaviorProfilesAsync(string slug, CancellationToken cancellationToken)
    {
        var pack = await FindContentPackAsync(slug, cancellationToken);
        if (pack is null)
        {
            return [];
        }

        var profiles = await dbContext.TutorBehaviorProfiles
            .AsNoTracking()
            .Where(profile => profile.ContentPackId == pack.Id)
            .OrderBy(profile => profile.TutorId)
            .ToListAsync(cancellationToken);

        return profiles.Select(MapTutorBehaviorProfile).ToList();
    }

    public async Task<CmsTutorBehaviorProfileResponse?> GetTutorBehaviorProfileAsync(string slug, string profileIdOrTutorId, CancellationToken cancellationToken)
    {
        var profile = await FindTutorBehaviorProfileAsync(slug, profileIdOrTutorId, cancellationToken, asNoTracking: true);
        return profile is null ? null : MapTutorBehaviorProfile(profile);
    }

    public async Task<CmsContentUpdateResponse?> UpdateTopicAsync(string slug, string topicIdOrKey, UpdateCmsTopicRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var topic = await FindTopicAsync(slug, topicIdOrKey, cancellationToken, asNoTracking: false);
        if (topic is null)
        {
            return null;
        }

        var beforeHash = HashTopic(topic);
        var changedFields = new List<string>();
        SetIfChanged(topic.Title, request.Title, changedFields, value => topic.Title = value, nameof(topic.Title));
        SetIfChanged(topic.Description, request.Description, changedFields, value => topic.Description = value, nameof(topic.Description));
        SetIfChanged(topic.SortOrder, request.SortOrder, changedFields, value => topic.SortOrder = value, nameof(topic.SortOrder));
        SetIfChanged(topic.IsActive, request.IsActive, changedFields, value => topic.IsActive = value, nameof(topic.IsActive));

        return await SaveDraftUpdateAsync(
            changedFields,
            topic.ContentPackId,
            nameof(CmsLessonTopicEntity),
            topic.Id,
            beforeHash,
            HashTopic(topic),
            request.Reason,
            actorUserId,
            now => topic.UpdatedAtUtc = now,
            cancellationToken);
    }

    public async Task<CmsContentUpdateResponse?> UpdateScenarioAsync(string slug, string scenarioIdOrKey, UpdateCmsScenarioRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var scenario = await FindScenarioAsync(slug, scenarioIdOrKey, cancellationToken, asNoTracking: false);
        if (scenario is null)
        {
            return null;
        }

        var requestedIsActive = request.IsActive ?? scenario.IsActive;
        var requestedDefinitionJson = request.DefinitionJson is null ? scenario.DefinitionJson : request.DefinitionJson.Trim();
        var definitionErrors = CmsScenarioDefinitionJson.ValidateDefinitionJson(requestedDefinitionJson, scenario.StableScenarioKey, requestedIsActive);
        if (definitionErrors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", definitionErrors));
        }

        if (!string.IsNullOrWhiteSpace(requestedDefinitionJson))
        {
            var consistencyErrors = CmsScenarioDefinitionJson.ValidateSimpleFieldConsistency(
                requestedDefinitionJson,
                scenario.StableScenarioKey,
                request.Title ?? scenario.Title,
                request.SetupMessage ?? scenario.SetupMessage);
            if (consistencyErrors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(" ", consistencyErrors));
            }
        }

        var beforeHash = HashScenario(scenario);
        var changedFields = new List<string>();
        SetIfChanged(scenario.Title, request.Title, changedFields, value => scenario.Title = value, nameof(scenario.Title));
        SetIfChanged(scenario.Description, request.Description, changedFields, value => scenario.Description = value, nameof(scenario.Description));
        SetIfChanged(scenario.SetupMessage, request.SetupMessage, changedFields, value => scenario.SetupMessage = value, nameof(scenario.SetupMessage));
        SetIfChanged(scenario.DefinitionJson, request.DefinitionJson?.Trim(), changedFields, value => scenario.DefinitionJson = value, nameof(scenario.DefinitionJson));
        SetIfChanged(scenario.IsActive, request.IsActive, changedFields, value => scenario.IsActive = value, nameof(scenario.IsActive));

        return await SaveDraftUpdateAsync(
            changedFields,
            scenario.ContentPackId,
            nameof(CmsLessonScenarioEntity),
            scenario.Id,
            beforeHash,
            HashScenario(scenario),
            request.Reason,
            actorUserId,
            now => scenario.UpdatedAtUtc = now,
            cancellationToken);
    }

    public async Task<CmsContentUpdateResponse?> UpdatePromptTemplateAsync(string slug, string templateIdOrKey, UpdateCmsPromptTemplateRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var template = await FindPromptTemplateAsync(slug, templateIdOrKey, cancellationToken, asNoTracking: false);
        if (template is null)
        {
            return null;
        }

        var beforeHash = HashPromptTemplate(template);
        var changedFields = new List<string>();
        SetIfChanged(template.Body, request.Body, changedFields, value => template.Body = value, nameof(template.Body));
        SetIfChanged(template.IsActive, request.IsActive, changedFields, value => template.IsActive = value, nameof(template.IsActive));

        return await SaveDraftUpdateAsync(
            changedFields,
            template.ContentPackId,
            nameof(PromptTemplateEntity),
            template.Id,
            beforeHash,
            HashPromptTemplate(template),
            request.Reason,
            actorUserId,
            now =>
            {
                template.UpdatedAtUtc = now;
                template.UpdatedByUserId = actorUserId;
            },
            cancellationToken);
    }

    public async Task<CmsContentUpdateResponse?> UpdateTutorBehaviorProfileAsync(string slug, string profileIdOrTutorId, UpdateCmsTutorBehaviorProfileRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await FindTutorBehaviorProfileAsync(slug, profileIdOrTutorId, cancellationToken, asNoTracking: false);
        if (profile is null)
        {
            return null;
        }

        ValidateJsonIfPresent(request.CommunicationStyleJson, nameof(request.CommunicationStyleJson));
        ValidateJsonIfPresent(request.SafetyNotesJson, nameof(request.SafetyNotesJson));

        var beforeHash = HashTutorBehaviorProfile(profile);
        var changedFields = new List<string>();
        SetIfChanged(profile.DisplayName, request.DisplayName, changedFields, value => profile.DisplayName = value, nameof(profile.DisplayName));
        SetIfChanged(profile.CommunicationStyleJson, request.CommunicationStyleJson, changedFields, value => profile.CommunicationStyleJson = value, nameof(profile.CommunicationStyleJson));
        SetIfChanged(profile.SafetyNotesJson, request.SafetyNotesJson, changedFields, value => profile.SafetyNotesJson = value, nameof(profile.SafetyNotesJson));
        SetIfChanged(profile.IsActive, request.IsActive, changedFields, value => profile.IsActive = value, nameof(profile.IsActive));

        return await SaveDraftUpdateAsync(
            changedFields,
            profile.ContentPackId,
            nameof(TutorBehaviorProfileEntity),
            profile.Id,
            beforeHash,
            HashTutorBehaviorProfile(profile),
            request.Reason,
            actorUserId,
            now => profile.UpdatedAtUtc = now,
            cancellationToken);
    }

    public async Task<CmsContentValidationResponse?> ValidateDraftAsync(string slug, CancellationToken cancellationToken)
    {
        var pack = await FindContentPackAsync(slug, cancellationToken);
        if (pack is null)
        {
            return null;
        }

        var validation = await validationService.ValidateDraftRowsAsync(pack.Id, cancellationToken);
        return new CmsContentValidationResponse
        {
            Success = validation.Success,
            ContentPackSlug = pack.Slug,
            Counts = new CmsContentValidationCountsResponse
            {
                Topics = validation.Counts.Topics,
                Scenarios = validation.Counts.Scenarios,
                PromptTemplates = validation.Counts.PromptTemplates,
                TutorBehaviorProfiles = validation.Counts.TutorBehaviorProfiles
            },
            Errors = validation.Errors,
            Warnings = validation.Warnings,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public async Task<CmsContentPreviewResponse?> GetPreviewSummaryAsync(string slug, CancellationToken cancellationToken)
    {
        var summary = await GetContentPackSummaryAsync(slug, cancellationToken);
        if (summary is null)
        {
            return null;
        }

        var validation = await ValidateDraftAsync(slug, cancellationToken);
        if (validation is null)
        {
            return null;
        }

        var sampleTopics = await dbContext.CmsLessonTopics
            .AsNoTracking()
            .Where(topic => topic.ContentPackId == summary.Id)
            .OrderBy(topic => topic.SortOrder)
            .ThenBy(topic => topic.StableTopicKey)
            .Take(PreviewSampleSize)
            .Select(topic => new CmsContentPreviewTopicSummaryResponse
            {
                Id = topic.Id,
                StableTopicKey = topic.StableTopicKey,
                Title = topic.Title,
                SortOrder = topic.SortOrder,
                IsActive = topic.IsActive
            })
            .ToListAsync(cancellationToken);

        var sampleScenarioRows = await dbContext.CmsLessonScenarios
            .AsNoTracking()
            .Include(scenario => scenario.Topic)
            .Where(scenario => scenario.ContentPackId == summary.Id)
            .OrderBy(scenario => scenario.Topic.SortOrder)
            .ThenBy(scenario => scenario.StableScenarioKey)
            .Take(PreviewSampleSize)
            .ToListAsync(cancellationToken);

        var sampleScenarios = sampleScenarioRows.Select(scenario => new CmsContentPreviewScenarioSummaryResponse
            {
                Id = scenario.Id,
                StableScenarioKey = scenario.StableScenarioKey,
                TopicKey = scenario.Topic.StableTopicKey,
                Title = scenario.Title,
                IsActive = scenario.IsActive,
                DefinitionJsonPresent = !string.IsNullOrWhiteSpace(scenario.DefinitionJson),
                DefinitionJsonValid = CmsScenarioDefinitionJson.ValidateDefinitionJson(scenario.DefinitionJson, scenario.StableScenarioKey, scenario.IsActive).Count == 0
            })
            .ToList();

        return new CmsContentPreviewResponse
        {
            ContentPackSlug = summary.Slug,
            ContentPackName = summary.Name,
            ContentPackStatus = summary.Status,
            TopicCount = summary.TopicCount,
            ScenarioCount = summary.ScenarioCount,
            PromptTemplateCount = summary.PromptTemplateCount,
            TutorBehaviorProfileCount = summary.TutorBehaviorProfileCount,
            CurrentPublishedVersionNumber = summary.CurrentPublishedVersionNumber,
            SampleTopics = sampleTopics,
            SampleScenarios = sampleScenarios,
            Validation = validation,
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private async Task<CmsContentUpdateResponse> SaveDraftUpdateAsync(
        List<string> changedFields,
        Guid contentPackId,
        string entityType,
        Guid entityId,
        string beforeHash,
        string afterHash,
        string? reason,
        Guid actorUserId,
        Action<DateTimeOffset> touchEntity,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (changedFields.Count == 0)
        {
            return new CmsContentUpdateResponse
            {
                Success = true,
                EntityType = entityType,
                EntityId = entityId,
                ContentPackId = contentPackId,
                ChangedFields = [],
                NoChanges = true,
                UpdatedAtUtc = now
            };
        }

        touchEntity(now);
        dbContext.ContentAuditLogs.Add(new ContentAuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = CmsContentConstants.ContentAuditActions.DraftUpdated,
            EntityType = entityType,
            EntityId = entityId,
            ContentPackId = contentPackId,
            BeforeHash = beforeHash,
            AfterHash = afterHash,
            ChangedFieldsJson = CmsContentJson.SerializeDeterministic(changedFields.OrderBy(field => field, StringComparer.Ordinal).ToArray()),
            Reason = SanitizeReason(reason),
            CreatedAtUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CmsContentUpdateResponse
        {
            Success = true,
            EntityType = entityType,
            EntityId = entityId,
            ContentPackId = contentPackId,
            ChangedFields = changedFields,
            NoChanges = false,
            UpdatedAtUtc = now
        };
    }

    private async Task<ContentPackEntity?> FindContentPackAsync(string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = NormalizeRouteValue(slug);
        return await dbContext.ContentPacks
            .AsNoTracking()
            .SingleOrDefaultAsync(pack => pack.Slug == normalizedSlug, cancellationToken);
    }

    private async Task<CmsLessonTopicEntity?> FindTopicAsync(string slug, string topicIdOrKey, CancellationToken cancellationToken, bool asNoTracking)
    {
        var pack = await FindContentPackAsync(slug, cancellationToken);
        if (pack is null)
        {
            return null;
        }

        var query = dbContext.CmsLessonTopics.Where(topic => topic.ContentPackId == pack.Id);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await MatchTopic(query, topicIdOrKey).SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<CmsLessonScenarioEntity?> FindScenarioAsync(string slug, string scenarioIdOrKey, CancellationToken cancellationToken, bool asNoTracking)
    {
        var pack = await FindContentPackAsync(slug, cancellationToken);
        if (pack is null)
        {
            return null;
        }

        var query = dbContext.CmsLessonScenarios
            .Include(scenario => scenario.Topic)
            .Where(scenario => scenario.ContentPackId == pack.Id);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await MatchScenario(query, scenarioIdOrKey).SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<PromptTemplateEntity?> FindPromptTemplateAsync(string slug, string templateIdOrKey, CancellationToken cancellationToken, bool asNoTracking)
    {
        var pack = await FindContentPackAsync(slug, cancellationToken);
        if (pack is null)
        {
            return null;
        }

        var query = dbContext.PromptTemplates.Where(template => template.ContentPackId == pack.Id);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var value = NormalizeRouteValue(templateIdOrKey);
        return Guid.TryParse(value, out var id)
            ? await query.SingleOrDefaultAsync(template => template.Id == id, cancellationToken)
            : await query.SingleOrDefaultAsync(template => template.TemplateKey == value, cancellationToken);
    }

    private async Task<TutorBehaviorProfileEntity?> FindTutorBehaviorProfileAsync(string slug, string profileIdOrTutorId, CancellationToken cancellationToken, bool asNoTracking)
    {
        var pack = await FindContentPackAsync(slug, cancellationToken);
        if (pack is null)
        {
            return null;
        }

        var query = dbContext.TutorBehaviorProfiles.Where(profile => profile.ContentPackId == pack.Id);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var value = NormalizeRouteValue(profileIdOrTutorId);
        return Guid.TryParse(value, out var id)
            ? await query.SingleOrDefaultAsync(profile => profile.Id == id, cancellationToken)
            : await query.SingleOrDefaultAsync(profile => profile.TutorId == value, cancellationToken);
    }

    private static IQueryable<CmsLessonTopicEntity> MatchTopic(IQueryable<CmsLessonTopicEntity> query, string topicIdOrKey)
    {
        var value = NormalizeRouteValue(topicIdOrKey);
        return Guid.TryParse(value, out var id)
            ? query.Where(topic => topic.Id == id)
            : query.Where(topic => topic.StableTopicKey == value);
    }

    private static IQueryable<CmsLessonScenarioEntity> MatchScenario(IQueryable<CmsLessonScenarioEntity> query, string scenarioIdOrKey)
    {
        var value = NormalizeRouteValue(scenarioIdOrKey);
        return Guid.TryParse(value, out var id)
            ? query.Where(scenario => scenario.Id == id)
            : query.Where(scenario => scenario.StableScenarioKey == value);
    }

    private static CmsContentPackSummaryResponse MapContentPackSummary(
        ContentPackEntity pack,
        int topicCount,
        int scenarioCount,
        int promptTemplateCount,
        int tutorBehaviorProfileCount,
        int? currentPublishedVersionNumber,
        DateTimeOffset? currentPublishedAtUtc)
    {
        return new CmsContentPackSummaryResponse
        {
            Id = pack.Id,
            Slug = pack.Slug,
            Name = pack.Name,
            Description = pack.Description,
            Status = pack.Status,
            BaseStaticContentVersion = pack.BaseStaticContentVersion,
            CreatedAtUtc = pack.CreatedAtUtc,
            UpdatedAtUtc = pack.UpdatedAtUtc,
            TopicCount = topicCount,
            ScenarioCount = scenarioCount,
            PromptTemplateCount = promptTemplateCount,
            TutorBehaviorProfileCount = tutorBehaviorProfileCount,
            CurrentPublishedVersionNumber = currentPublishedVersionNumber,
            CurrentPublishedAtUtc = currentPublishedAtUtc
        };
    }

    private static CmsContentTopicResponse MapTopic(CmsLessonTopicEntity topic, int scenarioCount)
    {
        return new CmsContentTopicResponse
        {
            Id = topic.Id,
            ContentPackId = topic.ContentPackId,
            StableTopicKey = topic.StableTopicKey,
            SortOrder = topic.SortOrder,
            Title = topic.Title,
            Description = topic.Description,
            IsActive = topic.IsActive,
            CreatedAtUtc = topic.CreatedAtUtc,
            UpdatedAtUtc = topic.UpdatedAtUtc,
            ScenarioCount = scenarioCount
        };
    }

    private static CmsContentScenarioResponse MapScenario(CmsLessonScenarioEntity scenario, bool includeDefinitionJson)
    {
        return new CmsContentScenarioResponse
        {
            Id = scenario.Id,
            ContentPackId = scenario.ContentPackId,
            TopicId = scenario.TopicId,
            TopicKey = scenario.Topic.StableTopicKey,
            StableScenarioKey = scenario.StableScenarioKey,
            Title = scenario.Title,
            Description = scenario.Description,
            LessonType = scenario.LessonType,
            SupportedLevelIdsJson = scenario.SupportedLevelIdsJson,
            SetupMessage = scenario.SetupMessage,
            DefinitionJson = includeDefinitionJson ? FormatScenarioDefinitionJsonForResponse(scenario) : string.Empty,
            IsDefinitionJsonFallback = includeDefinitionJson && CmsScenarioDefinitionJson.IsFallback(scenario),
            SoftWrapUpAfterUserTurn = scenario.SoftWrapUpAfterUserTurn,
            FinalMessageAtUserTurn = scenario.FinalMessageAtUserTurn,
            IsActive = scenario.IsActive,
            CreatedAtUtc = scenario.CreatedAtUtc,
            UpdatedAtUtc = scenario.UpdatedAtUtc
        };
    }

    private static CmsPromptTemplateResponse MapPromptTemplate(PromptTemplateEntity template)
    {
        return new CmsPromptTemplateResponse
        {
            Id = template.Id,
            ContentPackId = template.ContentPackId,
            TemplateKey = template.TemplateKey,
            TargetStudyLanguageId = template.TargetStudyLanguageId,
            Body = template.Body,
            AllowedPlaceholdersJson = template.AllowedPlaceholdersJson,
            RequiredPlaceholdersJson = template.RequiredPlaceholdersJson,
            MaxLength = template.MaxLength,
            IsActive = template.IsActive,
            CreatedAtUtc = template.CreatedAtUtc,
            UpdatedAtUtc = template.UpdatedAtUtc
        };
    }

    private static CmsTutorBehaviorProfileResponse MapTutorBehaviorProfile(TutorBehaviorProfileEntity profile)
    {
        return new CmsTutorBehaviorProfileResponse
        {
            Id = profile.Id,
            ContentPackId = profile.ContentPackId,
            TutorId = profile.TutorId,
            DisplayName = profile.DisplayName,
            CommunicationStyleJson = profile.CommunicationStyleJson,
            SafetyNotesJson = profile.SafetyNotesJson,
            IsActive = profile.IsActive,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }

    private static string FormatScenarioDefinitionJsonForResponse(CmsLessonScenarioEntity scenario)
    {
        var definitionJson = CmsScenarioDefinitionJson.GetDefinitionJsonOrFallback(scenario);
        try
        {
            return CmsScenarioDefinitionJson.PrettyPrint(definitionJson);
        }
        catch (JsonException)
        {
            return definitionJson;
        }
    }

    private static void SetIfChanged(string? currentValue, string? requestedValue, List<string> changedFields, Action<string> assign, string fieldName)
    {
        if (requestedValue is null || string.Equals(currentValue, requestedValue, StringComparison.Ordinal))
        {
            return;
        }

        assign(requestedValue);
        changedFields.Add(fieldName);
    }

    private static void SetIfChanged(int currentValue, int? requestedValue, List<string> changedFields, Action<int> assign, string fieldName)
    {
        if (!requestedValue.HasValue || currentValue == requestedValue.Value)
        {
            return;
        }

        assign(requestedValue.Value);
        changedFields.Add(fieldName);
    }

    private static void SetIfChanged(bool currentValue, bool? requestedValue, List<string> changedFields, Action<bool> assign, string fieldName)
    {
        if (!requestedValue.HasValue || currentValue == requestedValue.Value)
        {
            return;
        }

        assign(requestedValue.Value);
        changedFields.Add(fieldName);
    }

    private static void ValidateJsonIfPresent(string? value, string fieldName)
    {
        if (value is null)
        {
            return;
        }

        try
        {
            using var _ = JsonDocument.Parse(value);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"{fieldName} must contain valid JSON.", ex);
        }
    }

    private static string NormalizeRouteValue(string value) => value.Trim();

    private static string SanitizeReason(string? reason)
    {
        var trimmed = (reason ?? string.Empty).Trim();
        return SecretLikeReasonPattern().IsMatch(trimmed) ? "[redacted]" : trimmed;
    }

    [GeneratedRegex("(?i)(sk-[a-z0-9_-]{20,}|api[_-]?key|bearer\\s+[a-z0-9._-]{20,}|password|token|secret)", RegexOptions.CultureInvariant)]
    private static partial Regex SecretLikeReasonPattern();


    private static string HashTopic(CmsLessonTopicEntity topic) => CmsContentJson.Sha256Hex(CmsContentJson.SerializeDeterministic(new { topic.StableTopicKey, topic.Title, topic.Description, topic.SortOrder, topic.IsActive }));

    private static string HashScenario(CmsLessonScenarioEntity scenario) => CmsContentJson.Sha256Hex(CmsContentJson.SerializeDeterministic(new { scenario.StableScenarioKey, scenario.TopicId, scenario.Title, scenario.Description, scenario.LessonType, scenario.SupportedLevelIdsJson, scenario.SetupMessage, scenario.ContextSelectionJson, scenario.LearningGoalJson, scenario.SituationJson, scenario.RolesJson, scenario.TargetLanguageJson, scenario.LevelProfilesJson, scenario.ConversationFlowJson, scenario.RoleplayBeatsJson, scenario.ReciprocalQuestionHandlingJson, scenario.ExpectedScenarioProgressionJson, scenario.ControlledVariationJson, scenario.OffTopicHandlingJson, scenario.FeedbackRulesJson, scenario.HintRulesJson, scenario.RepetitionLogicJson, scenario.AiTutorPromptInstructionsJson, scenario.DefinitionJson, scenario.SoftWrapUpAfterUserTurn, scenario.FinalMessageAtUserTurn, scenario.IsActive }));

    private static string HashPromptTemplate(PromptTemplateEntity template) => CmsContentJson.Sha256Hex(CmsContentJson.SerializeDeterministic(new { template.TemplateKey, template.TargetStudyLanguageId, template.Body, template.AllowedPlaceholdersJson, template.RequiredPlaceholdersJson, template.MaxLength, template.IsActive }));

    private static string HashTutorBehaviorProfile(TutorBehaviorProfileEntity profile) => CmsContentJson.Sha256Hex(CmsContentJson.SerializeDeterministic(new { profile.TutorId, profile.DisplayName, profile.CommunicationStyleJson, profile.SafetyNotesJson, profile.IsActive }));
}
