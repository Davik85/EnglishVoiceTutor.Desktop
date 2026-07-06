using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.UserSettings;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class UserSettingsServiceTests
{
    [Fact]
    public async Task GetOrCreateIncludesCanonicalSelectedTutorId()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var settings = await service.GetOrCreateAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal("lana", settings.SelectedTutorId);
    }

    [Fact]
    public async Task UpdateWithValidSelectedTutorIdPersistsAndReturnsCanonicalSelectedTutorId()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();

        var settings = await service.UpdateAsync(userId, CreateValidRequest(selectedTutorId: " NELLI "), CancellationToken.None);
        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == userId);

        Assert.Equal("nelli", settings.SelectedTutorId);
        Assert.Equal("nelli", profile.SelectedTutorId);
        Assert.Equal("alloy", settings.SpeechVoice);
        Assert.Equal("alloy", (await dbContext.UserSettings.SingleAsync(userSettings => userSettings.UserId == userId)).SpeechVoice);
    }

    [Fact]
    public async Task UpdateWithoutSelectedTutorIdPreservesExistingSelectedTutorId()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();
        await service.UpdateAsync(userId, CreateValidRequest(selectedTutorId: "david"), CancellationToken.None);

        var settings = await service.UpdateAsync(userId, CreateValidRequest(selectedTutorId: null, speechVoice: "verse"), CancellationToken.None);
        var profile = await dbContext.UserProfiles.SingleAsync(profile => profile.UserId == userId);

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
            service.UpdateAsync(Guid.NewGuid(), CreateValidRequest(selectedTutorId: "unsupported"), CancellationToken.None));

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
        }, CancellationToken.None);

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

    private static UpdateUserSettingsRequest CreateValidRequest(string? selectedTutorId, string speechVoice = "alloy") => new()
    {
        StudyLanguage = StudyLanguageConstants.English,
        ExplanationLanguage = "en",
        SelectedTutorId = selectedTutorId,
        SpeechVoice = speechVoice,
        SpeechSpeed = 1.0m,
        ConversationModeEnabled = true
    };
}
