using EnglishVoiceTutor.Api.Data.Entities;

namespace EnglishVoiceTutor.Api.Services.Email;

public interface IPasswordResetEmailSender
{
    bool IsConfigured { get; }

    Task SendPasswordResetAsync(UserEntity user, string resetCode, string resetUrl, CancellationToken cancellationToken);
}
