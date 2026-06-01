using EnglishVoiceTutor.Api.Data.Entities;

namespace EnglishVoiceTutor.Api.Services.Email;

public interface IPasswordResetEmailSender
{
    Task SendPasswordResetAsync(UserEntity user, string resetToken, string resetUrl, CancellationToken cancellationToken);
}
