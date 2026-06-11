using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models.Auth;
using EnglishVoiceTutor.Desktop.Services.Auth;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class WelcomeViewModel : ViewModelBase, IDisposable
{
    private readonly Action navigateToLevelSelection;
    private readonly Action navigateToSettings;
    private readonly Action navigateToAccountSettings;
    private readonly AuthBackendService authBackendService;
    private readonly AppLocalizedText localizedText;
    private bool isDisposed;

    public string AppTitle => localizedText.WelcomeTitle;

    public string Subtitle => localizedText.WelcomeSubtitle;

    public string StartLessonButtonText => localizedText.WelcomeStartLessonButton;

    public string SettingsButtonText => localizedText.WelcomeSettingsButton;

    [ObservableProperty]
    private string accountStatusButtonText = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public WelcomeViewModel(
        AppLocalizedText localizedText,
        AuthBackendService authBackendService,
        Action navigateToLevelSelection,
        Action navigateToSettings,
        Action navigateToAccountSettings)
    {
        this.localizedText = localizedText;
        this.authBackendService = authBackendService;
        this.navigateToLevelSelection = navigateToLevelSelection;
        this.navigateToSettings = navigateToSettings;
        this.navigateToAccountSettings = navigateToAccountSettings;
        AccountStatusButtonText = localizedText.WelcomeSignInToAccountButton;
        this.authBackendService.AuthStateChanged += OnAuthStateChanged;
        _ = RefreshAccountStatusAsync();
    }

    [RelayCommand]
    private void StartLesson()
    {
        navigateToLevelSelection();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        navigateToSettings();
    }

    [RelayCommand]
    private void OpenAccountSettings()
    {
        navigateToAccountSettings();
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        authBackendService.AuthStateChanged -= OnAuthStateChanged;
        isDisposed = true;
    }

    private void OnAuthStateChanged(object? sender, AuthStateChangedEventArgs e)
    {
        RunOnUiThread(() => AccountStatusButtonText = BuildAccountStatusButtonText(e.User));
    }

    private async Task RefreshAccountStatusAsync()
    {
        var session = await authBackendService.TryRestoreSessionAsync();
        RunOnUiThread(() => AccountStatusButtonText = BuildAccountStatusButtonText(session?.User));
    }

    private string BuildAccountStatusButtonText(AuthUserDto? user)
    {
        if (user is null)
        {
            return localizedText.WelcomeSignInToAccountButton;
        }

        var displayName = GetBestAccountDisplayName(user);
        return string.IsNullOrWhiteSpace(displayName)
            ? localizedText.WelcomeSignedInButton
            : string.Format(localizedText.WelcomeSignedInAsFormat, displayName);
    }

    private static string GetBestAccountDisplayName(AuthUserDto user)
    {
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName.Trim();
        }

        return string.IsNullOrWhiteSpace(user.Email) ? string.Empty : user.Email.Trim();
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}
