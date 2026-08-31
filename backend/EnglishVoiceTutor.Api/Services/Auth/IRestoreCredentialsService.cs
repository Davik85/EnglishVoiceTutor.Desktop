using EnglishVoiceTutor.Api.Contracts.Auth;

namespace EnglishVoiceTutor.Api.Services.Auth;

public interface IRestoreCredentialsService
{
    Task<RestoreCredentialCeremonyResponse?> CreateRegistrationOptionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> VerifyRegistrationAsync(Guid userId, RestoreCredentialVerifyRequest request, CancellationToken cancellationToken);
    Task<RestoreCredentialCeremonyResponse?> CreateAssertionOptionsAsync(CancellationToken cancellationToken);
    Task<AuthResponse?> VerifyAssertionAsync(RestoreCredentialVerifyRequest request, CancellationToken cancellationToken);
}
