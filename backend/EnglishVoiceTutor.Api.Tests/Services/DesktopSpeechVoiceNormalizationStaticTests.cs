namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class DesktopSpeechVoiceNormalizationStaticTests
{
    [Fact]
    public void LocalSettingsNormalizeVoiceWithCanonicalTutorBeforePersistence()
    {
        var source = ReadSource("Services/UserSettingsService.cs");
        Assert.Contains("settings.SpeechVoiceId = SpeechVoiceOptions.ResolveSupportedVoiceId(settings.SpeechVoiceId, settings.SelectedTutorAvatarId);", source);
        Assert.True(source.IndexOf("settings.SelectedTutorAvatarId = TutorAvatarOptions.GetById", StringComparison.Ordinal)
            < source.IndexOf("settings.SpeechVoiceId = SpeechVoiceOptions.ResolveSupportedVoiceId", StringComparison.Ordinal));
        Assert.Contains("Normalize(settings);", source[..source.IndexOf("public void Save", StringComparison.Ordinal)]);
        Assert.Contains("Normalize(settings);", source[source.IndexOf("public void Save", StringComparison.Ordinal)..]);
    }

    [Fact]
    public void SettingsSelectorUsesSharedCatalogAndNormalizesBothLocalAndBackendValuesWithTutor()
    {
        var source = ReadSource("ViewModels/SettingsViewModel.cs");
        Assert.Contains("AvailableSpeechVoices { get; } = SpeechVoiceOptions.All;", source);
        Assert.Contains("SpeechVoiceOptions.GetById(currentSpeechVoiceId, selectedTutorAvatarOption.Id)", source);
        Assert.Contains("SpeechVoiceOptions.ResolveSupportedVoiceId(settings.SpeechVoice, SelectedTutorAvatarOption?.Id)", source);
        Assert.Contains("SpeechVoiceOptions.ResolveSupportedVoiceId(backendSettingsSpeechVoice, SelectedTutorAvatarOption?.Id)", source);
        Assert.Contains("ItemsSource=\"{Binding AvailableSpeechVoices}\"", ReadSource("Views/SettingsView.xaml"));
    }

    [Fact]
    public void MainSettingsSaveAndLessonHandoffPreserveTutorContext()
    {
        var source = ReadSource("ViewModels/MainViewModel.cs");
        Assert.Contains("SpeechVoiceOptions.ResolveSupportedVoiceId(speechVoiceId, userSettings.SelectedTutorAvatarId)", source);
        Assert.Contains("SpeechVoiceOptions.ResolveSupportedVoiceId(userSettings.SpeechVoiceId, userSettings.SelectedTutorAvatarId)", source);
    }

    [Fact]
    public void LessonConstructorAndRuntimeNormalizeUsingTutorContext()
    {
        var source = ReadSource("ViewModels/LessonChatViewModel.cs");
        Assert.Contains("this.speechVoiceId = SpeechVoiceOptions.ResolveSupportedVoiceId(speechVoiceId, tutorAvatar.Id);", source);
        Assert.Contains("CurrentSpeechVoiceId => SpeechVoiceOptions.ResolveSupportedVoiceId(speechVoiceId, tutorAvatarId);", source);
    }

    private static string ReadSource(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EnglishVoiceTutor.Desktop.csproj")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
