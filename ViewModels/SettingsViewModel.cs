using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly Action<string> saveSettings;
    private readonly Action navigateBack;

    public string Title => AppConstants.SettingsTitle;

    public string Subtitle => AppConstants.SettingsSubtitle;

    public string NativeLanguageTitle => AppConstants.NativeLanguageTitle;

    public string NativeLanguageSubtitle => AppConstants.NativeLanguageSubtitle;

    public IReadOnlyList<string> SupportedNativeLanguages { get; } = AppConstants.SupportedNativeLanguages;

    [ObservableProperty]
    private string selectedNativeLanguage;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SettingsViewModel(string currentNativeLanguage, Action<string> saveSettings, Action navigateBack)
    {
        selectedNativeLanguage = currentNativeLanguage;
        this.saveSettings = saveSettings;
        this.navigateBack = navigateBack;
    }

    [RelayCommand]
    private void Save()
    {
        saveSettings(SelectedNativeLanguage);
        StatusMessage = AppConstants.SettingsSavedMessage;
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }
}
