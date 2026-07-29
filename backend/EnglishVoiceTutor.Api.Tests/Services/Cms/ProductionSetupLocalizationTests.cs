using System.Text.Json;
using EnglishVoiceTutor.Api.Contracts.Cms;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Api.Services.Cms;
using EnglishVoiceTutor.Shared.StudyLanguages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishVoiceTutor.Api.Tests.Services.Cms;

public sealed class ProductionSetupLocalizationTests
{
    [Fact]
    public void ActiveCanonicalLessonsContainTheCompleteApprovedSetupLocalizationPack()
    {
        var files = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "Content", "Lessons"), "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var requiredLanguageIds = StudyLanguageCatalog.All
            .Where(language => !language.IsDefault)
            .Select(language => language.Id)
            .ToArray();

        Assert.Equal(26, files.Length);
        Assert.Equal(["fr", "de", "pt", "es", "it"], requiredLanguageIds);

        var scenarioIds = new HashSet<string>(StringComparer.Ordinal);
        var setupMessageCount = 0;
        var contextTitleCount = 0;

        foreach (var file in files)
        {
            var definition = File.ReadAllText(file);
            using var document = JsonDocument.Parse(definition);
            var root = document.RootElement;
            var scenarioId = root.GetProperty("id").GetString();
            Assert.True(scenarioIds.Add(scenarioId!), $"Duplicate scenario id '{scenarioId}'.");
            Assert.False(root.TryGetProperty("localizedSetup", out _));
            Assert.Empty(CmsScenarioDefinitionJson.ValidateDefinitionJson(definition, scenarioId!, requireNonEmpty: true));
            Assert.Empty(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(definition, scenarioId!));

            var contextIds = root.GetProperty("controlledVariation").GetProperty("contextVariants")
                .EnumerateArray()
                .Select(variant => variant.GetProperty("id").GetString()!)
                .ToHashSet(StringComparer.Ordinal);
            var englishPlaceholders = Placeholders(root.GetProperty("lessonSetup").GetProperty("setupMessage").GetString());
            var localizations = root.GetProperty("setupLocalizations");
            Assert.Equal(requiredLanguageIds.OrderBy(id => id, StringComparer.Ordinal), localizations.EnumerateObject().Select(property => property.Name).OrderBy(id => id, StringComparer.Ordinal));

            foreach (var languageId in requiredLanguageIds)
            {
                var localization = localizations.GetProperty(languageId);
                var template = localization.GetProperty("setupMessageTemplate").GetString();
                Assert.False(string.IsNullOrWhiteSpace(template));
                Assert.Equal(englishPlaceholders, Placeholders(template));
                setupMessageCount++;

                var titles = localization.GetProperty("contextVariantTitles");
                Assert.Equal(contextIds.OrderBy(id => id, StringComparer.Ordinal), titles.EnumerateObject().Select(property => property.Name).OrderBy(id => id, StringComparer.Ordinal));
                foreach (var title in titles.EnumerateObject())
                {
                    Assert.False(string.IsNullOrWhiteSpace(title.Value.GetString()));
                    contextTitleCount++;
                }
            }
        }

