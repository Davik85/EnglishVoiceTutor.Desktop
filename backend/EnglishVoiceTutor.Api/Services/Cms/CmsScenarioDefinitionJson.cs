using System.Text.Json;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Desktop.Models.LessonContent;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Api.Services.Cms;

internal static class CmsScenarioDefinitionJson
{
    private static readonly JsonSerializerOptions ReadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string[] RequiredRootProperties =
    [
        "id",
        "metadata",
        "lessonSetup",
        "learningGoal",
        "targetLanguage",
        "levelProfiles",
        "conversationFlow",
        "controlledVariation",
        "offTopicHandling",
        "feedbackRules",
        "hintRules",
        "aiTutorPromptInstructions"
    ];

    public static string SerializeDefinition(LessonScenario scenario)
    {
        var canonicalScenario = JsonSerializer.Deserialize<LessonScenario>(JsonSerializer.Serialize(scenario), ReadJsonOptions)
            ?? throw new JsonException("Scenario definition serialization produced an empty lesson scenario.");
        canonicalScenario.LocalizedSetup = null;
        return CmsContentJson.SerializeDeterministic(canonicalScenario);
    }

    public static string GetDefinitionJsonOrFallback(CmsLessonScenarioEntity scenario)
    {
        return string.IsNullOrWhiteSpace(scenario.DefinitionJson)
            ? BuildFallbackDefinitionJson(scenario)
            : scenario.DefinitionJson.Trim();
    }

    public static bool IsFallback(CmsLessonScenarioEntity scenario) => string.IsNullOrWhiteSpace(scenario.DefinitionJson);

