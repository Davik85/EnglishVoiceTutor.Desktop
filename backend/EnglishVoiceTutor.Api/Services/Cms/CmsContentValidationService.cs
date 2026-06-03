using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed partial class CmsContentValidationService : ICmsContentValidationService
{
    private static readonly HashSet<string> SupportedStudyLanguageIds = StudyLanguageCatalog.All
        .Select(language => language.Id)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
        ValidateTutorProfiles(draft, result);
        ValidateSecrets(draft, result);
        ValidateDeterministicSerialization(draft, result);

        result.Warnings.AddRange(draft.Warnings);
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

            if (!topicKeys.Contains(scenarioDraft.TopicKey))
            {
                result.Errors.Add($"Scenario '{scenarioDraft.StableScenarioKey}' references missing topic '{scenarioDraft.TopicKey}'.");
            }

            if (scenario.Metadata.SupportedLevels.Count == 0)
            {
                result.Errors.Add($"Scenario '{scenarioDraft.StableScenarioKey}' has no supported levels.");
            }

            if (scenario.Metadata.SoftWrapUpAfterUserTurn <= 0)
            {
                result.Errors.Add($"Scenario '{scenarioDraft.StableScenarioKey}' has an invalid soft wrap-up turn value.");
            }

            if (scenario.Metadata.FinalMessageAtUserTurn <= 0)
            {
                result.Errors.Add($"Scenario '{scenarioDraft.StableScenarioKey}' has an invalid final-message turn value.");
            }

            if (scenario.Metadata.FinalMessageAtUserTurn < scenario.Metadata.SoftWrapUpAfterUserTurn)
            {
                result.Errors.Add($"Scenario '{scenarioDraft.StableScenarioKey}' final-message turn must be greater than or equal to the soft wrap-up turn.");
            }
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

    private static void ValidateTutorProfiles(CmsStaticContentImportDraft draft, CmsContentValidationResult result)
    {
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
