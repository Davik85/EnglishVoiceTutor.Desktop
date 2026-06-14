using EnglishVoiceTutor.Api.Contracts.Devices;

namespace EnglishVoiceTutor.Api.Services.Devices;

public interface IDeviceRegistrationService
{
    Task<DeviceRegistrationResponse> RegisterAsync(Guid userId, DeviceRegistrationRequest request, CancellationToken cancellationToken);
}
