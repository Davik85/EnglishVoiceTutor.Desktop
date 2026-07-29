using System.Text.Json;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Api.Services.Cms;
using EnglishVoiceTutor.Desktop.Models.LessonContent;

namespace EnglishVoiceTutor.Api.Tests.Services.Cms;

public sealed class SetupLocalizationTests
{
    [Fact]
    public void ResolverReturnsCanonicalEnglishProjection()
    {
        var result = new LocalizedLessonSetupResolver().Resolve(Scenario(), "English");
        Assert.Equal("en", result.ResolvedStudyLanguageId);
        Assert.Equal("Hello {{userDisplayName}}", result.SetupMessageTemplate);
        Assert.Equal("canonical_english", result.Source);
        Assert.Equal("complete", result.Status);
        Assert.Equal("English title", result.ContextVariantDisplayTitles["context-a"]);
    }

    [Fact]
    public void ResolverReturnsOnlyAuthoredNonEnglishValuesAndDoesNotLeak()
    {
        var resolver = new LocalizedLessonSetupResolver();
        var scenario = Scenario();
        var spanish = resolver.Resolve(scenario, "Spanish");
        var french = resolver.Resolve(scenario, "French");
        Assert.Equal("Hola {{userDisplayName}}", spanish.SetupMessageTemplate);
        Assert.Equal("Título español", spanish.ContextVariantDisplayTitles["context-a"]);
        Assert.Equal("complete", spanish.Status);
        Assert.Null(french.SetupMessageTemplate);
        Assert.Empty(french.ContextVariantDisplayTitles);
        Assert.Equal("incomplete", french.Status);
        Assert.Equal("English title", scenario.ControlledVariation.ContextVariants[0].Title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("en")]
    public void ResolverDefaultsOnlyMissingLanguageToEnglish(string? language)
    {
        var result = new LocalizedLessonSetupResolver().Resolve(Scenario(), language);
        Assert.Equal("canonical_english", result.Source);
        Assert.Equal("complete", result.Status);
    }

    [Fact]
    public void ResolverReturnsControlledIncompleteProjectionForUnsupportedPersistedLanguage()
    {
        var result = new LocalizedLessonSetupResolver().Resolve(Scenario(), "LegacyUnsupported");
        Assert.Equal(string.Empty, result.ResolvedStudyLanguageId);
        Assert.Equal("unsupported_study_language", result.Source);
        Assert.Equal("incomplete", result.Status);
        Assert.Null(result.SetupMessageTemplate);
        Assert.Empty(result.ContextVariantDisplayTitles);
    }

    [Fact]
    public void ResolverCopiesLocalizedTitleDictionaries()
    {
        var resolver = new LocalizedLessonSetupResolver();
        var scenario = Scenario();
        var first = resolver.Resolve(scenario, "Spanish");
        first.ContextVariantDisplayTitles["context-a"] = "Changed";
        var second = resolver.Resolve(scenario, "Spanish");
        Assert.Equal("Título español", scenario.SetupLocalizations!["es"].ContextVariantTitles["context-a"]);
        Assert.Equal("Título español", second.ContextVariantDisplayTitles["context-a"]);
    }

    [Theory]
    [InlineData("{\"setupLocalizations\":{\"xx\":{\"setupMessageTemplate\":\"Hi\",\"contextVariantTitles\":{\"context-a\":\"Uno\"}}}}")]
    [InlineData("{\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"\",\"contextVariantTitles\":{\"context-a\":\"Uno\"}}}}")]
    [InlineData("{\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{\"unknown\":\"Uno\"}}}}")]
    public void ValidationRejectsMalformedSuppliedLocalization(string localizationJson) =>
        Assert.NotEmpty(CmsScenarioDefinitionJson.ValidateDefinitionJson(RequiredScenarioJson(localizationJson), "scenario", true));

    [Theory]
    [InlineData("{\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{\"context-a\":\"Uno\"}},\"ES\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{\"context-a\":\"Uno\"}}}}")]
    [InlineData("{\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{}}}}")]
    public void ValidationRejectsIncompleteOrDuplicateEquivalentLocalization(string localizationJson) =>
        Assert.NotEmpty(CmsScenarioDefinitionJson.ValidateDefinitionJson(RequiredScenarioJson(localizationJson), "scenario", true));

    [Fact]
    public void ValidationRequiresExactVariantIdSpellingAndRejectsCaseConflicts()
    {
        var localization = "{\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{\"Context-A\":\"Uno\"}}}}";
        Assert.NotEmpty(CmsScenarioDefinitionJson.ValidateDefinitionJson(RequiredScenarioJson(localization), "scenario", true));

        var conflicting = RequiredScenarioJson(string.Empty).Replace("{\"id\":\"context-a\",\"title\":\"English title\"}", "{\"id\":\"context-a\",\"title\":\"English title\"},{\"id\":\"Context-A\",\"title\":\"Conflict\"}");
        Assert.NotEmpty(CmsScenarioDefinitionJson.ValidateDefinitionJson(conflicting.Replace("\"aiTutorPromptInstructions\":[\"Rule\"]", "\"aiTutorPromptInstructions\":[\"Rule\"],\"setupLocalizations\":{}"), "scenario", true));
    }

    [Fact]
    public void ValidationAcceptsExactCoverageAndNoVariantScenario()
    {
        var valid = "{\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{\"context-a\":\"Uno\"}}}}";
        Assert.Empty(CmsScenarioDefinitionJson.ValidateDefinitionJson(RequiredScenarioJson(valid), "scenario", true));
        var multiVariant = RequiredScenarioJson("{\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{\"context-a\":\"Uno\",\"context-b\":\"Dos\"}}}}")
            .Replace("[{\"id\":\"context-a\",\"title\":\"English title\"}]", "[{\"id\":\"context-a\",\"title\":\"English title\"},{\"id\":\"context-b\",\"title\":\"Second title\"}]");
        Assert.Empty(CmsScenarioDefinitionJson.ValidateDefinitionJson(multiVariant, "scenario", true));
        var partialMultiVariant = RequiredScenarioJson(valid)
            .Replace("[{\"id\":\"context-a\",\"title\":\"English title\"}]", "[{\"id\":\"context-a\",\"title\":\"English title\"},{\"id\":\"context-b\",\"title\":\"Second title\"}]");
        Assert.NotEmpty(CmsScenarioDefinitionJson.ValidateDefinitionJson(partialMultiVariant, "scenario", true));
        var noVariants = RequiredScenarioJson("{\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{}}}}").Replace("[{\"id\":\"context-a\",\"title\":\"English title\"}]", "[]");
        Assert.Empty(CmsScenarioDefinitionJson.ValidateDefinitionJson(noVariants, "scenario", true));
    }

    [Fact]
    public void LegacyAndLocalizedScenarioJsonDeserializeAndSerialize()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var legacy = JsonSerializer.Deserialize<LessonScenario>(RequiredScenarioJson(string.Empty), options);
        var localized = JsonSerializer.Deserialize<LessonScenario>(RequiredScenarioJson("{\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{\"context-a\":\"Uno\"}}}}"), options);
        Assert.Null(legacy!.SetupLocalizations);
        Assert.Equal("Hola", localized!.SetupLocalizations!["es"].SetupMessageTemplate);
        Assert.Contains("setupLocalizations", JsonSerializer.Serialize(localized, options), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("localizedSetup")]
    [InlineData("LocalizedSetup")]
    [InlineData("LOCALIZEDSETUP")]
    public void ValidationRejectsResponseOnlyLocalizedSetupRegardlessOfCase(string propertyName)
    {
        var forged = RequiredScenarioJson("{\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{\"context-a\":\"Uno\"}}}}")
            .Replace("\"aiTutorPromptInstructions\":[\"Rule\"]", $"\"aiTutorPromptInstructions\":[\"Rule\"],\"{propertyName}\":{{\"status\":\"complete\"}}");
        Assert.NotEmpty(CmsScenarioDefinitionJson.ValidateDefinitionJson(forged, "scenario", true));
    }

    [Fact]
    public void DefinitionDeserializationStripsForgedResponseProjectionAndPreservesAuthoredLocalization()
    {
        var forged = RequiredScenarioJson("{\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{\"context-a\":\"Uno\"}}}}")
            .Replace("\"aiTutorPromptInstructions\":[\"Rule\"]", "\"aiTutorPromptInstructions\":[\"Rule\"],\"localizedSetup\":{\"setupMessageTemplate\":\"Forged\"}");
        var lesson = CmsScenarioDefinitionJson.DeserializeLessonScenario(new CmsLessonScenarioEntity { StableScenarioKey = "scenario", DefinitionJson = forged });
        Assert.Null(lesson.LocalizedSetup);
        Assert.Equal("Hola", lesson.SetupLocalizations!["es"].SetupMessageTemplate);
        Assert.Equal("Hello", lesson.LessonSetup.SetupMessage);
        Assert.Equal("English title", lesson.ControlledVariation.ContextVariants[0].Title);
    }

    [Fact]
    public void DefinitionSerializationOmitsResponseOnlyLocalizedSetupWithoutMutatingCaller()
    {
        var scenario = Scenario();
        scenario.LocalizedSetup = new LocalizedLessonSetup { Status = "complete", SetupMessageTemplate = "Forged" };
        var definition = CmsScenarioDefinitionJson.SerializeDefinition(scenario);
        Assert.DoesNotContain("localizedSetup", definition, StringComparison.Ordinal);
        Assert.NotNull(scenario.LocalizedSetup);
        Assert.Contains("setupLocalizations", definition, StringComparison.Ordinal);
    }

    private static LessonScenario Scenario() => new()
    {
        LessonSetup = new LessonSetup { SetupMessage = "Hello {{userDisplayName}}" },
        ControlledVariation = new ControlledVariation { ContextVariants = [new ContextVariant { Id = "context-a", Title = "English title" }] },
        SetupLocalizations = new Dictionary<string, LessonSetupLocalization>(StringComparer.Ordinal) { ["es"] = new() { SetupMessageTemplate = "Hola {{userDisplayName}}", ContextVariantTitles = new Dictionary<string, string> { ["context-a"] = "Título español" } } }
    };

    private static string RequiredScenarioJson(string extra) => """
        {"id":"scenario","metadata":{"topic":"Topic","subtopic":"Title","lessonType":"guided_roleplay","supportedLevels":["A1"]},"lessonSetup":{"setupMessage":"Hello"},"learningGoal":{"goal":"Goal"},"targetLanguage":{"name":"English"},"levelProfiles":{"A1":{"level":"A1"}},"conversationFlow":{"opening":"Hi"},"controlledVariation":{"contextVariants":[{"id":"context-a","title":"English title"}]},"offTopicHandling":{"redirect":"Redirect"},"feedbackRules":{"feedbackStyle":"brief"},"hintRules":{"exampleHint":"Hint"},"aiTutorPromptInstructions":["Rule"]%s}
        """.Replace("%s", string.IsNullOrWhiteSpace(extra) ? string.Empty : "," + extra.Trim()[1..^1]);
}
