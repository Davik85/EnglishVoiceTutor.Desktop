using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models.Auth;

namespace EnglishVoiceTutor.Desktop.Services.Auth;

public sealed class AuthSessionStorageService
{
    private const string ProtectedPayloadPurpose = "EnglishVoiceTutor.Desktop.AuthSession.v1";

    private static readonly string[] LegacyProtectedPayloadPurposes =
    [
        "LanguageVoiceTutor.Desktop.AuthSession.v1",
        "Language Voice Tutor.AuthSession.v1"
    ];

    private static readonly string[] LegacyAppDataFolderNames =
    [
        "LanguageVoiceTutor.Desktop",
        "Language Voice Tutor"
    ];

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string authSessionFilePath;
    private readonly string[] authSessionFilePaths;

    public AuthSessionStorageService()
    {
        var roamingAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appSettingsDirectory = Path.Combine(roamingAppDataPath, StorageConstants.AppDataFolderName);
        authSessionFilePath = Path.Combine(appSettingsDirectory, StorageConstants.AuthSessionFileName);
        authSessionFilePaths = BuildSessionFilePathCandidates(roamingAppDataPath, localAppDataPath);
    }

    public string AuthSessionFilePath => authSessionFilePath;

    public IReadOnlyList<string> AuthSessionFilePathCandidates => authSessionFilePaths;

    public async Task SaveAsync(StoredAuthSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var directoryPath = Path.GetDirectoryName(authSessionFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(session, SerializerOptions);
        var protectedPayload = Protect(json);
        await File.WriteAllTextAsync(authSessionFilePath, protectedPayload, cancellationToken);
    }

    public async Task<StoredAuthSession?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(authSessionFilePath))
        {
            return await TryLoadLegacySessionAsync(cancellationToken);
        }

        var currentSession = await TryLoadSessionFileAsync(authSessionFilePath, isCurrentPath: true, cancellationToken);
        if (currentSession is not null)
        {
            return currentSession;
        }

        return await TryLoadLegacySessionAsync(cancellationToken);
    }

    public async Task<StoredAuthSession?> GetValidSessionOrNullAsync(CancellationToken cancellationToken = default)
    {
        var session = await LoadAsync(cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (IsExpired(session))
        {
            await ClearAsync(cancellationToken);
            return null;
        }

        return session;
    }

    public Task<bool> HasStoredSessionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(authSessionFilePaths.Any(File.Exists));
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        foreach (var sessionFilePath in authSessionFilePaths)
        {
            TryDeleteSessionFile(sessionFilePath);
        }

        return Task.CompletedTask;
    }

    public static bool IsExpired(StoredAuthSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.ExpiresAtUtc <= DateTimeOffset.UtcNow;
    }

    private async Task<StoredAuthSession?> TryLoadLegacySessionAsync(CancellationToken cancellationToken)
    {
        foreach (var legacySessionFilePath in authSessionFilePaths.Where(path => !StringComparer.OrdinalIgnoreCase.Equals(path, authSessionFilePath)))
        {
            var migratedSession = await TryLoadSessionFileAsync(legacySessionFilePath, isCurrentPath: false, cancellationToken);
            if (migratedSession is not null)
            {
                await SaveAsync(migratedSession, cancellationToken);
                return migratedSession;
            }
        }

        return null;
    }

    private async Task<StoredAuthSession?> TryLoadSessionFileAsync(string sessionFilePath, bool isCurrentPath, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(sessionFilePath))
            {
                return null;
            }

            var protectedPayload = await File.ReadAllTextAsync(sessionFilePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(protectedPayload))
            {
                TryDeleteSessionFile(sessionFilePath);
                return null;
            }

            var (json, requiresRewrite) = TryReadProtectedPayload(protectedPayload);
            var session = JsonSerializer.Deserialize<StoredAuthSession>(json, SerializerOptions);
            if (session is null || string.IsNullOrWhiteSpace(session.AccessToken) || session.User is null || string.IsNullOrWhiteSpace(session.User.Email))
            {
                TryDeleteSessionFile(sessionFilePath);
                return null;
            }

            if (requiresRewrite || !isCurrentPath)
            {
                await SaveAsync(session, cancellationToken);
            }

            return session;
        }
        catch
        {
            TryDeleteSessionFile(sessionFilePath);
            return null;
        }
    }

    private static string Protect(string json)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Protected auth session storage is only supported on Windows.");
        }

        var bytes = Encoding.UTF8.GetBytes(json);
        var entropy = Encoding.UTF8.GetBytes(ProtectedPayloadPurpose);
        var protectedBytes = ProtectedData.Protect(bytes, entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static (string Json, bool MigratedFromPlainText) TryReadProtectedPayload(string protectedPayload)
    {
        try
        {
            return (Unprotect(protectedPayload, ProtectedPayloadPurpose), false);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            foreach (var legacyPurpose in LegacyProtectedPayloadPurposes)
            {
                try
                {
                    return (Unprotect(protectedPayload, legacyPurpose), true);
                }
                catch (Exception legacyException) when (legacyException is CryptographicException or FormatException)
                {
                    // Try the next known legacy DPAPI purpose before treating the payload as corrupt.
                }
            }

            if (protectedPayload.TrimStart().StartsWith('{'))
            {
                return (protectedPayload, true);
            }

            throw;
        }
    }

    private static string Unprotect(string protectedPayload, string purpose)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Protected auth session storage is only supported on Windows.");
        }

        var protectedBytes = Convert.FromBase64String(protectedPayload.Trim());
        var entropy = Encoding.UTF8.GetBytes(purpose);
        var bytes = ProtectedData.Unprotect(protectedBytes, entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string[] BuildSessionFilePathCandidates(string roamingAppDataPath, string localAppDataPath)
    {
        var candidates = new List<string>
        {
            Path.Combine(roamingAppDataPath, StorageConstants.AppDataFolderName, StorageConstants.AuthSessionFileName)
        };

        foreach (var appDataRoot in new[] { roamingAppDataPath, localAppDataPath })
        {
            foreach (var legacyFolderName in LegacyAppDataFolderNames)
            {
                candidates.Add(Path.Combine(appDataRoot, legacyFolderName, StorageConstants.AuthSessionFileName));
            }
        }

        candidates.Add(Path.Combine(localAppDataPath, StorageConstants.AppDataFolderName, StorageConstants.AuthSessionFileName));

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void TryDeleteSessionFile(string sessionFilePath)
    {
        try
        {
            if (File.Exists(sessionFilePath))
            {
                File.Delete(sessionFilePath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Startup/session restore must never crash because a stale or corrupt session file could not be removed.
        }
    }
}
