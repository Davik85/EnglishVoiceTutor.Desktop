using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace EnglishVoiceTutor.Desktop.Services.Updates;

public static class DesktopAppVersionProvider
{
    public const string BundledVersionFileName = "release-version.txt";
    public const string AppVersionFallbackText = "0.0.0-local";
    private const int AppModelErrorNoPackage = 15700;

    public static string GetCurrentVersionText()
    {
        if (DesktopDistributionChannelProvider.IsStore)
        {
            return GetStoreVersionText();
        }

        return GetDirectVersionText();
    }

    public static string GetInstalledVersionDisplayText()
    {
        var version = GetCurrentVersionText();
        return DesktopDistributionChannelProvider.IsStore
            ? $"Version: {version}"
            : $"Version: v{version}";
    }

    private static string GetDirectVersionText()
    {
        var bundledVersion = TryReadBundledVersionFile();
        if (!string.IsNullOrWhiteSpace(bundledVersion))
        {
            return bundledVersion;
        }

        return GetAssemblyVersionText();
    }

    private static string GetStoreVersionText()
    {
        var packageVersion = TryReadMsixPackageIdentityVersion();
        if (!string.IsNullOrWhiteSpace(packageVersion) && !string.Equals(packageVersion, AppVersionFallbackText, StringComparison.OrdinalIgnoreCase))
        {
            return packageVersion;
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

    private static string? TryReadMsixPackageIdentityVersion()
    {
        try
        {
            return TryReadCurrentPackageFullNameVersion();
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
    }

    private static string? TryReadCurrentPackageFullNameVersion()
    {
        var length = 0;
        var result = GetCurrentPackageFullName(ref length, null);
        if (result == AppModelErrorNoPackage)
        {
            return null;
        }

        if (length <= 0)
        {
            return null;
        }

        var builder = new StringBuilder(length);
        result = GetCurrentPackageFullName(ref length, builder);
        if (result == AppModelErrorNoPackage)
        {
            return null;
        }

        if (result != 0)
        {
            throw new Win32Exception(result);
        }

        var fullName = builder.ToString();
        var parts = fullName.Split('_');
        return parts.Length >= 2 && IsFourPartNumericVersion(parts[1]) ? parts[1] : null;
    }

    private static bool IsFourPartNumericVersion(string value)
    {
        var parts = value.Split('.');
        return parts.Length == 4 && parts.All(part => ushort.TryParse(part, out _));
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

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
}
