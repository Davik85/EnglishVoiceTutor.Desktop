using System.Text.Json;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Models.RealtimeVoice;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Cms;
using EnglishVoiceTutor.Desktop.Models.LessonContent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class TutorBehaviorProfileResolverTests
{
    [Fact]
    public async Task PublishedProfileControlsChatFeedbackHintRealtimeAndIdentityGuard()
    {
        var resolver = CreateResolver(Published("cms-style-only", "CmsLana"));
        var profile = await resolver.ResolveAsync("lana", TestContext.Current.CancellationToken);
        var builder = CreateBuilder();
        var clientRequest = new LessonChatRequest
        {
            TutorAvatarId = "lana",
            TutorDisplayName = "Spoof",
            SelectedLevel = "A1",
            UserMessage = "Hello"
        };
        var chat = builder.BuildInput(clientRequest, profile);
        Assert.Contains("You are CmsLana.", chat);
        Assert.Contains("cms-style-only", chat);
        Assert.DoesNotContain("Spoof", chat);

        var feedback = builder.BuildInput(new LessonChatRequest
        {
            TutorAvatarId = "lana",
            TutorDisplayName = "Spoof",
            SelectedLevel = "A1",
            RequestPurpose = "feedback",
            UserMessage = "Hello"
        }, profile);
        Assert.Contains("cms-style-only", feedback);
        Assert.DoesNotContain("Spoof", feedback);

        var hint = builder.BuildHintInput(clientRequest, profile);
        Assert.Contains("cms-style-only", hint);
        Assert.DoesNotContain("Spoof", hint);

        var realtimeRequest = new RealtimeVoiceSessionStartRequest
        {
            TutorProfileId = "lana",
            TutorDisplayName = "Spoof",
            TutorProfileAge = 99,
            TutorProfileHomeCity = "Spoof City",
            TutorProfileStudies = "spoofing",
            TutorProfileCommunicationStyle = ["spoof-style"],
            SelectedLevel = "A1"
        };
        var sessionInstructions = builder.BuildRealtimeInstructions(realtimeRequest, profile);
        var responseInstructions = builder.BuildRealtimeResponseInstructions(realtimeRequest, profile);
        Assert.Contains("cms-style-only", sessionInstructions);
        Assert.Contains("You are CmsLana.", sessionInstructions);
        Assert.DoesNotContain("spoof-style", sessionInstructions);
        Assert.DoesNotContain("Spoof City", sessionInstructions);
        Assert.Contains("Respond now as CmsLana", responseInstructions);
        Assert.DoesNotContain("Spoof", responseInstructions);

        var guard = new TutorIdentityGuard(NullLogger<TutorIdentityGuard>.Instance);
        Assert.Equal("Hello, I'm CmsLana.", guard.PreventWrongTutorSelfIntroduction(
            new LessonChatResponse { BotReply = "Hello, I'm David." }, profile).BotReply);
    }

    [Fact]
    public async Task PublishedCmsBehaviorUsesStaticBiographyWhenSnapshotBiographyIsMissing()
    {
        var staticProfiles = StaticProfiles();
        var staticProfile = staticProfiles.GetById("lana");
        var runtime = Published("cms-style-only", "CmsLana");
        var publishedProfile = Assert.Single(runtime.Content!.TutorBehaviorProfiles).TutorProfile;
        Assert.Equal(0, publishedProfile.Age);
        Assert.Empty(publishedProfile.HomeCity);
        Assert.Empty(publishedProfile.CountryOrRegion);
        Assert.Empty(publishedProfile.Studies);
        Assert.Empty(publishedProfile.Hobbies);

        var resolved = await new TutorBehaviorProfileResolver(new FakeRuntimeService(runtime), staticProfiles)
            .ResolveAsync("lana", TestContext.Current.CancellationToken);

        Assert.Equal("CmsLana", resolved.DisplayName);
        Assert.Equal(["cms-style-only"], resolved.CommunicationStyle);
        Assert.Equal(publishedProfile.SpeakingRules, resolved.SpeakingRules);
        Assert.Equal(publishedProfile.IdentityRules, resolved.IdentityRules);
        Assert.Equal(staticProfile.Age, resolved.Age);
        Assert.Equal(staticProfile.HomeCity, resolved.HomeCity);
        Assert.Equal(staticProfile.CountryOrRegion, resolved.CountryOrRegion);
        Assert.Equal(staticProfile.Studies, resolved.Studies);
        Assert.Equal(staticProfile.Hobbies, resolved.Hobbies);
        Assert.NotSame(staticProfile.Hobbies, resolved.Hobbies);
        Assert.Equal("Lana", staticProfile.DisplayName);
    }

    [Fact]
    public async Task InactivePublishedProfileUsesStaticFallback()
    {
        var resolved = await CreateResolver(Published("cms-style-only", "CmsLana", isActive: false))
            .ResolveAsync("lana", TestContext.Current.CancellationToken);

        AssertSameProfile(StaticProfiles().GetById("lana"), resolved);
    }

    [Fact]
    public async Task MissingOrInvalidPublishedProfileUsesWholeStaticProfile()
    {
        foreach (var runtime in new[]
        {
            new CmsRuntimeLessonContentReadResult { Success = false },
            Published("cms-style-only", "CmsLana", validSpeakingRules: false),
            Published("cms-style-only", "CmsLana", includeTutor: false)
        })
        {
            var profile = await CreateResolver(runtime).ResolveAsync("lana", TestContext.Current.CancellationToken);
            var staticProfile = StaticProfiles().GetById("lana");
            AssertSameProfile(staticProfile, profile);
            Assert.Contains("You are Lana.", CreateBuilder().BuildInput(new LessonChatRequest
            {
                TutorAvatarId = "lana", UserMessage = "Hello", SelectedLevel = "A1"
            }, profile));
            Assert.Contains("You are Lana.", CreateBuilder().BuildRealtimeInstructions(new RealtimeVoiceSessionStartRequest
            {
                TutorProfileId = "lana", TutorDisplayName = "Spoof", SelectedLevel = "A1"
            }, profile));
        }
    }

    [Fact]
    public async Task LegacyAliasResolvesPublishedCanonicalIdentityAndUnknownIdKeepsStaticFallback()
    {
        var resolver = CreateResolver(Published("cms-style-only", "CmsLana"));
        Assert.Equal("CmsLana", (await resolver.ResolveAsync("elena", TestContext.Current.CancellationToken)).DisplayName);
        Assert.Equal("lana", (await resolver.ResolveAsync("LANA", TestContext.Current.CancellationToken)).Id);
        Assert.Equal("unknown", (await resolver.ResolveAsync("unknown", TestContext.Current.CancellationToken)).Id);
    }

    [Fact]
    public async Task ExistingDesktopAndAndroidRequestShapesNeedOnlyTutorIdentifiers()
    {
        var desktop = JsonSerializer.Deserialize<LessonChatRequest>("""
            {"tutorAvatarId":"lana","tutorDisplayName":"Client Name","userMessage":"Hello"}
            """, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var android = JsonSerializer.Deserialize<LessonChatRequest>("""
            {"tutorAvatarId":"lana","tutorDisplayName":"Lana","userMessage":"Hello"}
            """, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var realtime = JsonSerializer.Deserialize<RealtimeVoiceSessionStartRequest>("""
            {"tutorProfileId":"lana","tutorDisplayName":"Client Name"}
            """, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var resolver = CreateResolver(Published("cms-style-only", "CmsLana"));
        Assert.Equal("CmsLana", (await resolver.ResolveAsync(desktop.TutorAvatarId, TestContext.Current.CancellationToken)).DisplayName);
        Assert.Equal("CmsLana", (await resolver.ResolveAsync(android.TutorAvatarId, TestContext.Current.CancellationToken)).DisplayName);
        Assert.Equal("CmsLana", (await resolver.ResolveAsync(realtime.TutorProfileId, TestContext.Current.CancellationToken)).DisplayName);
    }

    [Fact]
    public async Task DraftTutorRowWithoutPublishedVersionIsInaccessibleToPublishedReader()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        var pack = new ContentPackEntity { Id = Guid.NewGuid(), Slug = "static-json-v1", Name = "Test", Status = "Draft" };
        db.ContentPacks.Add(pack);
        db.TutorBehaviorProfiles.Add(new TutorBehaviorProfileEntity
        {
            Id = Guid.NewGuid(), ContentPackId = pack.Id, TutorId = "lana",
            DisplayName = "DraftOnly", CommunicationStyleJson = "{}", SafetyNotesJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var reader = new CmsPublishedContentService(db,
            Microsoft.Extensions.Options.Options.Create(new CmsContentOptions()),
            NullLogger<CmsPublishedContentService>.Instance);

        var result = await reader.ReadLatestPublishedContentAsync(TestContext.Current.CancellationToken);

        Assert.True(result.FallbackUsed);
        Assert.Null(result.Content);
        Assert.Equal(CmsContentConstants.Sources.StaticJsonFallback, result.Source);
    }

    [Fact]
    public async Task PublishedSnapshotRemainsAuthoritativeAfterDraftTutorEdit()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        var pack = new ContentPackEntity { Id = Guid.NewGuid(), Slug = "static-json-v1", Name = "Test", Status = "Published" };
        var content = new CmsPublishedLessonContent
        {
            Topics = [new CmsPublishedLessonTopic { StableTopicKey = "topic", Title = "Topic" }],
            Scenarios = [new CmsPublishedLessonScenario
            {
                StableScenarioKey = "scenario", TopicKey = "topic", Title = "Scenario",
                Lesson = new LessonScenario
                {
                    Id = "scenario",
                    Metadata = new LessonMetadata { SupportedLevels = ["a1"] },
                    LessonSetup = new LessonSetup { SetupMessage = "Begin" }
                }
            }],
            PromptTemplates = [new CmsPublishedPromptTemplate { TemplateKey = "base", Body = "Prompt" }],
            TutorBehaviorProfiles = Published("published-only", "CmsLana").Content!.TutorBehaviorProfiles,
            LevelProfiles = [.. CmsLevelProfiles.Defaults]
        };
        var snapshotJson = CmsContentJson.SerializeDeterministic(content);
        var hash = CmsContentJson.Sha256Hex(snapshotJson);
        var version = new ContentVersionEntity
        {
            Id = Guid.NewGuid(), ContentPackId = pack.Id, ContentPack = pack,
            VersionNumber = 1, PublishStatus = CmsContentConstants.ContentVersionPublishStatuses.Published,
            SnapshotHash = hash
        };
        var snapshot = new PublishedContentSnapshotEntity
        {
            Id = Guid.NewGuid(), ContentVersionId = version.Id, ContentVersion = version,
            SnapshotJson = snapshotJson, SnapshotHash = hash
        };
        version.PublishedSnapshot = snapshot;
        db.ContentPacks.Add(pack);
        db.ContentVersions.Add(version);
        db.PublishedContentSnapshots.Add(snapshot);
        db.TutorBehaviorProfiles.Add(new TutorBehaviorProfileEntity
        {
            Id = Guid.NewGuid(), ContentPackId = pack.Id, TutorId = "lana",
            DisplayName = "DraftOnly", CommunicationStyleJson = "{}", SafetyNotesJson = "{}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var reader = new CmsPublishedContentService(db,
            Microsoft.Extensions.Options.Options.Create(new CmsContentOptions()),
            NullLogger<CmsPublishedContentService>.Instance);

        var result = await reader.ReadLatestPublishedContentAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.False(result.FallbackUsed);
        Assert.Equal("CmsLana", Assert.Single(result.Content!.TutorBehaviorProfiles).DisplayName);
        Assert.Equal("published-only", Assert.Single(result.Content.TutorBehaviorProfiles[0].TutorProfile.CommunicationStyle));
    }

    private static TutorBehaviorProfileResolver CreateResolver(CmsRuntimeLessonContentReadResult runtime) =>
        new(new FakeRuntimeService(runtime), StaticProfiles());

    private static TutorAvatarProfileProvider StaticProfiles() =>
        new(NullLogger<TutorAvatarProfileProvider>.Instance);

    private static LessonPromptBuilder CreateBuilder() => new(StaticProfiles());

    private static CmsRuntimeLessonContentReadResult Published(
        string style, string name, bool validSpeakingRules = true, bool includeTutor = true, bool isActive = true) => new()
    {
        Success = true,
        Source = CmsContentConstants.Sources.CmsPublishedSnapshot,
        Content = new CmsRuntimeLessonContent
        {
            TutorBehaviorProfiles = includeTutor ? [new CmsPublishedTutorBehaviorProfile
            {
                TutorId = "lana", DisplayName = name, IsActive = isActive,
                TutorProfile = new TutorProfile
                {
                    Id = "lana", DisplayName = name,
                    CommunicationStyle = [style], IdentityRules = ["Always use this identity."],
                    SpeakingRules = validSpeakingRules
                        ? new Dictionary<string, string> { ["a1"] = "short", ["a2"] = "short", ["b1"] = "natural", ["b2"] = "nuanced" }
                        : new Dictionary<string, string>()
                }
            }] : []
        }
    };

    private static void AssertSameProfile(TutorAvatarProfile expected, TutorAvatarProfile actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.DisplayName, actual.DisplayName);
        Assert.Equal(expected.Age, actual.Age);
        Assert.Equal(expected.HomeCity, actual.HomeCity);
        Assert.Equal(expected.CountryOrRegion, actual.CountryOrRegion);
        Assert.Equal(expected.Studies, actual.Studies);
        Assert.Equal(expected.Hobbies, actual.Hobbies);
        Assert.Equal(expected.CommunicationStyle, actual.CommunicationStyle);
        Assert.Equal(expected.SpeakingRules, actual.SpeakingRules);
        Assert.Equal(expected.IdentityRules, actual.IdentityRules);
    }

    private sealed class FakeRuntimeService(CmsRuntimeLessonContentReadResult runtime) : ICmsRuntimeLessonContentService
    {
        public Task<CmsRuntimeLessonContentReadResult> ReadRuntimeLessonContentAsync(CancellationToken cancellationToken) =>
            Task.FromResult(runtime);
    }
}
