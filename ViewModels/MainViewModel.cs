using CommunityToolkit.Mvvm.ComponentModel;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly UserSettingsService userSettingsService = new();
    private readonly UserSettings userSettings;

    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainViewModel()
    {
        userSettings = userSettingsService.Load();
        currentViewModel = CreateWelcomeViewModel();
    }

    public void NavigateToWelcome()
    {
        CurrentViewModel = CreateWelcomeViewModel();
    }

    public void NavigateToLevelSelection()
    {
        CurrentViewModel = CreateLevelSelectionViewModel();
    }

    public void NavigateToHome(string selectedLevel)
    {
        CurrentViewModel = CreateHomeViewModel(selectedLevel);
    }

    public void NavigateToSubtopics(string selectedLevel, Topic selectedTopic)
    {
        CurrentViewModel = CreateSubtopicsViewModel(selectedLevel, selectedTopic);
    }

    public void NavigateToLessonChat(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic)
    {
        CurrentViewModel = CreateLessonChatViewModel(selectedLevel, selectedTopic, selectedSubtopic);
    }

    public void NavigateToLessonSummary(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic)
    {
        CurrentViewModel = CreateLessonSummaryViewModel(selectedLevel, selectedTopic, selectedSubtopic);
    }

    private void NavigateToSettings(Action navigateBack)
    {
        CurrentViewModel = new SettingsViewModel(
            userSettings.NativeLanguageName,
            SaveNativeLanguage,
            navigateBack);
    }

    private void SaveNativeLanguage(string nativeLanguage)
    {
        userSettings.NativeLanguageName = nativeLanguage;
        userSettingsService.Save(userSettings);
    }

    private WelcomeViewModel CreateWelcomeViewModel()
    {
        return new WelcomeViewModel(NavigateToLevelSelection, () => NavigateToSettings(NavigateToWelcome));
    }

    private LevelSelectionViewModel CreateLevelSelectionViewModel()
    {
        return new LevelSelectionViewModel(NavigateToWelcome, NavigateToHome);
    }

    private HomeViewModel CreateHomeViewModel(string selectedLevel)
    {
        return new HomeViewModel(
            selectedLevel,
            NavigateToLevelSelection,
            topic => NavigateToSubtopics(selectedLevel, topic),
            () => NavigateToSettings(() => NavigateToHome(selectedLevel)));
    }

    private SubtopicsViewModel CreateSubtopicsViewModel(string selectedLevel, Topic selectedTopic)
    {
        return new SubtopicsViewModel(
            selectedLevel,
            selectedTopic,
            () => NavigateToHome(selectedLevel),
            subtopic => NavigateToLessonChat(selectedLevel, selectedTopic, subtopic));
    }

    private LessonChatViewModel CreateLessonChatViewModel(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic)
    {
        return new LessonChatViewModel(
            selectedLevel,
            selectedTopic,
            selectedSubtopic,
            userSettings.NativeLanguageName,
            () => NavigateToSubtopics(selectedLevel, selectedTopic),
            () => NavigateToLessonSummary(selectedLevel, selectedTopic, selectedSubtopic));
    }

    private LessonSummaryViewModel CreateLessonSummaryViewModel(string selectedLevel, Topic selectedTopic, Subtopic selectedSubtopic)
    {
        return new LessonSummaryViewModel(
            selectedLevel,
            selectedTopic,
            selectedSubtopic,
            () => NavigateToSubtopics(selectedLevel, selectedTopic),
            () => NavigateToHome(selectedLevel));
    }
}