    public static string PrettyPrint(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement, PrettyJsonOptions);
    }

    public static LessonScenario DeserializeLessonScenario(CmsLessonScenarioEntity scenario)
    {
        var lesson = JsonSerializer.Deserialize<LessonScenario>(GetDefinitionJsonOrFallback(scenario), ReadJsonOptions)
            ?? throw new JsonException($"Scenario '{scenario.StableScenarioKey}' definition JSON deserialized to an empty lesson scenario.");
        lesson.LocalizedSetup = null;
        return lesson;
    }

    public static IReadOnlyList<string> ValidateDefinitionJson(string? definitionJson, string scenarioKey, bool requireNonEmpty)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(definitionJson))
        {
            if (requireNonEmpty)
            {
                errors.Add($"Scenario '{scenarioKey}' full scenario JSON is required.");
            }

            return errors;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(definitionJson);
        }
        catch (JsonException ex)
        {
            errors.Add($"Scenario '{scenarioKey}' full scenario JSON must contain valid JSON: {ex.Message}");
            return errors;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"Scenario '{scenarioKey}' full scenario JSON root must be an object.");
                return errors;
            }

            foreach (var propertyName in RequiredRootProperties)
            {
                if (!root.TryGetProperty(propertyName, out var property) || IsMissing(property))
                {
                    errors.Add($"Scenario '{scenarioKey}' full scenario JSON is missing required property '{propertyName}'.");
                }
            }

            if (root.TryGetProperty("id", out var idProperty) && (idProperty.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(idProperty.GetString())))
            {
                errors.Add($"Scenario '{scenarioKey}' full scenario JSON property 'id' must be a non-empty string.");
            }

            if (!root.TryGetProperty("lessonSetup", out var lessonSetup) || lessonSetup.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"Scenario '{scenarioKey}' full scenario JSON property 'lessonSetup' must be an object.");
            }
            else if (!lessonSetup.TryGetProperty("setupMessage", out var setupMessage) || setupMessage.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(setupMessage.GetString()))
            {
                errors.Add($"Scenario '{scenarioKey}' full scenario JSON is missing required string property 'lessonSetup.setupMessage'.");
            }

            if (root.EnumerateObject().Any(property => string.Equals(property.Name, "localizedSetup", StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"Scenario '{scenarioKey}' full scenario JSON must not contain response-only property 'localizedSetup'.");
            }

            ValidateSetupLocalizations(root, scenarioKey, errors);
        }

        return errors;
    }

    private static void ValidateSetupLocalizations(JsonElement root, string scenarioKey, List<string> errors)
    {
        if (!root.TryGetProperty("setupLocalizations", out var localizations))
        {
            return;
        }

        if (localizations.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"Scenario '{scenarioKey}' setupLocalizations must be an object.");
            return;
        }

        var supportedIds = StudyLanguageCatalog.All.Select(language => language.Id).ToHashSet(StringComparer.Ordinal);
        var variantIds = new HashSet<string>(StringComparer.Ordinal);
        var equivalentVariantIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("controlledVariation", out var variation)
            && variation.ValueKind == JsonValueKind.Object
            && variation.TryGetProperty("contextVariants", out var variants)
            && variants.ValueKind == JsonValueKind.Array)
        {
            foreach (var variant in variants.EnumerateArray())
            {
                var id = variant.ValueKind == JsonValueKind.Object && variant.TryGetProperty("id", out var idElement)
                    ? idElement.GetString()?.Trim()
                    : null;
                if (string.IsNullOrWhiteSpace(id) || !equivalentVariantIds.Add(id))
                {
                    errors.Add($"Scenario '{scenarioKey}' has blank or duplicate-equivalent context variant IDs.");
                    return;
                }

                variantIds.Add(id);
            }
        }

        var languageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var languageProperty in localizations.EnumerateObject())
        {
            var languageId = languageProperty.Name.Trim();
            if (!supportedIds.Contains(languageId) || !languageIds.Add(languageId))
            {
                errors.Add($"Scenario '{scenarioKey}' setupLocalizations contains unsupported or duplicate-equivalent language id '{languageProperty.Name}'.");
                continue;
            }

            if (languageProperty.Value.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' must be an object.");
                continue;
            }

            if (!languageProperty.Value.TryGetProperty("setupMessageTemplate", out var template)
                || template.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(template.GetString()))
            {
                errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' requires a non-empty setupMessageTemplate.");
            }

            if (!languageProperty.Value.TryGetProperty("contextVariantTitles", out var titles) || titles.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' requires a contextVariantTitles object.");
                continue;
            }

            var titleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var title in titles.EnumerateObject())
            {
                if (!titleIds.Add(title.Name) || !variantIds.Contains(title.Name) || title.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(title.Value.GetString()))
                {
                    errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' contains an unknown, duplicate-equivalent, or blank context variant title.");
                }
            }

            if (!titleIds.SetEquals(variantIds))
            {
                errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' must cover each context variant ID exactly.");
            }
        }
    }

    public static IReadOnlyList<string> ValidateSetupLocalizationsForPublication(string? definitionJson, string scenarioKey)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(definitionJson))
        {
            errors.Add($"Scenario '{scenarioKey}' is missing full scenario JSON required for setup-localization publication.");
            return errors;
        }

        try
        {
            using var document = JsonDocument.Parse(definitionJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"Scenario '{scenarioKey}' full scenario JSON root must be an object for setup-localization publication.");
                return errors;
            }

            if (!root.TryGetProperty("lessonSetup", out var lessonSetup)
                || lessonSetup.ValueKind != JsonValueKind.Object
                || !lessonSetup.TryGetProperty("setupMessage", out var setupMessage)
                || setupMessage.ValueKind != JsonValueKind.String)
            {
                errors.Add($"Scenario '{scenarioKey}' is missing canonical lessonSetup.setupMessage required for setup-localization publication.");
                return errors;
            }

            var requiredPlaceholders = ExtractPlaceholders(setupMessage.GetString());
            var variantIds = GetContextVariantIds(root, scenarioKey, errors);
            if (errors.Count > 0)
            {
                return errors;
            }

            if (!root.TryGetProperty("setupLocalizations", out var localizations)
                || localizations.ValueKind != JsonValueKind.Object)
            {
                foreach (var languageId in RequiredNonEnglishStudyLanguageIds)
                {
                    errors.Add($"Scenario '{scenarioKey}' is missing required setup localization language '{languageId}' for publication.");
                }

                return errors;
            }

            foreach (var languageId in RequiredNonEnglishStudyLanguageIds)
            {
                if (!localizations.TryGetProperty(languageId, out var localization)
                    || localization.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"Scenario '{scenarioKey}' is missing required setup localization language '{languageId}' for publication.");
                    continue;
                }

                if (!localization.TryGetProperty("setupMessageTemplate", out var template)
                    || template.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(template.GetString()))
                {
                    errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' is missing a non-empty setupMessageTemplate for publication.");
                }
                else
                {
                    var actualPlaceholders = ExtractPlaceholders(template.GetString());
                    foreach (var placeholder in requiredPlaceholders.Except(actualPlaceholders, StringComparer.Ordinal))
                    {
                        errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' is missing required placeholder '{placeholder}'.");
                    }

                    foreach (var placeholder in actualPlaceholders.Except(requiredPlaceholders, StringComparer.Ordinal))
                    {
                        errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' contains unsupported placeholder '{placeholder}'.");
                    }
                }

                if (!localization.TryGetProperty("contextVariantTitles", out var titles)
                    || titles.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' is missing contextVariantTitles required for publication.");
                    continue;
                }

                var titleIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var title in titles.EnumerateObject())
                {
                    if (!titleIds.Add(title.Name) || !variantIds.Contains(title.Name))
                    {
                        errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' contains unknown context variant ID '{title.Name}'.");
                    }
                    else if (title.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(title.Value.GetString()))
                    {
                        errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' has a blank context title for ID '{title.Name}'.");
                    }
                }

                foreach (var variantId in variantIds.Except(titleIds, StringComparer.Ordinal))
                {
                    errors.Add($"Scenario '{scenarioKey}' setup localization '{languageId}' is missing context variant ID '{variantId}'.");
                }
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"Scenario '{scenarioKey}' full scenario JSON is invalid for setup-localization publication: {ex.Message}");
        }

        return errors;
    }

    private static readonly string[] RequiredNonEnglishStudyLanguageIds = StudyLanguageCatalog.All
        .Where(language => !language.IsDefault)
        .Select(language => language.Id)
        .ToArray();

    private static HashSet<string> GetContextVariantIds(JsonElement root, string scenarioKey, List<string> errors)
    {
        var variantIds = new HashSet<string>(StringComparer.Ordinal);
        if (!root.TryGetProperty("controlledVariation", out var variation)
            || variation.ValueKind != JsonValueKind.Object
            || !variation.TryGetProperty("contextVariants", out var variants)
            || variants.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"Scenario '{scenarioKey}' is missing controlledVariation.contextVariants required for setup-localization publication.");
            return variantIds;
        }

        foreach (var variant in variants.EnumerateArray())
        {
            var id = variant.ValueKind == JsonValueKind.Object && variant.TryGetProperty("id", out var idElement)
                ? idElement.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(id) || !variantIds.Add(id))
            {
                errors.Add($"Scenario '{scenarioKey}' has blank or duplicate context variant IDs for setup-localization publication.");
                break;
            }
        }

        return variantIds;
    }

    private static HashSet<string> ExtractPlaceholders(string? value)
    {
        return System.Text.RegularExpressions.Regex.Matches(value ?? string.Empty, @"\{\{[^{}]+\}\}")
            .Select(match => match.Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    public static IReadOnlyList<string> ValidateSimpleFieldConsistency(
        string definitionJson,
        string stableScenarioKey,
        string? title,
        string? setupMessage)
    {
        var errors = new List<string>();
        using var document = JsonDocument.Parse(definitionJson);
        var root = document.RootElement;

        if (root.TryGetProperty("id", out var idProperty))
        {
            var jsonId = idProperty.ValueKind == JsonValueKind.String ? idProperty.GetString() : null;
            if (!string.IsNullOrWhiteSpace(jsonId) && !string.Equals(jsonId.Trim(), stableScenarioKey, StringComparison.Ordinal))
            {
                errors.Add($"Full scenario JSON id must match stable scenario key '{stableScenarioKey}'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(title)
            && root.TryGetProperty("metadata", out var metadata)
            && metadata.ValueKind == JsonValueKind.Object
            && metadata.TryGetProperty("subtopic", out var subtopicProperty))
        {
            var subtopic = subtopicProperty.ValueKind == JsonValueKind.String ? subtopicProperty.GetString() : null;
            if (!string.IsNullOrWhiteSpace(subtopic) && !string.Equals(subtopic.Trim(), title.Trim(), StringComparison.Ordinal))
            {
                errors.Add("Title must match full scenario JSON metadata.subtopic when that JSON field is present.");
            }
        }

        if (!string.IsNullOrWhiteSpace(setupMessage)
            && root.TryGetProperty("lessonSetup", out var lessonSetup)
            && lessonSetup.ValueKind == JsonValueKind.Object
            && lessonSetup.TryGetProperty("setupMessage", out var setupMessageProperty))
        {
            var jsonSetupMessage = setupMessageProperty.ValueKind == JsonValueKind.String ? setupMessageProperty.GetString() : null;
            if (!string.IsNullOrWhiteSpace(jsonSetupMessage) && !string.Equals(jsonSetupMessage.Trim(), setupMessage.Trim(), StringComparison.Ordinal))
            {
                errors.Add("Setup message must match full scenario JSON lessonSetup.setupMessage when that JSON field is present.");
            }
        }

        return errors;
    }

    private static bool IsMissing(JsonElement property)
    {
        return property.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            || (property.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(property.GetString()))
            || (property.ValueKind == JsonValueKind.Array && property.GetArrayLength() == 0)
            || (property.ValueKind == JsonValueKind.Object && !property.EnumerateObject().Any());
    }

    private static string BuildFallbackDefinitionJson(CmsLessonScenarioEntity scenario)
    {
        var fallback = new
        {
            id = scenario.StableScenarioKey,
            metadata = new
            {
                topic = scenario.Topic?.StableTopicKey ?? string.Empty,
                subtopic = scenario.Title,
                lessonType = scenario.LessonType,
                supportedLevels = ParseJsonElementOrNull(scenario.SupportedLevelIdsJson),
                softWrapUpAfterUserTurn = (int?)null,
                finalMessageAtUserTurn = (int?)null,
                cmsDefinitionJsonFallback = true
            },
            lessonSetup = new
            {
                setupMessage = scenario.SetupMessage,
                contextSelection = ParseJsonElementOrNull(scenario.ContextSelectionJson)
            },
            learningGoal = ParseJsonElementOrNull(scenario.LearningGoalJson),
            situation = ParseJsonElementOrNull(scenario.SituationJson),
            roles = ParseJsonElementOrNull(scenario.RolesJson),
            targetLanguage = ParseJsonElementOrNull(scenario.TargetLanguageJson),
            levelProfiles = ParseJsonElementOrNull(scenario.LevelProfilesJson),
            conversationFlow = ParseJsonElementOrNull(scenario.ConversationFlowJson),
            roleplayBeats = ParseJsonElementOrNull(scenario.RoleplayBeatsJson),
            reciprocalQuestionHandling = ParseJsonElementOrNull(scenario.ReciprocalQuestionHandlingJson),
            expectedScenarioProgression = ParseJsonElementOrNull(scenario.ExpectedScenarioProgressionJson),
            controlledVariation = ParseJsonElementOrNull(scenario.ControlledVariationJson),
            offTopicHandling = ParseJsonElementOrNull(scenario.OffTopicHandlingJson),
            feedbackRules = ParseJsonElementOrNull(scenario.FeedbackRulesJson),
            hintRules = ParseJsonElementOrNull(scenario.HintRulesJson),
            repetitionLogic = ParseJsonElementOrNull(scenario.RepetitionLogicJson),
            aiTutorPromptInstructions = ParseJsonElementOrNull(scenario.AiTutorPromptInstructionsJson)
        };

        return CmsContentJson.SerializeDeterministic(fallback);
    }

    private static JsonElement? ParseJsonElementOrNull(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
