using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Shared.StudyLanguages;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed partial class CmsContentValidationService(AppDbContext dbContext) : ICmsContentValidationService
{
    private static readonly HashSet<string> SupportedStudyLanguageIds = StudyLanguageCatalog.All
        .Select(language => language.Id)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<string> RequiredTutorBehaviorProfileIds = TutorAvatarOptions.All
        .Select(avatar => avatar.Id)
        .OrderBy(id => id, StringComparer.Ordinal)
        .ToArray();

    public CmsContentValidationResult Validate(CmsStaticContentImportDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var result = new CmsContentValidationResult
        {
            Counts = new CmsContentValidationCounts
            {
                Topics = draft.Topics.Count,
                Scenarios = draft.Scenarios.Count,
                PromptTemplates = draft.PromptTemplates.Count,
                TutorBehaviorProfiles = draft.TutorBehaviorProfiles.Count
            }
        };

        ValidateStudyLanguages(draft, result);
        ValidateTopics(draft, result);
        ValidateScenarios(draft, result);
        ValidatePromptTemplates(draft, result);
        ValidateLevelProfiles(draft.PromptTemplates.FirstOrDefault(template => template.TemplateKey == CmsContentConstants.PromptTemplateKeys.LevelProfiles)?.Body, result);
        ValidateTutorProfiles(draft, result);
        ValidateSecrets(draft, result);
        ValidateDeterministicSerialization(draft, result);

        result.Warnings.AddRange(draft.Warnings);
        return result;
    }


    public async Task<CmsContentValidationResult> ValidateDraftRowsAsync(Guid contentPackId, CancellationToken cancellationToken)
    {
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
            .ToListAsync(cancellationToken);

        var tutorProfiles = await dbContext.TutorBehaviorProfiles
            .AsNoTracking()
            .Where(profile => profile.ContentPackId == contentPackId)
            .OrderBy(profile => profile.TutorId)
            .ToListAsync(cancellationToken);

        var result = new CmsContentValidationResult
        {
            Counts = new CmsContentValidationCounts
            {
                Topics = topics.Count,
                Scenarios = scenarios.Count,
                PromptTemplates = promptTemplates.Count,
                TutorBehaviorProfiles = tutorProfiles.Count
            }
        };

        ValidateDraftTopics(topics, scenarios, result);
        ValidateDraftScenarios(topics, scenarios, result);
        ValidateDraftPromptTemplates(promptTemplates, result);
        ValidateLevelProfiles(promptTemplates.FirstOrDefault(template => template.TemplateKey == CmsContentConstants.PromptTemplateKeys.LevelProfiles && template.IsActive)?.Body, result);
        ValidateDraftTutorProfiles(tutorProfiles, result);
        ValidateDraftJsonPayloads(scenarios, promptTemplates, tutorProfiles, result);
        ValidateDraftSecrets(scenarios, promptTemplates, tutorProfiles, result);

        return result;
    }

    public async Task<CmsContentValidationResult> ValidateDraftRowsForPublicationAsync(Guid contentPackId, CancellationToken cancellationToken)
    {
        var result = await ValidateDraftRowsAsync(contentPackId, cancellationToken);
        if (!result.Success)
        {
            return result;
        }

        var activeScenarios = await dbContext.CmsLessonScenarios
            .AsNoTracking()
            .Where(scenario => scenario.ContentPackId == contentPackId && scenario.IsActive)
            .OrderBy(scenario => scenario.StableScenarioKey)
            .ToListAsync(cancellationToken);

        foreach (var scenario in activeScenarios)
        {
            result.Errors.AddRange(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(
                scenario.DefinitionJson,
                scenario.StableScenarioKey));
        }

        return result;
    }

    private static void ValidateStudyLanguages(CmsStaticContentImportDraft draft, CmsContentValidationResult result)
    {
        foreach (var languageId in draft.StudyLanguageIds)
        {
            if (!SupportedStudyLanguageIds.Contains(languageId))
            {
                result.Errors.Add($"Unsupported study language id in static catalog: {languageId}");
            }
        }

        var expected = StudyLanguageCatalog.All.Select(language => language.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var actual = draft.StudyLanguageIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            result.Errors.Add($"Static study language catalog must match the supported IDs exactly: {string.Join(", ", expected)}.");
        }
    }

    private static void ValidateTopics(CmsStaticContentImportDraft draft, CmsContentValidationResult result)
    {
        if (draft.Topics.Count == 0)
        {
            result.Errors.Add("No lesson topics were found in static lesson content.");
            return;
        }

        foreach (var topic in draft.Topics)
        {
            Require(topic.StableTopicKey, $"Topic '{topic.Title}' is missing a stable topic key.", result);
            Require(topic.Title, $"Topic '{topic.StableTopicKey}' is missing a title.", result);

            if (draft.Scenarios.All(scenario => !string.Equals(scenario.TopicKey, topic.StableTopicKey, StringComparison.Ordinal)))
            {
                result.Errors.Add($"Topic '{topic.StableTopicKey}' has no imported scenarios.");
            }
        }
    }

    private static void ValidateScenarios(CmsStaticContentImportDraft draft, CmsContentValidationResult result)
    {
        if (draft.Scenarios.Count == 0)
        {
            result.Errors.Add("No lesson scenarios were found in static lesson content.");
            return;
        }

        var topicKeys = draft.Topics.Select(topic => topic.StableTopicKey).ToHashSet(StringComparer.Ordinal);
        foreach (var scenarioDraft in draft.Scenarios)
        {
            var scenario = scenarioDraft.Scenario;
            Require(scenarioDraft.StableScenarioKey, "A lesson scenario is missing a stable scenario key.", result);
            Require(scenario.Id, $"Scenario '{scenarioDraft.StableScenarioKey}' is missing its JSON id.", result);
            Require(scenarioDraft.TopicKey, $"Scenario '{scenarioDraft.StableScenarioKey}' is missing a topic key.", result);
            Require(scenarioDraft.Title, $"Scenario '{scenarioDraft.StableScenarioKey}' is missing a title/subtopic.", result);
            Require(scenario.Metadata.LessonType, $"Scenario '{scenarioDraft.StableScenarioKey}' is missing lesson type.", result);
            Require(scenario.LessonSetup.SetupMessage, $"Scenario '{scenarioDraft.StableScenarioKey}' is missing setup message.", result);
            foreach (var error in CmsScenarioDefinitionJson.ValidateDefinitionJson(scenarioDraft.DefinitionJson, scenarioDraft.StableScenarioKey, requireNonEmpty: true))
            {
                result.Errors.Add(error);
            }

            if (!topicKeys.Contains(scenarioDraft.TopicKey))
            {
                result.Errors.Add($"Scenario '{scenarioDraft.StableScenarioKey}' references missing topic '{scenarioDraft.TopicKey}'.");
            }

            if (scenario.Metadata.SupportedLevels.Count == 0)
            {
                result.Errors.Add($"Scenario '{scenarioDraft.StableScenarioKey}' has no supported levels.");
            }

            // Legacy scenario metadata turn-limit fields are tolerated for import compatibility,
            // but level profiles are the only validated source of lesson length behavior.
        }
    }

    private static void ValidatePromptTemplates(CmsStaticContentImportDraft draft, CmsContentValidationResult result)
    {
        foreach (var template in draft.PromptTemplates)
        {
            Require(template.TemplateKey, "A prompt template is missing its template key.", result);
            Require(template.Body, $"Prompt template '{template.TemplateKey}' is empty.", result);
        }
    }


    private static void ValidateLevelProfiles(string? levelProfilesJson, CmsContentValidationResult result)
    {
        try
        {
            CmsLevelProfiles.Validate(CmsLevelProfiles.DeserializeOrDefaults(levelProfilesJson), result.Errors, "CMS level profiles");
        }
        catch (JsonException)
        {
            result.Errors.Add("CMS level profiles JSON is invalid.");
        }
    }

    private static void ValidateTutorProfiles(CmsStaticContentImportDraft draft, CmsContentValidationResult result)
    {
        ValidateTutorProfileIds(draft.TutorBehaviorProfiles.Select(tutor => tutor.TutorId), "Tutor behavior profile", result);
        foreach (var tutor in draft.TutorBehaviorProfiles)
        {
            Require(tutor.TutorId, "A tutor behavior profile is missing its tutor id.", result);
            Require(tutor.DisplayName, $"Tutor behavior profile '{tutor.TutorId}' is missing display name.", result);
            if (tutor.TutorProfile.CommunicationStyle.Count == 0)
            {
                result.Errors.Add($"Tutor behavior profile '{tutor.TutorId}' has no communication style rules.");
            }
        }
    }

    private static void ValidateSecrets(CmsStaticContentImportDraft draft, CmsContentValidationResult result)
    {
        foreach (var (label, value) in EnumerateTextFields(draft))
        {
            if (SecretPattern().IsMatch(value))
            {
                result.Errors.Add($"Potential secret-like value found in {label}.");
            }
        }
    }

    private static void ValidateDeterministicSerialization(CmsStaticContentImportDraft draft, CmsContentValidationResult result)
    {
        try
        {
            _ = CmsContentJson.SerializeDeterministic(draft.Topics.OrderBy(topic => topic.StableTopicKey, StringComparer.Ordinal));
            _ = CmsContentJson.SerializeDeterministic(draft.Scenarios.OrderBy(scenario => scenario.StableScenarioKey, StringComparer.Ordinal).Select(scenario => scenario.Scenario));
            _ = CmsContentJson.SerializeDeterministic(draft.TutorBehaviorProfiles.OrderBy(tutor => tutor.TutorId, StringComparer.Ordinal).Select(tutor => tutor.TutorProfile));
        }
        catch (Exception ex) when (ex is NotSupportedException or JsonException)
        {
            result.Errors.Add($"Static content could not be serialized deterministically: {ex.Message}");
        }
    }

    private static IEnumerable<(string Label, string Value)> EnumerateTextFields(CmsStaticContentImportDraft draft)
    {
        foreach (var scenario in draft.Scenarios)
        {
            yield return ($"scenario '{scenario.StableScenarioKey}'", CmsContentJson.SerializeDeterministic(scenario.Scenario));
        }

        foreach (var template in draft.PromptTemplates)
        {
            yield return ($"prompt template '{template.TemplateKey}'", template.Body);
        }

        foreach (var tutor in draft.TutorBehaviorProfiles)
        {
            yield return ($"tutor behavior profile '{tutor.TutorId}'", CmsContentJson.SerializeDeterministic(tutor.TutorProfile));
        }
    }


    private static void ValidateDraftTopics(
        IReadOnlyList<CmsLessonTopicEntity> topics,
        IReadOnlyList<CmsLessonScenarioEntity> scenarios,
        CmsContentValidationResult result)
    {
        if (topics.Count == 0)
        {
            result.Errors.Add("No draft lesson topics exist for this content pack.");
            return;
        }

        foreach (var topic in topics)
        {
            Require(topic.StableTopicKey, $"Draft topic '{topic.Id}' is missing a stable topic key.", result);
            Require(topic.Title, $"Draft topic '{topic.StableTopicKey}' is missing a title.", result);
            if (scenarios.All(scenario => scenario.TopicId != topic.Id))
            {
                result.Errors.Add($"Draft topic '{topic.StableTopicKey}' has no scenarios.");
            }
        }
    }

    private static void ValidateDraftScenarios(
        IReadOnlyList<CmsLessonTopicEntity> topics,
        IReadOnlyList<CmsLessonScenarioEntity> scenarios,
        CmsContentValidationResult result)
    {
        if (scenarios.Count == 0)
        {
            result.Errors.Add("No draft lesson scenarios exist for this content pack.");
            return;
        }

        var topicIds = topics.Select(topic => topic.Id).ToHashSet();
        foreach (var scenario in scenarios)
        {
            Require(scenario.StableScenarioKey, $"Draft scenario '{scenario.Id}' is missing a stable scenario key.", result);
            Require(scenario.Title, $"Draft scenario '{scenario.StableScenarioKey}' is missing a title.", result);
            Require(scenario.LessonType, $"Draft scenario '{scenario.StableScenarioKey}' is missing lesson type.", result);
            Require(scenario.SetupMessage, $"Draft scenario '{scenario.StableScenarioKey}' is missing setup message.", result);

            if (!topicIds.Contains(scenario.TopicId))
            {
                result.Errors.Add($"Draft scenario '{scenario.StableScenarioKey}' references missing topic '{scenario.TopicId}'.");
            }

            // Legacy draft scenario turn-limit columns are ignored by runtime and are not normal behavior controls.
        }
    }

    private static void ValidateDraftPromptTemplates(IReadOnlyList<PromptTemplateEntity> promptTemplates, CmsContentValidationResult result)
    {
        foreach (var template in promptTemplates)
        {
            Require(template.TemplateKey, $"Draft prompt template '{template.Id}' is missing its template key.", result);
            Require(template.Body, $"Draft prompt template '{template.TemplateKey}' is empty.", result);
        }
    }

    private static void ValidateDraftTutorProfiles(IReadOnlyList<TutorBehaviorProfileEntity> tutorProfiles, CmsContentValidationResult result)
    {
        ValidateTutorProfileIds(tutorProfiles.Select(profile => profile.TutorId), "Draft tutor behavior profile", result);
        foreach (var profile in tutorProfiles)
        {
            Require(profile.TutorId, $"Draft tutor behavior profile '{profile.Id}' is missing tutor id.", result);
            Require(profile.DisplayName, $"Draft tutor behavior profile '{profile.TutorId}' is missing display name.", result);
            Require(profile.CommunicationStyleJson, $"Draft tutor behavior profile '{profile.TutorId}' is missing communication style JSON.", result);
        }
    }

    private static void ValidateTutorProfileIds(IEnumerable<string> tutorIds, string label, CmsContentValidationResult result)
    {
        var actualIds = tutorIds
            .Select(TutorAvatarOptions.ToCanonicalId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var actualIdSet = actualIds.ToHashSet(StringComparer.Ordinal);
        var requiredIdSet = RequiredTutorBehaviorProfileIds.ToHashSet(StringComparer.Ordinal);
        var missingIds = RequiredTutorBehaviorProfileIds.Where(id => !actualIdSet.Contains(id)).ToArray();
        var unknownIds = actualIds.Where(id => !requiredIdSet.Contains(id)).Distinct(StringComparer.Ordinal).ToArray();
        var duplicateIds = actualIds.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();

        if (missingIds.Length > 0 || unknownIds.Length > 0 || duplicateIds.Length > 0)
        {
            result.Errors.Add($"{label} ids do not match approved stable tutor ids. Expected required tutor ids: {FormatIds(RequiredTutorBehaviorProfileIds)}; actual tutor ids: {FormatIds(actualIds)}; missing tutor ids: {FormatIds(missingIds)}; unknown/extra tutor ids: {FormatIds(unknownIds)}; duplicate tutor ids: {FormatIds(duplicateIds)}.");
        }
    }

    private static string FormatIds(IEnumerable<string> ids)
    {
        var values = ids.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
        return values.Length == 0 ? "none" : string.Join(", ", values);
    }

    private static void ValidateDraftJsonPayloads(
        IReadOnlyList<CmsLessonScenarioEntity> scenarios,
        IReadOnlyList<PromptTemplateEntity> promptTemplates,
        IReadOnlyList<TutorBehaviorProfileEntity> tutorProfiles,
        CmsContentValidationResult result)
    {
        foreach (var scenario in scenarios)
        {
            ValidateJson(scenario.SupportedLevelIdsJson, nameof(scenario.SupportedLevelIdsJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.ContextSelectionJson, nameof(scenario.ContextSelectionJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.LearningGoalJson, nameof(scenario.LearningGoalJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.SituationJson, nameof(scenario.SituationJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.RolesJson, nameof(scenario.RolesJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.TargetLanguageJson, nameof(scenario.TargetLanguageJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.LevelProfilesJson, nameof(scenario.LevelProfilesJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.ConversationFlowJson, nameof(scenario.ConversationFlowJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.RoleplayBeatsJson, nameof(scenario.RoleplayBeatsJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.ReciprocalQuestionHandlingJson, nameof(scenario.ReciprocalQuestionHandlingJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.ExpectedScenarioProgressionJson, nameof(scenario.ExpectedScenarioProgressionJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.ControlledVariationJson, nameof(scenario.ControlledVariationJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.OffTopicHandlingJson, nameof(scenario.OffTopicHandlingJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.FeedbackRulesJson, nameof(scenario.FeedbackRulesJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.HintRulesJson, nameof(scenario.HintRulesJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.RepetitionLogicJson, nameof(scenario.RepetitionLogicJson), scenario.StableScenarioKey, result);
            ValidateJson(scenario.AiTutorPromptInstructionsJson, nameof(scenario.AiTutorPromptInstructionsJson), scenario.StableScenarioKey, result);
            foreach (var error in CmsScenarioDefinitionJson.ValidateDefinitionJson(scenario.DefinitionJson, scenario.StableScenarioKey, scenario.IsActive))
            {
                result.Errors.Add(error);
            }
        }

        foreach (var template in promptTemplates)
        {
            ValidateJson(template.AllowedPlaceholdersJson, nameof(template.AllowedPlaceholdersJson), template.TemplateKey, result);
            ValidateJson(template.RequiredPlaceholdersJson, nameof(template.RequiredPlaceholdersJson), template.TemplateKey, result);
        }

        foreach (var profile in tutorProfiles)
        {
            ValidateJson(profile.CommunicationStyleJson, nameof(profile.CommunicationStyleJson), profile.TutorId, result);
            ValidateJson(profile.SafetyNotesJson, nameof(profile.SafetyNotesJson), profile.TutorId, result);
        }
    }


    private static void ValidateDraftSecrets(
        IReadOnlyList<CmsLessonScenarioEntity> scenarios,
        IReadOnlyList<PromptTemplateEntity> promptTemplates,
        IReadOnlyList<TutorBehaviorProfileEntity> tutorProfiles,
        CmsContentValidationResult result)
    {
        foreach (var (label, value) in EnumerateDraftTextFields(scenarios, promptTemplates, tutorProfiles))
        {
            if (SecretPattern().IsMatch(value))
            {
                result.Errors.Add($"Potential secret-like value found in draft {label}.");
            }
        }
    }

    private static IEnumerable<(string Label, string Value)> EnumerateDraftTextFields(
        IReadOnlyList<CmsLessonScenarioEntity> scenarios,
        IReadOnlyList<PromptTemplateEntity> promptTemplates,
        IReadOnlyList<TutorBehaviorProfileEntity> tutorProfiles)
    {
        foreach (var scenario in scenarios)
        {
            yield return ($"scenario '{scenario.StableScenarioKey}' setup message", scenario.SetupMessage);
            yield return ($"scenario '{scenario.StableScenarioKey}' JSON payloads", CmsContentJson.SerializeDeterministic(new
            {
                scenario.ContextSelectionJson,
                scenario.LearningGoalJson,
                scenario.SituationJson,
                scenario.RolesJson,
                scenario.TargetLanguageJson,
                scenario.LevelProfilesJson,
                scenario.ConversationFlowJson,
                scenario.RoleplayBeatsJson,
                scenario.ReciprocalQuestionHandlingJson,
                scenario.ExpectedScenarioProgressionJson,
                scenario.ControlledVariationJson,
                scenario.OffTopicHandlingJson,
                scenario.FeedbackRulesJson,
                scenario.HintRulesJson,
                scenario.RepetitionLogicJson,
                scenario.AiTutorPromptInstructionsJson,
                scenario.DefinitionJson
            }));
        }

        foreach (var template in promptTemplates)
        {
            yield return ($"prompt template '{template.TemplateKey}'", template.Body);
        }

        foreach (var profile in tutorProfiles)
        {
            yield return ($"tutor behavior profile '{profile.TutorId}'", CmsContentJson.SerializeDeterministic(new
            {
                profile.CommunicationStyleJson,
                profile.SafetyNotesJson
            }));
        }
    }

    private static void ValidateJson(string value, string fieldName, string entityKey, CmsContentValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result.Errors.Add($"Draft entity '{entityKey}' has empty JSON field '{fieldName}'.");
            return;
        }

        try
        {
            using var _ = JsonDocument.Parse(value);
        }
        catch (JsonException ex)
        {
            result.Errors.Add($"Draft entity '{entityKey}' has invalid JSON in '{fieldName}': {ex.Message}");
        }
    }

    private static void Require(string? value, string errorMessage, CmsContentValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result.Errors.Add(errorMessage);
        }
    }

    [GeneratedRegex("(?i)(sk-[a-z0-9_-]{20,}|api[_-]?key\\s*[=:]\\s*['\"]?[a-z0-9_-]{20,}|bearer\\s+[a-z0-9._-]{20,}|-----BEGIN (?:RSA |OPENSSH |EC |DSA )?PRIVATE KEY-----|connectionstring\\s*=)", RegexOptions.CultureInvariant)]
    private static partial Regex SecretPattern();
}
