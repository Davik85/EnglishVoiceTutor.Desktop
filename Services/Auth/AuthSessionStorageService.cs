using System.IO;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models.Auth;

namespace EnglishVoiceTutor.Desktop.Services.Auth;

public sealed class AuthSessionStorageService
{
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

        // MVP local session storage for development. Replace with secure OS-backed storage before production use.
        var json = JsonSerializer.Serialize(session, SerializerOptions);
        await File.WriteAllTextAsync(authSessionFilePath, json, cancellationToken);
    }

    public async Task<StoredAuthSession?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(authSessionFilePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(authSessionFilePath, cancellationToken);
            var session = JsonSerializer.Deserialize<StoredAuthSession>(json, SerializerOptions);
            if (session is null)
            {
                await ClearAsync(cancellationToken);
                return null;
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
}
