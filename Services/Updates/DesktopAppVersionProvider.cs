using System.Reflection;

namespace EnglishVoiceTutor.Desktop.Services.Updates;

public static class DesktopAppVersionProvider
{
    private const string AppVersionFallbackText = "0.0.0-local";

    public static string GetCurrentVersionText()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion.Trim();
        }

        var assemblyVersion = assembly.GetName().Version?.ToString(fieldCount: 3);
        return string.IsNullOrWhiteSpace(assemblyVersion) ? AppVersionFallbackText : assemblyVersion;
    }
}
