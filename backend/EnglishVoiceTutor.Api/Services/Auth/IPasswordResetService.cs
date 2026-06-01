using EnglishVoiceTutor.Api.Contracts.Auth;

namespace EnglishVoiceTutor.Api.Services.Auth;

public interface IPasswordResetService
{
    Task RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken cancellationToken);
    Task<bool> ConfirmPasswordResetAsync(PasswordResetConfirmRequest request, CancellationToken cancellationToken);
}
