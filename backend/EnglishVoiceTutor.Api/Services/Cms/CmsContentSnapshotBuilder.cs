using System.Text.Json;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Desktop.Models.LessonContent;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Cms;

internal static class CmsContentSnapshotBuilder
{
    private static readonly JsonSerializerOptions ReadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<string> BuildSnapshotJsonFromDraftRowsAsync(AppDbContext dbContext, Guid contentPackId, CmsPublishedLessonContent? baseContent, CancellationToken cancellationToken)
    {
        var pack = await dbContext.ContentPacks
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == contentPackId, cancellationToken);

        var topics = await dbContext.CmsLessonTopics
            .AsNoTracking()
            .Where(topic => topic.ContentPackId == contentPackId)
            .OrderBy(topic => topic.SortOrder)
            .ThenBy(topic => topic.StableTopicKey)
            .ToListAsync(cancellationToken);

        var scenarios = await dbContext.CmsLessonScenarios
            .AsNoTracking()
            .Include(scenario => scenario.Topic)
            .Where(scenario => scenario.ContentPackId == contentPackId)
            .OrderBy(scenario => scenario.StableScenarioKey)
            .ToListAsync(cancellationToken);

        var promptTemplates = await dbContext.PromptTemplates
            .AsNoTracking()
            .Where(template => template.ContentPackId == contentPackId)
            .OrderBy(template => template.TemplateKey)
            .ThenBy(template => template.TargetStudyLanguageId)
            .ToListAsync(cancellationToken);

        var tutorProfiles = await dbContext.TutorBehaviorProfiles
            .AsNoTracking()
            .Where(profile => profile.ContentPackId == contentPackId)
            .OrderBy(profile => profile.TutorId)
            .ToListAsync(cancellationToken);

