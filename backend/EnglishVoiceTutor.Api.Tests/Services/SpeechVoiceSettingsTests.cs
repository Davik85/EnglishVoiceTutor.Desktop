using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.UserSettings;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class SpeechVoiceSettingsTests
{
    public static IEnumerable<object[]> SupportedRequests => SpeechVoiceOptionsTests.ExpectedIds
        .SelectMany(voice => new[] { new object[] { voice, voice }, new object[] { $" {voice.ToUpperInvariant()} ", voice } });

    public static IEnumerable<object[]> PersistedVoices =>
        from voice in new[] { "nova", "onyx", "fable", "unknown", "", " ", " ONYX " }
        from tutor in new[] { "david", "lana", "nelli", "elena", "unknown" }
        select new object[] { voice, tutor, tutor == "david" ? "cedar" : "coral" };

    [Theory]
    [MemberData(nameof(SupportedRequests))]
    public async Task UpdateAcceptsEverySupportedVoiceAndStoresCanonicalLowercase(string input, string expected)
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var id = Guid.NewGuid();
        var response = await service.UpdateAsync(id, Request(input), TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();
        Assert.Equal(expected, response.SpeechVoice);
        Assert.Equal(expected, (await db.UserSettings.SingleAsync(TestContext.Current.CancellationToken)).SpeechVoice);
    }

    [Theory]
    [InlineData("nova")]
    [InlineData("onyx")]
    [InlineData("fable")]
    [InlineData(" NOVA ")]
    [InlineData(" ONYX ")]
    [InlineData(" FABLE ")]
    [InlineData("random-voice")]
    public async Task UpdateRejectsUnsupportedVoiceWithoutChangingExistingSettings(string input)
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var id = Guid.NewGuid();
        var before = await service.GetOrCreateAsync(id, TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<UserSettingsValidationException>(() =>
            service.UpdateAsync(id, Request(input), TestContext.Current.CancellationToken));
        Assert.Equal("Speech voice must be one of: alloy, ash, ballad, coral, echo, sage, shimmer, verse, marin, cedar.", exception.Message);
        db.ChangeTracker.Clear();
        var stored = await db.UserSettings.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(before.SpeechVoice, stored.SpeechVoice);
        Assert.Equal(before.UpdatedAt, stored.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task MissingVoiceKeepsExistingRequiredValidationErrorAndDoesNotCreateUser(string? input)
    {
        await using var db = CreateDbContext();
        var exception = await Assert.ThrowsAsync<UserSettingsValidationException>(() =>
            CreateService(db).UpdateAsync(Guid.NewGuid(), Request(input!), TestContext.Current.CancellationToken));
        Assert.Equal("Speech voice is required.", exception.Message);
        Assert.Empty(await db.Users.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [MemberData(nameof(PersistedVoices))]
    [InlineData(" CEDAR ", "lana", "cedar")]
    [InlineData(" ALLOY ", "david", "alloy")]
    public async Task LoadRepairsPersistedVoiceUsingTutorAndPersistsOnlyOneRepair(string input, string tutor, string expected)
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var id = Guid.NewGuid();
        await service.GetOrCreateAsync(id, TestContext.Current.CancellationToken);
        var row = await db.UserSettings.SingleAsync(TestContext.Current.CancellationToken);
        var profile = await db.UserProfiles.SingleAsync(TestContext.Current.CancellationToken);
        var oldTimestamp = DateTimeOffset.UtcNow.AddDays(-1);
        var createdAt = row.CreatedAt;
        row.SpeechVoice = input;
        row.UpdatedAt = oldTimestamp;
        profile.SelectedTutorId = tutor;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();

        var response = await service.GetOrCreateAsync(id, TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();
        var persisted = await db.UserSettings.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expected, response.SpeechVoice);
        Assert.Equal(expected, persisted.SpeechVoice);
        Assert.True(persisted.UpdatedAt > oldTimestamp);
        Assert.Equal(persisted.UpdatedAt, response.UpdatedAt);
        Assert.Equal(createdAt, persisted.CreatedAt);
        Assert.Equal(1.0m, persisted.SpeechSpeed);
        Assert.True(persisted.ConversationModeEnabled);

        var secondResponse = await service.GetOrCreateAsync(id, TestContext.Current.CancellationToken);
        Assert.Equal(expected, secondResponse.SpeechVoice);
        Assert.Equal(response.UpdatedAt, secondResponse.UpdatedAt);
    }

    [Theory]
    [MemberData(nameof(SpeechVoiceOptionsTests.SupportedVoices), MemberType = typeof(SpeechVoiceOptionsTests))]
    public async Task LoadPreservesAlreadyCanonicalVoiceAndTimestamp(string voice)
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var id = Guid.NewGuid();
        var before = await service.UpdateAsync(id, Request(voice), TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();
        var after = await service.GetOrCreateAsync(id, TestContext.Current.CancellationToken);
        Assert.Equal(voice, after.SpeechVoice);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
    }

    [Fact]
    public async Task NewSettingsStillDefaultToCoral()
    {
        await using var db = CreateDbContext();
        var response = await CreateService(db).GetOrCreateAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.Equal("coral", response.SpeechVoice);
    }

    private static UserSettingsService CreateService(AppDbContext db) => new(db, new DevUserProvider());

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static UpdateUserSettingsRequest Request(string voice) => new()
    {
        StudyLanguage = StudyLanguageConstants.English,
        ExplanationLanguage = "en",
        SelectedTutorId = "david",
        SpeechVoice = voice,
        SpeechSpeed = 1.0m,
        ConversationModeEnabled = true
    };
}
