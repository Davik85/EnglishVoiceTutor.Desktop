using System.Text.Json;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Desktop.Models.LessonContent;

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

    public static string SerializeDefinition(LessonScenario scenario) => CmsContentJson.SerializeDeterministic(scenario);

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
        return JsonSerializer.Deserialize<LessonScenario>(GetDefinitionJsonOrFallback(scenario), ReadJsonOptions)
            ?? throw new JsonException($"Scenario '{scenario.StableScenarioKey}' definition JSON deserialized to an empty lesson scenario.");
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
        }

        return errors;
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
                softWrapUpAfterUserTurn = scenario.SoftWrapUpAfterUserTurn,
                finalMessageAtUserTurn = scenario.FinalMessageAtUserTurn,
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