        return BuildSnapshotJson(pack, topics, scenarios, promptTemplates, tutorProfiles, baseContent);
    }

    public static string BuildSnapshotJson(
        ContentPackEntity pack,
        IReadOnlyList<CmsLessonTopicEntity> topics,
        IReadOnlyList<CmsLessonScenarioEntity> scenarios,
        IReadOnlyList<PromptTemplateEntity> promptTemplates,
        IReadOnlyList<TutorBehaviorProfileEntity> tutorProfiles,
        CmsPublishedLessonContent? baseContent = null)
    {
        var baseScenarios = baseContent?.Scenarios.ToDictionary(scenario => scenario.StableScenarioKey, StringComparer.Ordinal)
            ?? new Dictionary<string, CmsPublishedLessonScenario>(StringComparer.Ordinal);
        var baseTutorProfiles = baseContent?.TutorBehaviorProfiles.ToDictionary(profile => profile.TutorId, StringComparer.Ordinal)
            ?? new Dictionary<string, CmsPublishedTutorBehaviorProfile>(StringComparer.Ordinal);
        var levelProfiles = ResolveLevelProfiles(promptTemplates);

        return CmsContentJson.SerializeDeterministic(new
        {
            ContentPack = new
            {
                pack.Slug,
                pack.Name,
                pack.BaseStaticContentVersion
            },
            Topics = topics.OrderBy(topic => topic.SortOrder).ThenBy(topic => topic.StableTopicKey, StringComparer.Ordinal).Select(topic => new
            {
                topic.StableTopicKey,
                topic.Title,
                topic.Description,
                topic.SortOrder,
                topic.IsActive
            }),
            Scenarios = scenarios.OrderBy(scenario => scenario.StableScenarioKey, StringComparer.Ordinal).Select(scenario => new
            {
                scenario.StableScenarioKey,
                TopicKey = scenario.Topic.StableTopicKey,
                scenario.Title,
                scenario.Description,
                scenario.LessonType,
                DefinitionJson = CmsScenarioDefinitionJson.GetDefinitionJsonOrFallback(scenario),
                Lesson = BuildLessonScenario(scenario, baseScenarios.TryGetValue(scenario.StableScenarioKey, out var baseScenario) ? baseScenario.Lesson : null)
            }),
            PromptTemplates = promptTemplates.OrderBy(template => template.TemplateKey, StringComparer.Ordinal).ThenBy(template => template.TargetStudyLanguageId, StringComparer.Ordinal).Select(template => new
            {
                template.TemplateKey,
                template.AllowedPlaceholdersJson,
                template.RequiredPlaceholdersJson,
                template.MaxLength,
                template.IsActive,
                template.Body
            }),
            TutorBehaviorProfiles = tutorProfiles.OrderBy(tutor => tutor.TutorId, StringComparer.Ordinal).Select(tutor => new
            {
                tutor.TutorId,
                tutor.DisplayName,
                tutor.IsActive,
                TutorProfile = BuildTutorProfile(tutor, baseTutorProfiles.TryGetValue(tutor.TutorId, out var baseTutor) ? baseTutor.TutorProfile : null)
            }),
            LevelProfiles = levelProfiles.OrderBy(level => level.SortOrder).ThenBy(level => level.StableLevelKey, StringComparer.Ordinal)
        });
    }

    public static CmsPublishedLessonContent DeserializePublishedContent(string snapshotJson)
    {
        return JsonSerializer.Deserialize<CmsPublishedLessonContent>(snapshotJson, ReadJsonOptions)
            ?? throw new JsonException("Published CMS content snapshot deserialized to an empty content object.");
    }

    private static List<CmsLevelProfile> ResolveLevelProfiles(IReadOnlyList<PromptTemplateEntity> promptTemplates)
    {
        var template = promptTemplates.FirstOrDefault(template => template.TemplateKey == CmsContentConstants.PromptTemplateKeys.LevelProfiles && template.IsActive);
        return CmsLevelProfiles.DeserializeOrDefaults(template?.Body);
    }

    private static LessonScenario BuildLessonScenario(CmsLessonScenarioEntity scenario, LessonScenario? baseScenario)
    {
        if (!string.IsNullOrWhiteSpace(scenario.DefinitionJson))
        {
            return CmsScenarioDefinitionJson.DeserializeLessonScenario(scenario);
        }

        var lesson = baseScenario ?? new LessonScenario { Id = scenario.StableScenarioKey };
        lesson.Id = string.IsNullOrWhiteSpace(lesson.Id) ? scenario.StableScenarioKey : lesson.Id;
        lesson.Metadata.Topic = string.IsNullOrWhiteSpace(lesson.Metadata.Topic) ? scenario.Topic.StableTopicKey : lesson.Metadata.Topic;
        lesson.Metadata.LessonType = scenario.LessonType;
        lesson.Metadata.SupportedLevels = Deserialize<List<string>>(scenario.SupportedLevelIdsJson);
        lesson.Metadata.SoftWrapUpAfterUserTurn = scenario.SoftWrapUpAfterUserTurn ?? 0;
        lesson.Metadata.FinalMessageAtUserTurn = scenario.FinalMessageAtUserTurn ?? 0;
        lesson.LessonSetup.SetupMessage = scenario.SetupMessage;
        lesson.LessonSetup.ContextSelection = Deserialize<LessonContextSelection>(scenario.ContextSelectionJson);
        lesson.LearningGoal = Deserialize<LearningGoal>(scenario.LearningGoalJson);
        lesson.Situation = Deserialize<LessonSituation>(scenario.SituationJson);
        lesson.Roles = Deserialize<LessonRoles>(scenario.RolesJson);
        lesson.TargetLanguage = Deserialize<TargetLanguage>(scenario.TargetLanguageJson);
        lesson.LevelProfiles = Deserialize<Dictionary<string, LevelProfile>>(scenario.LevelProfilesJson);
        lesson.ConversationFlow = Deserialize<ConversationFlow>(scenario.ConversationFlowJson);
        lesson.RoleplayBeats = Deserialize<List<RoleplayBeat>>(scenario.RoleplayBeatsJson);
        lesson.ReciprocalQuestionHandling = Deserialize<ReciprocalQuestionHandling>(scenario.ReciprocalQuestionHandlingJson);
        lesson.ExpectedScenarioProgression = Deserialize<List<string>>(scenario.ExpectedScenarioProgressionJson);
        lesson.ControlledVariation = Deserialize<ControlledVariation>(scenario.ControlledVariationJson);
        lesson.OffTopicHandling = Deserialize<OffTopicHandling>(scenario.OffTopicHandlingJson);
        lesson.FeedbackRules = Deserialize<FeedbackRules>(scenario.FeedbackRulesJson);
        lesson.HintRules = Deserialize<HintRules>(scenario.HintRulesJson);
        lesson.RepetitionLogic = Deserialize<RepetitionLogic>(scenario.RepetitionLogicJson);
        lesson.AiTutorPromptInstructions = Deserialize<List<string>>(scenario.AiTutorPromptInstructionsJson);
        return lesson;
    }

    private static TutorProfile BuildTutorProfile(TutorBehaviorProfileEntity profile, TutorProfile? baseProfile)
    {
        using var communicationStyleDocument = JsonDocument.Parse(profile.CommunicationStyleJson);
        using var safetyNotesDocument = JsonDocument.Parse(profile.SafetyNotesJson);

        var tutorProfile = baseProfile ?? new TutorProfile { Id = profile.TutorId };
        tutorProfile.Id = string.IsNullOrWhiteSpace(tutorProfile.Id) ? profile.TutorId : tutorProfile.Id;
        tutorProfile.DisplayName = profile.DisplayName;
        tutorProfile.CommunicationStyle = DeserializeProperty<List<string>>(communicationStyleDocument.RootElement, "communicationStyle")
            ?? DeserializeProperty<List<string>>(communicationStyleDocument.RootElement, "CommunicationStyle")
            ?? [];
        tutorProfile.SpeakingRules = DeserializeProperty<Dictionary<string, string>>(communicationStyleDocument.RootElement, "speakingRules")
            ?? DeserializeProperty<Dictionary<string, string>>(communicationStyleDocument.RootElement, "SpeakingRules")
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        tutorProfile.IdentityRules = DeserializeProperty<List<string>>(safetyNotesDocument.RootElement, "identityRules")
            ?? DeserializeProperty<List<string>>(safetyNotesDocument.RootElement, "IdentityRules")
            ?? [];
        return tutorProfile;
    }

    private static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, ReadJsonOptions)
            ?? throw new JsonException($"JSON payload could not be deserialized as {typeof(T).Name}.");
    }

    private static T? DeserializeProperty<T>(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            ? property.Deserialize<T>(ReadJsonOptions)
            : default;
    }
}
