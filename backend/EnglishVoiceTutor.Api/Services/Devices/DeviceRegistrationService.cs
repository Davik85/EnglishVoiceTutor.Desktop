using EnglishVoiceTutor.Api.Contracts.Devices;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Devices;

public sealed class DeviceRegistrationService(AppDbContext dbContext) : IDeviceRegistrationService
{
    private const int PlatformMaxLength = 64;
    private const int DeviceNameMaxLength = 128;
    private const int AppVersionMaxLength = 64;
    private const string UnknownDeviceName = "Unknown";

    public async Task<DeviceRegistrationResponse> RegisterAsync(Guid userId, DeviceRegistrationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTimeOffset.UtcNow;
        var platform = Normalize(request.Platform, PlatformMaxLength, OperatingSystem.IsWindows() ? "Windows" : "Desktop");
        var deviceName = Normalize(request.DeviceName, DeviceNameMaxLength, UnknownDeviceName);
        var appVersion = Normalize(request.AppVersion, AppVersionMaxLength, "Unknown");

        var device = await dbContext.Devices
            .FirstOrDefaultAsync(item => item.UserId == userId
                && item.Platform == platform
                && item.DeviceName == deviceName
                && item.AppVersion == appVersion, cancellationToken);

        if (device is null)
        {
            device = new DeviceEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Platform = platform,
                DeviceName = deviceName,
                AppVersion = appVersion,
                CreatedAt = now,
                LastSeenAt = now
            };
            dbContext.Devices.Add(device);
        }
        else
        {
            device.LastSeenAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeviceRegistrationResponse
        {
            Registered = true,
            LastSeenAt = device.LastSeenAt
        };
    }

    private static string Normalize(string? value, int maxLength, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        normalized = normalized.ReplaceLineEndings(" ");
        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
