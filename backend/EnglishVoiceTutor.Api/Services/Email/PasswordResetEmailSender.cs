using EnglishVoiceTutor.Api.Data.Entities;

namespace EnglishVoiceTutor.Api.Services.Email;

public sealed class PasswordResetEmailSender(IEmailSender emailSender) : IPasswordResetEmailSender
{
    public bool IsConfigured => emailSender.IsConfigured;

    public Task SendPasswordResetAsync(UserEntity user, string resetCode, string resetUrl, CancellationToken cancellationToken)
    {
        return emailSender.SendAsync(
            new EmailMessage(user.Email, "Reset your Language Voice Tutor password", BuildBody(resetCode, resetUrl)),
            cancellationToken);
    }

    internal static string BuildBody(string resetCode, string resetUrl)
    {
        var instructions = string.IsNullOrWhiteSpace(resetUrl)
            ? "Open Language Voice Tutor Desktop, choose Forgot password?, and enter the reset code below."
            : $"Open this reset link or enter the reset code in Language Voice Tutor Desktop.\n\nReset link: {resetUrl}";

        return $"We received a request to reset your Language Voice Tutor password.\n\n{instructions}\n\nReset code: {resetCode}\n\nThis code expires soon and can be used only once. If you did not request this reset, you can ignore this email.";
    }
}
