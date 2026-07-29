using System.Text.Json;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Api.Services.Cms;

namespace EnglishVoiceTutor.Api.Tests.Services.Cms;

public sealed class SetupLocalizationSnapshotTests
{
    [Fact]
    public void SnapshotBuilderPreservesAuthoredLocalizationAndExcludesResponseProjection()
    {
        var topic = new CmsLessonTopicEntity { Id = Guid.NewGuid(), StableTopicKey = "topic", Title = "Topic", IsActive = true };
        var definition = DefinitionJson(includeLocalization: true);
        var scenario = new CmsLessonScenarioEntity
        {
            Id = Guid.NewGuid(), TopicId = topic.Id, StableScenarioKey = "scenario", Title = "Title", LessonType = "guided_roleplay", DefinitionJson = definition, Topic = topic
        };
        var snapshot = CmsContentSnapshotBuilder.BuildSnapshotJson(
            new ContentPackEntity { Id = Guid.NewGuid(), Slug = "pack", Name = "Pack" }, [topic], [scenario], [], []);
        var published = CmsContentSnapshotBuilder.DeserializePublishedContent(snapshot);
        var lesson = Assert.Single(published.Scenarios).Lesson;

        Assert.Contains("setupLocalizations", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("localizedSetup", snapshot, StringComparison.Ordinal);
        Assert.Equal(definition, published.Scenarios[0].DefinitionJson);
        Assert.Equal("Hola", lesson.SetupLocalizations!["es"].SetupMessageTemplate);
        Assert.Equal("Uno", lesson.SetupLocalizations["es"].ContextVariantTitles["context-a"]);
        Assert.Equal("Hello", lesson.LessonSetup.SetupMessage);
        Assert.Equal("English title", lesson.ControlledVariation.ContextVariants[0].Title);

        var roundTrip = CmsContentSnapshotBuilder.DeserializePublishedContent(JsonSerializer.Serialize(published));
        Assert.Equal("Uno", roundTrip.Scenarios[0].Lesson.SetupLocalizations!["es"].ContextVariantTitles["context-a"]);
    }

    [Fact]
    public void SnapshotBuilderKeepsLegacyDefinitionReadable()
    {
        var topic = new CmsLessonTopicEntity { Id = Guid.NewGuid(), StableTopicKey = "topic", Title = "Topic", IsActive = true };
        var scenario = new CmsLessonScenarioEntity { Id = Guid.NewGuid(), TopicId = topic.Id, StableScenarioKey = "scenario", Title = "Title", LessonType = "guided_roleplay", DefinitionJson = DefinitionJson(includeLocalization: false), Topic = topic };
        var snapshot = CmsContentSnapshotBuilder.BuildSnapshotJson(new ContentPackEntity { Id = Guid.NewGuid(), Slug = "pack", Name = "Pack" }, [topic], [scenario], [], []);
        var published = CmsContentSnapshotBuilder.DeserializePublishedContent(snapshot);
        Assert.Null(published.Scenarios[0].Lesson.SetupLocalizations);
        Assert.DoesNotContain("localizedSetup", snapshot, StringComparison.Ordinal);
    }

    [Fact]
    public void ForgedResponseProjectionIsRejectedAndStrippedBeforeTypedSnapshotUse()
    {
        var forged = DefinitionJson(includeLocalization: true).Replace("\"aiTutorPromptInstructions\":[\"Rule\"]", "\"aiTutorPromptInstructions\":[\"Rule\"],\"localizedSetup\":{\"status\":\"complete\"}");
        Assert.NotEmpty(CmsScenarioDefinitionJson.ValidateDefinitionJson(forged, "scenario", true));
        var lesson = CmsScenarioDefinitionJson.DeserializeLessonScenario(new CmsLessonScenarioEntity { StableScenarioKey = "scenario", DefinitionJson = forged });
        Assert.Null(lesson.LocalizedSetup);
        Assert.Equal("Hola", lesson.SetupLocalizations!["es"].SetupMessageTemplate);
    }

    private static string DefinitionJson(bool includeLocalization) => """
        {"id":"scenario","metadata":{"topic":"Topic","subtopic":"Title","lessonType":"guided_roleplay","supportedLevels":["A1"]},"lessonSetup":{"setupMessage":"Hello"},"learningGoal":{"goal":"Goal"},"targetLanguage":{"name":"English"},"levelProfiles":{"A1":{"level":"A1"}},"conversationFlow":{"opening":"Hi"},"controlledVariation":{"contextVariants":[{"id":"context-a","title":"English title"}]},"offTopicHandling":{"redirect":"Redirect"},"feedbackRules":{"feedbackStyle":"brief"},"hintRules":{"exampleHint":"Hint"},"aiTutorPromptInstructions":["Rule"]%s}
        """.Replace("%s", includeLocalization ? ",\"setupLocalizations\":{\"es\":{\"setupMessageTemplate\":\"Hola\",\"contextVariantTitles\":{\"context-a\":\"Uno\"}}}" : string.Empty);
}
