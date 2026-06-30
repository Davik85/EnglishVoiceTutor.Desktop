using System.IO;
using System.Reflection;

namespace EnglishVoiceTutor.Desktop.Services.Updates;

public static class DesktopAppVersionProvider
{
    public const string BundledVersionFileName = "release-version.txt";
    public const string AppVersionFallbackText = "0.0.0-local";
    public static string GetCurrentVersionText() => GetDirectVersionText();

    public static string GetInstalledVersionDisplayText() => $"Version: v{GetCurrentVersionText()}";

    private static string GetDirectVersionText()
    {
        var bundledVersion = TryReadBundledVersionFile();
        if (!string.IsNullOrWhiteSpace(bundledVersion))
        {
            return bundledVersion;
        }

        return GetAssemblyVersionText();
    }

    private static string GetAssemblyVersionText()
    {
        var informationalVersion = ReadInformationalVersion(Assembly.GetEntryAssembly())
            ?? ReadInformationalVersion(Assembly.GetExecutingAssembly());
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        var assemblyVersion = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version?.ToString(fieldCount: 3);
        return string.IsNullOrWhiteSpace(assemblyVersion) ? AppVersionFallbackText : assemblyVersion;
    }

    private static string? TryReadBundledVersionFile()
    {
        try
        {
            var versionFilePath = Path.Combine(AppContext.BaseDirectory, BundledVersionFileName);
            if (!File.Exists(versionFilePath))
            {
                return null;
            }

            var version = File.ReadAllText(versionFilePath).Trim();
            return string.IsNullOrWhiteSpace(version) ? null : version;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? ReadInformationalVersion(Assembly? assembly)
    {
        var version = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Trim();
        return string.IsNullOrWhiteSpace(version) ? null : version;
    }
}
