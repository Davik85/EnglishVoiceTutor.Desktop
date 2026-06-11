using System.Diagnostics;
using System.Windows;
using EnglishVoiceTutor.Desktop.Models.Updates;

namespace EnglishVoiceTutor.Desktop.Services.Updates;

public sealed class DesktopStartupUpdateCheckService
{
    private const int StartupUpdateCheckDelaySeconds = 5;
    private const string UpdateAvailableMessage = "A new version of Language Voice Tutor is available. Do you want to download and install it now?";
    private const string UpdateAvailableTitle = "Update available";
    private const string InstallerReadyMessage = "The update was downloaded and verified. Language Voice Tutor will close and restart during installation. Do you want to start the installer now?";
    private const string InstallerReadyTitle = "Start installer?";
    private const string UpdateFailureMessage = "The update could not be downloaded or verified. Please try again later.";
    private const string ActiveLessonInstallerMessage = "Please finish your current lesson before starting the installer.";
    private const string AppUpdatesTitle = "App updates";

    private readonly UpdateManifestClient updateManifestClient;
    private readonly UpdateDownloadService updateDownloadService;
    private bool hasStarted;

    public DesktopStartupUpdateCheckService(
        UpdateManifestClient? updateManifestClient = null,
        UpdateDownloadService? updateDownloadService = null)
    {
        this.updateManifestClient = updateManifestClient ?? new UpdateManifestClient();
        this.updateDownloadService = updateDownloadService ?? new UpdateDownloadService();
    }

    public void StartOnceWhenUiIsReady(Window owner, Func<bool>? isLessonActive = null)
    {
        if (hasStarted)
        {
            return;
        }

        hasStarted = true;
        _ = RunStartupUpdateCheckAsync(owner, isLessonActive);
    }

    private async Task RunStartupUpdateCheckAsync(Window owner, Func<bool>? isLessonActive)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(StartupUpdateCheckDelaySeconds));

            if (!owner.IsLoaded || !owner.IsVisible)
            {
                return;
            }

            var result = await updateManifestClient.LoadLatestAsync();
            if (!result.IsSuccess || result.ValidationResult?.Manifest is null || result.ValidationResult.InstallerUri is null)
            {
                return;
            }

            var manifest = result.ValidationResult.Manifest;
            var installerUri = result.ValidationResult.InstallerUri;
            if (!IsNewerVersionAvailable(manifest))
            {
                return;
            }

            var downloadChoice = MessageBox.Show(
                owner,
                UpdateAvailableMessage,
                UpdateAvailableTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (downloadChoice != MessageBoxResult.Yes)
            {
                return;
            }

            await DownloadVerifyAndMaybeRunUpdateAsync(owner, manifest, installerUri, isLessonActive);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Startup update check skipped without blocking app startup. Error={exception.Message}");
        }
    }

    private static bool IsNewerVersionAvailable(UpdateManifest manifest)
    {
        var currentVersion = DesktopAppVersionProvider.GetCurrentVersionText();
        return UpdateVersionComparer.Compare(currentVersion, manifest.Version) < 0;
    }

    private async Task DownloadVerifyAndMaybeRunUpdateAsync(Window owner, UpdateManifest manifest, Uri installerUri, Func<bool>? isLessonActive)
    {
        try
        {
            var result = await updateDownloadService.DownloadAndVerifyAsync(manifest, installerUri);
            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.FilePath))
            {
                ShowUpdateMessage(owner, UpdateFailureMessage, MessageBoxImage.Warning);
                return;
            }

            if (isLessonActive?.Invoke() == true)
            {
                ShowUpdateMessage(owner, ActiveLessonInstallerMessage, MessageBoxImage.Information);
                return;
            }

            var installChoice = MessageBox.Show(
                owner,
                InstallerReadyMessage,
                InstallerReadyTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (installChoice == MessageBoxResult.Yes)
            {
                UpdateDownloadService.TryStartVerifiedInstallerAfterAppShutdown(result.FilePath, message => ShowUpdateMessage(owner, message, MessageBoxImage.Warning));
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Startup update download or installer prompt failed safely. Error={exception.Message}");
            ShowUpdateMessage(owner, UpdateFailureMessage, MessageBoxImage.Warning);
        }
    }

    private static void ShowUpdateMessage(Window owner, string message, MessageBoxImage icon)
    {
        MessageBox.Show(owner, message, AppUpdatesTitle, MessageBoxButton.OK, icon);
    }
}
