using System.Text.Json;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class AiModelSettingsServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lvt-ai-model-settings-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task HistoricalJsonWithoutTemperatureFlagsLoadsWithLegacyDefaults()
    {
        var releaseRoot = Path.Combine(_root, "backend", "releases", "0.1.35-backend.148");
        var persistentPath = Path.Combine(_root, "backend", "site", "content", "ai-model-settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(persistentPath)!);
        File.WriteAllText(persistentPath, """
        {
          "active": {
            "lessonTutorChatModel": "gpt-5.5",
            "feedbackCorrectionModel": "gpt-5.2-feedback",
            "lessonHintModel": "gpt-5.2-hint",
            "translationModel": "gpt-5.2-translation",
            "speechToTextModel": "gpt-4o-mini-transcribe",
            "lessonChatTextToSpeechModel": "tts-1",
            "conversationModeTextToSpeechModel": "gpt-4o-mini-tts",
            "realtimeVoiceModel": "gpt-realtime"
          },
          "draft": {
            "lessonTutorChatModel": "gpt-5.6-terra",
            "feedbackCorrectionModel": "gpt-5.2-feedback-draft",
            "lessonHintModel": "gpt-5.2-hint-draft",
            "translationModel": "gpt-5.2-translation-draft",
            "speechToTextModel": "gpt-4o-mini-transcribe",
            "lessonChatTextToSpeechModel": "tts-1",
            "conversationModeTextToSpeechModel": "gpt-4o-mini-tts",
            "realtimeVoiceModel": "gpt-realtime"
          },
          "updatedAtUtc": "2026-09-04T00:00:00+00:00",
          "updatedBy": "historical-admin",
          "revision": 12
        }
        """);

        var response = await CreateService(releaseRoot).GetAsync(CancellationToken.None);

        Assert.Equal("gpt-5.5", response.Active.LessonTutorChatModel);
        Assert.Equal("gpt-5.2-feedback", response.Active.FeedbackCorrectionModel);
        Assert.Equal("gpt-5.2-hint", response.Active.LessonHintModel);
        Assert.Equal("gpt-5.2-translation", response.Active.TranslationModel);
        Assert.Equal("gpt-5.6-terra", response.Draft.LessonTutorChatModel);
        Assert.False(response.Active.LessonTutorChatOmitTemperature);
        Assert.False(response.Active.FeedbackCorrectionOmitTemperature);
        Assert.False(response.Active.LessonHintOmitTemperature);
        Assert.False(response.Active.TranslationOmitTemperature);
        Assert.False(response.Draft.LessonTutorChatOmitTemperature);
        Assert.False(response.Draft.FeedbackCorrectionOmitTemperature);
        Assert.False(response.Draft.LessonHintOmitTemperature);
        Assert.False(response.Draft.TranslationOmitTemperature);
    }

    [Fact]
    public async Task DraftValidatePublishAndResetPreserveAllTemperatureFlags()
    {
        var releaseRoot = Path.Combine(_root, "backend", "releases", "0.1.35-backend.148");
        var service = CreateService(releaseRoot);
        var flagged = AiModelSettings.Defaults with
        {
            LessonTutorChatOmitTemperature = true,
            FeedbackCorrectionOmitTemperature = false,
            LessonHintOmitTemperature = true,
            TranslationOmitTemperature = true
        };

        var validation = service.Validate(flagged);
        var saved = await service.SaveDraftAsync(flagged, "test-admin", CancellationToken.None);
        var reloadedAfterSave = await CreateService(releaseRoot).GetAsync(CancellationToken.None);
        var published = await service.PublishAsync("test-admin", CancellationToken.None);
        await service.SaveDraftAsync(AiModelSettings.Defaults, "test-admin", CancellationToken.None);
        var reset = await service.ResetDraftFromActiveAsync("test-admin", CancellationToken.None);

        Assert.True(validation.IsValid);
        AssertTemperatureFlags(saved.Draft, true, false, true, true);
        AssertTemperatureFlags(reloadedAfterSave.Draft, true, false, true, true);
        AssertTemperatureFlags(published.Active, true, false, true, true);
        AssertTemperatureFlags(published.Draft, true, false, true, true);
        AssertTemperatureFlags(reset.Active, true, false, true, true);
        AssertTemperatureFlags(reset.Draft, true, false, true, true);
    }

    [Fact]
    public async Task PublishPersistsActiveSettingsOutsideReleaseContentRoot()
    {
        var releaseRoot = Path.Combine(_root, "backend", "releases", "0.1.35-backend.81");
        var service = CreateService(releaseRoot);
        var draft = AiModelSettings.Defaults with { LessonTutorChatModel = "gpt-5.5" };

        await service.SaveDraftAsync(draft, "test-admin", CancellationToken.None);
        await service.PublishAsync("test-admin", CancellationToken.None);

        var persistentPath = Path.Combine(_root, "backend", "site", "content", "ai-model-settings.json");
        var releasePath = Path.Combine(releaseRoot, "site", "content", "ai-model-settings.json");
        Assert.True(File.Exists(persistentPath));
        Assert.False(File.Exists(releasePath));
        Assert.Equal("gpt-5.5", service.GetActiveSettings().LessonTutorChatModel);
    }

    [Fact]
    public async Task ReleaseFolderChangeLoadsPreviouslyPublishedActiveSettings()
    {
        var oldReleaseRoot = Path.Combine(_root, "backend", "releases", "0.1.35-backend.80");
        var newReleaseRoot = Path.Combine(_root, "backend", "releases", "0.1.35-backend.81");
        var published = AiModelSettings.Defaults with { LessonTutorChatModel = "gpt-5.5" };

        await CreateService(oldReleaseRoot).SaveDraftAsync(published, "test-admin", CancellationToken.None);
        await CreateService(oldReleaseRoot).PublishAsync("test-admin", CancellationToken.None);

        var afterDeploy = CreateService(newReleaseRoot).GetActiveSettings();

        Assert.Equal("gpt-5.5", afterDeploy.LessonTutorChatModel);
    }

    [Fact]
    public void ExistingPersistentActiveSettingsAreNotOverwrittenByPackagedDefaults()
    {
        var releaseRoot = Path.Combine(_root, "backend", "releases", "0.1.35-backend.81");
        var persistentPath = Path.Combine(_root, "backend", "site", "content", "ai-model-settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(persistentPath)!);
        WriteDocument(persistentPath, AiModelSettings.Defaults with { LessonTutorChatModel = "gpt-5.5" }, revision: 7);
        var legacyPath = Path.Combine(releaseRoot, "site", "content", "ai-model-settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(legacyPath)!);
        WriteDocument(legacyPath, AiModelSettings.Defaults with { LessonTutorChatModel = "gpt-5.2" }, revision: 1);

        var active = CreateService(releaseRoot).GetActiveSettings();

        Assert.Equal("gpt-5.5", active.LessonTutorChatModel);
        var persistedDocument = JsonSerializer.Deserialize<AiModelSettingsDocument>(
            File.ReadAllText(persistentPath),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(persistedDocument);
        Assert.Equal("gpt-5.5", persistedDocument.Active.LessonTutorChatModel);
        Assert.Equal("gpt-5.5", persistedDocument.Draft.LessonTutorChatModel);
    }

    [Fact]
    public void MissingPersistentSettingsImportLegacyReleaseSettingsOnce()
    {
        var releaseRoot = Path.Combine(_root, "backend", "releases", "0.1.35-backend.80");
        var legacyPath = Path.Combine(releaseRoot, "site", "content", "ai-model-settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(legacyPath)!);
        WriteDocument(legacyPath, AiModelSettings.Defaults with { LessonTutorChatModel = "gpt-5.5" }, revision: 3);

        var active = CreateService(releaseRoot).GetActiveSettings();

        var persistentPath = Path.Combine(_root, "backend", "site", "content", "ai-model-settings.json");
        Assert.Equal("gpt-5.5", active.LessonTutorChatModel);
        Assert.True(File.Exists(persistentPath));
    }

    private static AiModelSettingsService CreateService(string contentRootPath) =>
        new(Microsoft.Extensions.Options.Options.Create(new AiModelSettingsOptions()), new TestWebHostEnvironment(contentRootPath), NullLogger<AiModelSettingsService>.Instance);

    private static void WriteDocument(string path, AiModelSettings settings, int revision)
    {
        var document = new AiModelSettingsDocument(settings, settings, DateTimeOffset.UtcNow, null, revision);
        File.WriteAllText(path, JsonSerializer.Serialize(document, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
    }

    private static void AssertTemperatureFlags(AiModelSettings settings, bool lessonTutor, bool feedback, bool hint, bool translation)
    {
        Assert.Equal(lessonTutor, settings.LessonTutorChatOmitTemperature);
        Assert.Equal(feedback, settings.FeedbackCorrectionOmitTemperature);
        Assert.Equal(hint, settings.LessonHintOmitTemperature);
        Assert.Equal(translation, settings.TranslationOmitTemperature);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class TestWebHostEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "EnglishVoiceTutor.Api.Tests";
        public string WebRootPath { get; set; } = Path.Combine(contentRootPath, "wwwroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
