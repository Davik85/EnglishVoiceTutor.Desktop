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

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string authSessionFilePath;

    public AuthSessionStorageService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appSettingsDirectory = Path.Combine(appDataPath, StorageConstants.AppDataFolderName);
        authSessionFilePath = Path.Combine(appSettingsDirectory, StorageConstants.AuthSessionFileName);
    }

    public string AuthSessionFilePath => authSessionFilePath;

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
        try
        {
            if (!File.Exists(authSessionFilePath))
            {
                return null;
            }

            var protectedPayload = await File.ReadAllTextAsync(authSessionFilePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(protectedPayload))
            {
                await ClearAsync(cancellationToken);
                return null;
            }

            var (json, migratedFromPlainText) = TryReadProtectedPayload(protectedPayload);
            var session = JsonSerializer.Deserialize<StoredAuthSession>(json, SerializerOptions);
            if (session is null || string.IsNullOrWhiteSpace(session.AccessToken) || session.User is null || string.IsNullOrWhiteSpace(session.User.Email))
            {
                await ClearAsync(cancellationToken);
                return null;
            }

            if (migratedFromPlainText)
            {
                await SaveAsync(session, cancellationToken);
            }

            return session;
        }
        catch
        {
            await ClearAsync(cancellationToken);
            return null;
        }
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
        return Task.FromResult(File.Exists(authSessionFilePath));
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(authSessionFilePath))
        {
            File.Delete(authSessionFilePath);
        }

        return Task.CompletedTask;
    }

    public static bool IsExpired(StoredAuthSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.ExpiresAtUtc <= DateTimeOffset.UtcNow;
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
            return (Unprotect(protectedPayload), false);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            if (protectedPayload.TrimStart().StartsWith('{'))
            {
                return (protectedPayload, true);
            }

            throw;
        }
    }

    private static string Unprotect(string protectedPayload)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Protected auth session storage is only supported on Windows.");
        }

        var protectedBytes = Convert.FromBase64String(protectedPayload.Trim());
        var entropy = Encoding.UTF8.GetBytes(ProtectedPayloadPurpose);
        var bytes = ProtectedData.Unprotect(protectedBytes, entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
