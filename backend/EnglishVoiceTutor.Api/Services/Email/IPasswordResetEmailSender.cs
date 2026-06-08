using EnglishVoiceTutor.Api.Data.Entities;

namespace EnglishVoiceTutor.Api.Services.Email;

public interface IPasswordResetEmailSender
{
    bool IsConfigured { get; }

    Task SendPasswordResetAsync(UserEntity user, string resetToken, string resetUrl, CancellationToken cancellationToken);
}
