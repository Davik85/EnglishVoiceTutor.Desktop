using EnglishVoiceTutor.Api.Data.Entities;

namespace EnglishVoiceTutor.Api.Services.Email;

public sealed class NoOpPasswordResetEmailSender(ILogger<NoOpPasswordResetEmailSender> logger) : IPasswordResetEmailSender
{
    public Task SendPasswordResetAsync(UserEntity user, string resetToken, string resetUrl, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Password reset email delivery is not configured. Reset email was not sent. UserId={UserId}; ResetUrlConfigured={ResetUrlConfigured}.",
            user.Id,
            !string.IsNullOrWhiteSpace(resetUrl));
        return Task.CompletedTask;
    }
}
