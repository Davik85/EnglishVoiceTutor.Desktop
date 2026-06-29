namespace EnglishVoiceTutor.Desktop.Services.Updates;

public static class DesktopUpdatePolicy
{
    public const string StoreManagedUpdatesMessage = "Updates are managed by Microsoft Store.";

    public static bool CanUseDirectUpdateManifest => DesktopDistributionChannelProvider.IsDirect;

    public static bool CanDownloadDirectInstaller => DesktopDistributionChannelProvider.IsDirect;

    public static bool CanLaunchDirectInstaller => DesktopDistributionChannelProvider.IsDirect;

    public static bool ShouldRunStartupDirectUpdateCheck => DesktopDistributionChannelProvider.IsDirect;
}
