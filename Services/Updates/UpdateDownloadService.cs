using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows;
using EnglishVoiceTutor.Desktop.Models.Updates;

namespace EnglishVoiceTutor.Desktop.Services.Updates;

public sealed class UpdateDownloadService
{
    private readonly HttpClient httpClient;

    public UpdateDownloadService(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient();
    }

    public async Task<UpdateDownloadResult> DownloadAndVerifyAsync(
        UpdateManifest manifest,
        Uri installerUri,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(installerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return UpdateDownloadResult.Failure("The installer download address is not a valid HTTPS URL.");
        }

        var safeFileName = Path.GetFileName(manifest.InstallerFileName);
        if (string.IsNullOrWhiteSpace(safeFileName) || !safeFileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return UpdateDownloadResult.Failure("The installer file name is not valid.");
        }

        var updateDirectory = GetUpdateCacheDirectory();
        Directory.CreateDirectory(updateDirectory);
        var destinationPath = Path.Combine(updateDirectory, safeFileName);
        var temporaryPath = destinationPath + ".download";

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(10));

            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            using var response = await httpClient.GetAsync(installerUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                return UpdateDownloadResult.Failure("Could not download the installer right now. Please try again later.");
            }

            await using (var remoteStream = await response.Content.ReadAsStreamAsync(timeout.Token))
            await using (var localStream = File.Create(temporaryPath))
            {
                await remoteStream.CopyToAsync(localStream, timeout.Token);
            }

            var actualSha256 = await ComputeSha256Async(temporaryPath, timeout.Token);
            if (!string.Equals(actualSha256, manifest.InstallerSha256, StringComparison.OrdinalIgnoreCase))
            {
                DeleteIfExists(temporaryPath);
                DeleteIfExists(destinationPath);
                return UpdateDownloadResult.Failure("The downloaded installer did not pass verification. It was deleted for safety.");
            }

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Move(temporaryPath, destinationPath);
            return UpdateDownloadResult.Success(destinationPath);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            DeleteIfExists(temporaryPath);
            return UpdateDownloadResult.Failure("The download took too long. Please try again later.");
        }
        catch (Exception)
        {
            DeleteIfExists(temporaryPath);
            return UpdateDownloadResult.Failure("Could not download or verify the installer right now. Please try again later.");
        }
    }

    public static bool TryLaunchVerifiedInstallerAfterAppShutdown(string installerPath, Action<string>? showLaunchFailure = null)
    {
        if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
        {
            showLaunchFailure?.Invoke("The verified installer could not be found. Please check for updates again.");
            return false;
        }

        try
        {
            StartDetachedDelayedInstallerLauncher(installerPath);
            BeginApplicationShutdown();
            return true;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Could not start update installer helper. Error={exception.Message}");
            showLaunchFailure?.Invoke("The installer could not be started. Please try again, or restart the app and check for updates again.");
            return false;
        }
    }

    private static void StartDetachedDelayedInstallerLauncher(string installerPath)
    {
        var helperProcess = Process.Start(new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            Arguments = BuildDelayedInstallerLaunchArguments(installerPath),
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        if (helperProcess is null)
        {
            throw new InvalidOperationException("The update installer helper process could not be started.");
        }
    }

    private static string BuildDelayedInstallerLaunchArguments(string installerPath)
    {
        const int installerLaunchDelaySeconds = 4;
        return $"/d /c \"timeout /t {installerLaunchDelaySeconds} /nobreak >nul & start \"\" {QuoteForCmd(installerPath)}\"";
    }

    private static string QuoteForCmd(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static void BeginApplicationShutdown()
    {
        var application = Application.Current;
        if (application is null)
        {
            return;
        }

        application.Dispatcher.BeginInvoke(new Action(application.Shutdown));
    }

    public static void OpenContainingFolder(string installerPath)
    {
        if (string.IsNullOrWhiteSpace(installerPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(installerPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = directory,
            UseShellExecute = true
        });
    }

    private static string GetUpdateCacheDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "LanguageVoiceTutor", "Updates");
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
