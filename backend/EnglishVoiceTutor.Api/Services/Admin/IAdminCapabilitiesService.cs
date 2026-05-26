using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminCapabilitiesService
{
    AdminCapabilitiesResponse GetCapabilities();
}
