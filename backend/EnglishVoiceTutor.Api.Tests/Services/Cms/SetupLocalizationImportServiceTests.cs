using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Api.Services.Cms;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services.Cms;

public sealed class SetupLocalizationImportServiceTests
{
    [Fact]
    public async Task PreviewReportsExactCanonicalCountsAndPerformsNoWrites()
    {
        await using var fixture = await CreateOldDraftAsync();
        var before = await fixture.Db.CmsLessonScenarios.Select(row => row.DefinitionJson).ToArrayAsync(TestContext.Current.CancellationToken);

        var preview = await fixture.Service.PreviewSetupLocalizationsImportAsync(TestContext.Current.CancellationToken);

        Assert.True(preview.SafeToApply);
        Assert.Equal(26, preview.PackagedScenarioCount);
        Assert.Equal(26, preview.CmsDraftScenarioCount);
        Assert.Equal(26, preview.MatchedScenarioCount);
        Assert.Equal(130, preview.TemplateCount);
        Assert.Equal(625, preview.ContextTitleCount);
        Assert.Equal(0, await fixture.Db.ContentAuditLogs.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(before, await fixture.Db.CmsLessonScenarios.Select(row => row.DefinitionJson).ToArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApplyOnlyAddsLocalizationsPreservesOtherContentAndIsIdempotent()
    {
        await using var fixture = await CreateOldDraftAsync();
        var before = await fixture.Db.CmsLessonScenarios.OrderBy(row => row.StableScenarioKey).ToListAsync(TestContext.Current.CancellationToken);

        var result = await fixture.Service.ImportSetupLocalizationsAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(26, result.ScenariosUpdated);
        Assert.Equal(130, result.LanguageBlocksAdded);
        Assert.Equal(130, result.TemplatesAdded);
        Assert.Equal(625, result.ContextTitlesAdded);
        Assert.False(result.PublishedVersionChanged);
        Assert.False(result.PublishedSnapshotCreated);
        Assert.False(result.RuntimeChanged);
        Assert.Equal(0, await fixture.Db.ContentVersions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await fixture.Db.PublishedContentSnapshots.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(26, await fixture.Db.ContentAuditLogs.CountAsync(TestContext.Current.CancellationToken));
        var auditJson = string.Join("\n", await fixture.Db.ContentAuditLogs.Select(log => log.RequestMetadataJson).ToArrayAsync(TestContext.Current.CancellationToken));
        Assert.Contains("scenarioKey", auditJson, StringComparison.Ordinal);
        Assert.Contains("languageIds", auditJson, StringComparison.Ordinal);
        Assert.DoesNotContain("setupMessageTemplate", auditJson, StringComparison.Ordinal);

        var after = await fixture.Db.CmsLessonScenarios.OrderBy(row => row.StableScenarioKey).ToListAsync(TestContext.Current.CancellationToken);
        foreach (var pair in before.Zip(after))
        {
            var oldRoot = JsonNode.Parse(pair.First.DefinitionJson!)!.AsObject();
            var newRoot = JsonNode.Parse(pair.Second.DefinitionJson!)!.AsObject();
            oldRoot.Remove("setupLocalizations"); newRoot.Remove("setupLocalizations");
            Assert.True(JsonNode.DeepEquals(oldRoot, newRoot));
            Assert.Equal(pair.First.SetupMessage, pair.Second.SetupMessage);
            Assert.Equal(pair.First.Title, pair.Second.Title);
            Assert.Equal(pair.First.Description, pair.Second.Description);
            Assert.Equal(pair.First.LessonType, pair.Second.LessonType);
            Assert.Equal(pair.First.IsActive, pair.Second.IsActive);
            Assert.True(pair.Second.UpdatedAtUtc >= pair.First.UpdatedAtUtc);
            Assert.Equal("kept", JsonNode.Parse(pair.Second.DefinitionJson!)!["futureField"]!.GetValue<string>());
        }
        var freeConversation = after.Single(row => row.StableScenarioKey == "free_conversation_open_conversation");
        var freeLocalizations = JsonNode.Parse(freeConversation.DefinitionJson!)!["setupLocalizations"]!.AsObject();
        Assert.Contains("{{userDisplayName}}", freeLocalizations["fr"]!["setupMessageTemplate"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.Empty(freeLocalizations["fr"]!["contextVariantTitles"]!.AsObject());

        var second = await fixture.Service.ImportSetupLocalizationsAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.True(second.Success);
        Assert.Equal(0, second.ScenariosUpdated);
        Assert.Equal(0, second.LanguageBlocksAdded);
        Assert.Equal(26, await fixture.Db.ContentAuditLogs.CountAsync(TestContext.Current.CancellationToken));
        Assert.Empty(CmsScenarioDefinitionJson.ValidateSetupLocalizationsForPublication(freeConversation.DefinitionJson, freeConversation.StableScenarioKey));
    }

    [Theory]
    [InlineData("{\"bad\":true}")]
    [InlineData("\"bad\"")]
    public async Task MalformedOrDifferingExistingLocalizationsBlockEveryScenarioWrite(string setupLocalizations)
    {
        await using var fixture = await CreateOldDraftAsync();
        var target = await fixture.Db.CmsLessonScenarios.OrderBy(row => row.StableScenarioKey).FirstAsync(TestContext.Current.CancellationToken);
        var root = JsonNode.Parse(target.DefinitionJson!)!.AsObject();
        root["setupLocalizations"] = JsonNode.Parse(setupLocalizations);
        target.DefinitionJson = root.ToJsonString();
        await fixture.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var before = await fixture.Db.CmsLessonScenarios.Select(row => row.DefinitionJson).ToArrayAsync(TestContext.Current.CancellationToken);

        var result = await fixture.Service.ImportSetupLocalizationsAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(0, result.ScenariosUpdated);
        Assert.Equal(0, await fixture.Db.ContentAuditLogs.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(before, await fixture.Db.CmsLessonScenarios.Select(row => row.DefinitionJson).ToArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApplyRechecksAConflictIntroducedAfterEarlierSafePreview()
    {
        await using var fixture = await CreateOldDraftAsync();
        Assert.True((await fixture.Service.PreviewSetupLocalizationsImportAsync(TestContext.Current.CancellationToken)).SafeToApply);
        var target = await fixture.Db.CmsLessonScenarios.FirstAsync(TestContext.Current.CancellationToken);
        var root = JsonNode.Parse(target.DefinitionJson!)!.AsObject();
        root["setupLocalizations"] = new JsonObject { ["fr"] = new JsonObject { ["setupMessageTemplate"] = "Manually edited", ["contextVariantTitles"] = new JsonObject() } };
        target.DefinitionJson = root.ToJsonString();
        await fixture.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await fixture.Service.ImportSetupLocalizationsAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(0, result.ScenariosUpdated);
        Assert.Equal(0, await fixture.Db.ContentAuditLogs.CountAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<Fixture> CreateOldDraftAsync()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        var pack = new ContentPackEntity { Id = Guid.NewGuid(), Slug = "static-json-v1", Name = "Test", Status = "Draft", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow };
        var topic = new CmsLessonTopicEntity { Id = Guid.NewGuid(), ContentPackId = pack.Id, StableTopicKey = "test", Title = "Test", IsActive = true, CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow };
        db.ContentPacks.Add(pack); db.CmsLessonTopics.Add(topic);
        foreach (var path in Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "Content", "Lessons"), "*.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
        {
            var root = JsonNode.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken))!.AsObject();
            var id = root["id"]!.GetValue<string>();
            var title = root["metadata"]!["subtopic"]!.GetValue<string>();
            var lessonType = root["metadata"]!["lessonType"]!.GetValue<string>();
            var setup = root["lessonSetup"]!["setupMessage"]!.GetValue<string>();
            var description = root["situation"]!["description"]!.GetValue<string>();
            root.Remove("setupLocalizations"); root.Remove("localizedSetup"); root["futureField"] = "kept";
            db.CmsLessonScenarios.Add(new CmsLessonScenarioEntity { Id = Guid.NewGuid(), ContentPackId = pack.Id, TopicId = topic.Id, StableScenarioKey = id, Title = title, Description = description, LessonType = lessonType, SetupMessage = setup, DefinitionJson = root.ToJsonString(), IsActive = true, CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new Fixture(db, new CmsContentImportService(db, new CmsContentValidationService(db)));
    }

    private sealed record Fixture(AppDbContext Db, CmsContentImportService Service) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
