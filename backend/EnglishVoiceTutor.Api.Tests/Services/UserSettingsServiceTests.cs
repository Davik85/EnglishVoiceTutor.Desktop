using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.UserSettings;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class UserSettingsServiceTests
{
    [Fact]
    public async Task GetOrCreateReturnsDefaultCurrentLevel()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();

        var settings = await service.GetOrCreateAsync(userId, TestContext.Current.CancellationToken);
        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == userId, TestContext.Current.CancellationToken);

        Assert.Equal("A1", settings.CurrentLevel);
        Assert.Equal("A1", profile.CurrentLevel);
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("A2")]
    [InlineData("B1")]
    [InlineData("B2")]
    public async Task GetOrCreateReturnsExistingSavedCurrentLevel(string currentLevel)
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();
        await service.GetOrCreateAsync(userId, TestContext.Current.CancellationToken);
        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == userId, TestContext.Current.CancellationToken);
        profile.CurrentLevel = currentLevel;
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var settings = await service.GetOrCreateAsync(userId, TestContext.Current.CancellationToken);

        Assert.Equal(currentLevel, settings.CurrentLevel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    [InlineData(" UnKnOwN ")]
    public async Task GetOrCreateReplacesNonMeaningfulSavedCurrentLevelWithDefault(string? savedCurrentLevel)
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();
        await service.GetOrCreateAsync(userId, TestContext.Current.CancellationToken);
        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == userId, TestContext.Current.CancellationToken);
        profile.CurrentLevel = savedCurrentLevel!;
        if (savedCurrentLevel is not null)
        {
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var settings = await service.GetOrCreateAsync(userId, TestContext.Current.CancellationToken);

        Assert.Equal("A1", settings.CurrentLevel);
        Assert.Equal("A1", profile.CurrentLevel);
    }

    [Fact]
    public async Task GetOrCreateReturnsDefaultWithoutOverwritingArbitraryUnsupportedSavedCurrentLevel()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();
        await service.GetOrCreateAsync(userId, TestContext.Current.CancellationToken);
        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == userId, TestContext.Current.CancellationToken);
        var originalUpdatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        profile.CurrentLevel = "C1";
        profile.UpdatedAt = originalUpdatedAt;
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var settings = await service.GetOrCreateAsync(userId, TestContext.Current.CancellationToken);

        Assert.Equal("A1", settings.CurrentLevel);
        Assert.Equal("C1", profile.CurrentLevel);
        Assert.Equal(originalUpdatedAt, profile.UpdatedAt);
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("A2")]
    [InlineData("B1")]
    [InlineData("B2")]
    public async Task UpdatePersistsAndReturnsEachSupportedCurrentLevel(string currentLevel)
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();

        var settings = await service.UpdateAsync(userId, CreateValidRequest(selectedTutorId: null, currentLevel), TestContext.Current.CancellationToken);
        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == userId, TestContext.Current.CancellationToken);

        Assert.Equal(currentLevel, settings.CurrentLevel);
        Assert.Equal(currentLevel, profile.CurrentLevel);
    }

    [Theory]
    [InlineData(" a2 ", "A2")]
    [InlineData("b1", "B1")]
    public async Task UpdateNormalizesCurrentLevelCaseAndWhitespace(string suppliedLevel, string expectedLevel)
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();

        var settings = await service.UpdateAsync(userId, CreateValidRequest(selectedTutorId: null, suppliedLevel), TestContext.Current.CancellationToken);
        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == userId, TestContext.Current.CancellationToken);

        Assert.Equal(expectedLevel, settings.CurrentLevel);
        Assert.Equal(expectedLevel, profile.CurrentLevel);
    }

    [Theory]
    [InlineData("C1")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateWithUnsupportedOrBlankCurrentLevelReturnsValidationError(string currentLevel)
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<UserSettingsValidationException>(() =>
            service.UpdateAsync(Guid.NewGuid(), CreateValidRequest(selectedTutorId: null, currentLevel), TestContext.Current.CancellationToken));

        Assert.Equal("Current level must be one of: A1, A2, B1, B2.", exception.Message);
    }

    [Fact]
    public async Task UpdateWithoutCurrentLevelPreservesItWhileOtherSettingsChange()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();
        await service.UpdateAsync(userId, CreateValidRequest(selectedTutorId: "david", currentLevel: "B2"), TestContext.Current.CancellationToken);

        var settings = await service.UpdateAsync(userId, new UpdateUserSettingsRequest
        {
            NativeLanguage = "es",
            StudyLanguage = StudyLanguageConstants.Spanish,
            ExplanationLanguage = "fr",
            SelectedTutorId = "nelli",
            SpeechVoice = "verse",
            SpeechSpeed = 1.25m,
            ConversationModeEnabled = false
        }, TestContext.Current.CancellationToken);
        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == userId, TestContext.Current.CancellationToken);

        Assert.Equal("B2", settings.CurrentLevel);
        Assert.Equal("B2", profile.CurrentLevel);
        Assert.Equal("es", settings.NativeLanguage);
        Assert.Equal(StudyLanguageConstants.Spanish, settings.StudyLanguage);
        Assert.Equal("fr", settings.ExplanationLanguage);
        Assert.Equal("nelli", settings.SelectedTutorId);
        Assert.Equal("verse", settings.SpeechVoice);
        Assert.Equal(1.25m, settings.SpeechSpeed);
        Assert.False(settings.ConversationModeEnabled);
    }

    [Fact]
    public async Task UpdatingCurrentLevelDoesNotAlterOtherSettings()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();
        var originalRequest = new UpdateUserSettingsRequest
        {
            NativeLanguage = "es",
            StudyLanguage = StudyLanguageConstants.Spanish,
            ExplanationLanguage = "fr",
            CurrentLevel = "A2",
            SelectedTutorId = "nelli",
            SpeechVoice = "verse",
            SpeechSpeed = 1.25m,
            ConversationModeEnabled = false
        };
        await service.UpdateAsync(userId, originalRequest, TestContext.Current.CancellationToken);

        originalRequest.CurrentLevel = "B1";
        var settings = await service.UpdateAsync(userId, originalRequest, TestContext.Current.CancellationToken);

        Assert.Equal("B1", settings.CurrentLevel);
        Assert.Equal("es", settings.NativeLanguage);
        Assert.Equal(StudyLanguageConstants.Spanish, settings.StudyLanguage);
        Assert.Equal("fr", settings.ExplanationLanguage);
        Assert.Equal("nelli", settings.SelectedTutorId);
        Assert.Equal("verse", settings.SpeechVoice);
        Assert.Equal(1.25m, settings.SpeechSpeed);
        Assert.False(settings.ConversationModeEnabled);
    }

    [Fact]
    public async Task GetOrCreateIncludesCanonicalSelectedTutorId()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var settings = await service.GetOrCreateAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal("lana", settings.SelectedTutorId);
    }

    [Fact]
    public async Task UpdateWithValidSelectedTutorIdPersistsAndReturnsCanonicalSelectedTutorId()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();

        var settings = await service.UpdateAsync(userId, CreateValidRequest(selectedTutorId: " NELLI "), TestContext.Current.CancellationToken);
        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == userId, TestContext.Current.CancellationToken);

        Assert.Equal("nelli", settings.SelectedTutorId);
        Assert.Equal("nelli", profile.SelectedTutorId);
        Assert.Equal("alloy", settings.SpeechVoice);
        Assert.Equal("alloy", (await dbContext.UserSettings.SingleAsync(userSettings => userSettings.UserId == userId, TestContext.Current.CancellationToken)).SpeechVoice);
    }

    [Fact]
    public async Task UpdateWithoutSelectedTutorIdPreservesExistingSelectedTutorId()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();
        await service.UpdateAsync(userId, CreateValidRequest(selectedTutorId: "david"), TestContext.Current.CancellationToken);

        var settings = await service.UpdateAsync(userId, CreateValidRequest(selectedTutorId: null, speechVoice: "verse"), TestContext.Current.CancellationToken);
        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == userId, TestContext.Current.CancellationToken);

        Assert.Equal("david", settings.SelectedTutorId);
        Assert.Equal("david", profile.SelectedTutorId);
        Assert.Equal("verse", settings.SpeechVoice);
    }

    [Fact]
    public async Task UpdateWithInvalidSelectedTutorIdReturnsValidationError()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<UserSettingsValidationException>(() =>
            service.UpdateAsync(Guid.NewGuid(), CreateValidRequest(selectedTutorId: "unsupported"), TestContext.Current.CancellationToken));

        Assert.Equal("Selected tutor must be one of: lana, nelli, david.", exception.Message);
    }

    [Fact]
    public async Task ExistingSettingsFieldsStillUpdate()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();

        var settings = await service.UpdateAsync(userId, new UpdateUserSettingsRequest
        {
            NativeLanguage = "es",
            StudyLanguage = "Spanish",
            ExplanationLanguage = "fr",
            SpeechVoice = "nova",
            SpeechSpeed = 1.25m,
            ConversationModeEnabled = false
        }, TestContext.Current.CancellationToken);

        Assert.Equal("es", settings.NativeLanguage);
        Assert.Equal(StudyLanguageConstants.Spanish, settings.StudyLanguage);
        Assert.Equal("fr", settings.ExplanationLanguage);
        Assert.Equal("nova", settings.SpeechVoice);
        Assert.Equal(1.25m, settings.SpeechSpeed);
        Assert.False(settings.ConversationModeEnabled);
        Assert.Equal("lana", settings.SelectedTutorId);
    }

    private static UserSettingsService CreateService(AppDbContext dbContext) => new(dbContext, new DevUserProvider());

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static UpdateUserSettingsRequest CreateValidRequest(
        string? selectedTutorId,
        string? currentLevel = null,
        string speechVoice = "alloy") => new()
    {
        StudyLanguage = StudyLanguageConstants.English,
        ExplanationLanguage = "en",
        CurrentLevel = currentLevel,
        SelectedTutorId = selectedTutorId,
        SpeechVoice = speechVoice,
        SpeechSpeed = 1.0m,
        ConversationModeEnabled = true
    };
}