        Assert.Equal(130, setupMessageCount);
        Assert.Equal(625, contextTitleCount);
    }

    [Fact]
    public void PublicationValidationAllowsIncompleteDraftDefinitionsButRejectsEveryIncompleteLocalizationShape()
    {
        var incompleteDraft = DefinitionJson("{}");
        Assert.Empty(CmsScenarioDefinitionJson.ValidateDefinitionJson(incompleteDraft, "scenario", requireNonEmpty: true));
        Assert.Contains(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(incompleteDraft, "scenario"), error => error.Contains("missing required setup localization language 'fr'", StringComparison.Ordinal));

        Assert.Contains(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(DefinitionJson("{\"setupLocalizations\":{\"fr\":{\"setupMessageTemplate\":\"Bonjour {{userDisplayName}}\",\"contextVariantTitles\":{\"context-a\":\"Un\"}}}}"), "scenario"), error => error.Contains("missing required setup localization language 'de'", StringComparison.Ordinal));
        Assert.Contains(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(DefinitionJson(CompleteLocalizations("")), "scenario"), error => error.Contains("missing a non-empty setupMessageTemplate", StringComparison.Ordinal));
        Assert.Contains(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(DefinitionJson(CompleteLocalizations("\"contextVariantTitles\":{}")), "scenario"), error => error.Contains("missing context variant ID 'context-a'", StringComparison.Ordinal));
        Assert.Contains(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(DefinitionJson(CompleteLocalizations("\"contextVariantTitles\":{\"unknown\":\"Unknown\",\"context-a\":\"One\"}")), "scenario"), error => error.Contains("unknown context variant ID 'unknown'", StringComparison.Ordinal));
        Assert.Contains(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(DefinitionJson(CompleteLocalizations("\"contextVariantTitles\":{\"context-a\":\"\"}")), "scenario"), error => error.Contains("blank context title", StringComparison.Ordinal));
        Assert.Contains(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(DefinitionJson(CompleteLocalizations("\"setupMessageTemplate\":\"Hola\"")), "scenario"), error => error.Contains("missing required placeholder '{{userDisplayName}}'", StringComparison.Ordinal));
        Assert.Contains(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(DefinitionJson(CompleteLocalizations("\"setupMessageTemplate\":\"Hola {{userDisplayName}} {{unsupported}}\"")), "scenario"), error => error.Contains("unsupported placeholder '{{unsupported}}'", StringComparison.Ordinal));

        Assert.Empty(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(DefinitionJson(CompleteLocalizations()), "scenario"));
        Assert.Empty(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(
            DefinitionJson(CompleteLocalizations("\"setupMessageTemplate\":\"Hola {{userDisplayName}}\",\"contextVariantTitles\":{}"), noContextVariants: true),
            "scenario"));
    }

    [Fact]
    public async Task PublishingUsesThePublicationOnlyCompletenessValidation()
    {
        await using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var pack = new ContentPackEntity { Id = Guid.NewGuid(), Slug = "pack", Name = "Pack" };
        dbContext.ContentPacks.Add(pack);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var validation = new PublicationValidation();
        var response = await new CmsContentPublishingService(dbContext, validation).PublishDraftAsync(
            pack.Slug,
            new PublishCmsContentRequest { ChangeSummary = "Attempt publication" },
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.True(validation.PublicationValidationCalled);
        Assert.False(validation.DraftValidationCalled);
        Assert.Contains(response.Validation.Errors, error => error.Contains("missing required setup localization language 'fr'", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PublishingAcceptsACompleteActiveScenario()
    {
        await using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);
        var pack = new ContentPackEntity { Id = Guid.NewGuid(), Slug = "pack", Name = "Pack" };
        var topic = new CmsLessonTopicEntity { Id = Guid.NewGuid(), ContentPackId = pack.Id, StableTopicKey = "topic", Title = "Topic", IsActive = true };
        dbContext.AddRange(pack, topic, CompleteActiveScenario(pack.Id, topic.Id));
        dbContext.TutorBehaviorProfiles.AddRange(new[] { "david", "lana", "nelli" }.Select(tutorId => new TutorBehaviorProfileEntity
        {
            Id = Guid.NewGuid(),
            ContentPackId = pack.Id,
            TutorId = tutorId,
            DisplayName = tutorId,
            CommunicationStyleJson = "{}",
            SafetyNotesJson = "{}",
            IsActive = true
        }));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = await new CmsContentPublishingService(dbContext, new CmsContentValidationService(dbContext)).PublishDraftAsync(
            pack.Slug,
            new PublishCmsContentRequest { ChangeSummary = "Publish complete localizations" },
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.True(response.Success, string.Join(Environment.NewLine, response.Errors.Concat(response.Validation.Errors)));
        Assert.True(response.Created);
    }

    private static HashSet<string> Placeholders(string? text) => System.Text.RegularExpressions.Regex.Matches(text ?? string.Empty, @"\{\{[^{}]+\}\}")
        .Select(match => match.Value)
        .ToHashSet(StringComparer.Ordinal);

    private static string CompleteLocalizations(string? replacement = null)
    {
        var propertyBody = replacement ?? "\"setupMessageTemplate\":\"Hola {{userDisplayName}}\",\"contextVariantTitles\":{\"context-a\":\"One\"}";
        return "{\"setupLocalizations\":{" + string.Join(',', new[] { "fr", "de", "pt", "es", "it" }.Select(language => $"\"{language}\":{{{propertyBody}}}")) + "}}";
    }

    private static string DefinitionJson(string localizationJson, bool noContextVariants = false)
    {
        var contextVariants = noContextVariants ? "[]" : "[{\"id\":\"context-a\",\"title\":\"English title\"}]";
        var localizationBody = localizationJson.Trim()[1..^1];
        return "{\"id\":\"scenario\",\"metadata\":{\"topic\":\"Topic\",\"subtopic\":\"Title\",\"lessonType\":\"guided_roleplay\",\"supportedLevels\":[\"A1\"]},\"lessonSetup\":{\"setupMessage\":\"Hello {{userDisplayName}}\"},\"learningGoal\":{\"goal\":\"Goal\"},\"targetLanguage\":{\"name\":\"English\"},\"levelProfiles\":{\"A1\":{\"level\":\"A1\"}},\"conversationFlow\":{\"opening\":\"Hi\"},\"controlledVariation\":{\"contextVariants\":"
            + contextVariants
            + "},\"offTopicHandling\":{\"redirect\":\"Redirect\"},\"feedbackRules\":{\"feedbackStyle\":\"brief\"},\"hintRules\":{\"exampleHint\":\"Hint\"},\"aiTutorPromptInstructions\":[\"Rule\"]"
            + (string.IsNullOrWhiteSpace(localizationBody) ? string.Empty : "," + localizationBody)
            + "}";
    }

    private static CmsLessonScenarioEntity CompleteActiveScenario(Guid contentPackId, Guid topicId) => new()
    {
        Id = Guid.NewGuid(),
        ContentPackId = contentPackId,
        TopicId = topicId,
        StableScenarioKey = "scenario",
        Title = "Title",
        LessonType = "guided_roleplay",
        SetupMessage = "Hello {{userDisplayName}}",
        SupportedLevelIdsJson = "[]",
        ContextSelectionJson = "{}",
        LearningGoalJson = "{}",
        SituationJson = "{}",
        RolesJson = "{}",
        TargetLanguageJson = "{}",
        LevelProfilesJson = "{}",
        ConversationFlowJson = "{}",
        RoleplayBeatsJson = "[]",
        ReciprocalQuestionHandlingJson = "{}",
        ExpectedScenarioProgressionJson = "[]",
        ControlledVariationJson = "{}",
        OffTopicHandlingJson = "{}",
        FeedbackRulesJson = "{}",
        HintRulesJson = "{}",
        RepetitionLogicJson = "{}",
        AiTutorPromptInstructionsJson = "[]",
        DefinitionJson = DefinitionJson(CompleteLocalizations()),
        IsActive = true
    };

    private sealed class PublicationValidation : ICmsContentValidationService
    {
        public bool DraftValidationCalled { get; private set; }
        public bool PublicationValidationCalled { get; private set; }

        public CmsContentValidationResult Validate(CmsStaticContentImportDraft draft) => new();

        public Task<CmsContentValidationResult> ValidateDraftRowsAsync(Guid contentPackId, CancellationToken cancellationToken)
        {
            DraftValidationCalled = true;
            return Task.FromResult(new CmsContentValidationResult());
        }

        public Task<CmsContentValidationResult> ValidateDraftRowsForPublicationAsync(Guid contentPackId, CancellationToken cancellationToken)
        {
            PublicationValidationCalled = true;
            return Task.FromResult(new CmsContentValidationResult { Errors = ["Scenario 'scenario' is missing required setup localization language 'fr' for publication."] });
        }
    }
}
