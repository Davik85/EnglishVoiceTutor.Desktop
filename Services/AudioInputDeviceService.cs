using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using NAudio.Wave;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class AudioInputDeviceService
{
    public IReadOnlyList<AudioInputDeviceOption> GetAudioInputDevices(string systemDefaultDisplayName)
    {
        var devices = new List<AudioInputDeviceOption>
        {
            CreateSystemDefaultOption(systemDefaultDisplayName)
        };

        try
        {
            for (var deviceNumber = 0; deviceNumber < WaveIn.DeviceCount; deviceNumber++)
            {
                var capabilities = WaveIn.GetCapabilities(deviceNumber);
                var productName = NormalizeProductName(capabilities.ProductName, deviceNumber);

                devices.Add(new AudioInputDeviceOption
                {
                    Id = CreateDeviceId(deviceNumber, productName),
                    DisplayName = productName,
                    DeviceNumber = deviceNumber,
                    IsDefault = false
                });
            }
        }
        catch
        {
            return [CreateSystemDefaultOption(systemDefaultDisplayName)];
        }

        return devices;
    }

    public int? ResolveDeviceNumber(string? audioInputDeviceId)
    {
        if (IsSystemDefault(audioInputDeviceId))
        {
            return null;
        }

        try
        {
            for (var deviceNumber = 0; deviceNumber < WaveIn.DeviceCount; deviceNumber++)
            {
                var capabilities = WaveIn.GetCapabilities(deviceNumber);
                var productName = NormalizeProductName(capabilities.ProductName, deviceNumber);
                var deviceId = CreateDeviceId(deviceNumber, productName);

                if (string.Equals(deviceId, audioInputDeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    return deviceNumber;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public AudioInputDeviceOption CreateSystemDefaultOption(string systemDefaultDisplayName)
    {
        return new AudioInputDeviceOption
        {
            Id = AudioConstants.DefaultAudioInputDeviceId,
            DisplayName = systemDefaultDisplayName,
            DeviceNumber = AudioConstants.DefaultAudioInputDeviceNumber,
            IsDefault = true
        };
    }

    public bool IsSystemDefault(string? audioInputDeviceId)
    {
        return string.IsNullOrWhiteSpace(audioInputDeviceId)
            || string.Equals(audioInputDeviceId, AudioConstants.DefaultAudioInputDeviceId, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateDeviceId(int deviceNumber, string productName)
    {
        return $"{AudioConstants.AudioInputDeviceIdPrefix}:{deviceNumber}:{productName}";
    }

    private static string NormalizeProductName(string? productName, int deviceNumber)
    {
        return string.IsNullOrWhiteSpace(productName)
            ? $"Microphone {deviceNumber + 1}"
            : productName.Trim();
    }
}
