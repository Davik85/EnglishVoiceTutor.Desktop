using System.Net;
using System.Net.Mail;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Email;

public sealed class SmtpPasswordResetEmailSender(IOptions<SmtpEmailOptions> options, ILogger<SmtpPasswordResetEmailSender> logger) : IPasswordResetEmailSender
{
    public bool IsConfigured => IsSmtpConfigured(options.Value);

    public async Task SendPasswordResetAsync(UserEntity user, string resetCode, string resetUrl, CancellationToken cancellationToken)
    {
        var smtpOptions = options.Value;
        if (!IsSmtpConfigured(smtpOptions))
        {
            throw new InvalidOperationException("SMTP email delivery is not configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(smtpOptions.FromAddress.Trim(), smtpOptions.FromName.Trim()),
            Subject = "Reset your Language Voice Tutor password",
            Body = BuildBody(resetCode, resetUrl),
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(user.Email));

        using var client = new SmtpClient(smtpOptions.Host.Trim(), smtpOptions.Port)
        {
            EnableSsl = smtpOptions.UseStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(smtpOptions.UserName))
        {
            client.Credentials = new NetworkCredential(smtpOptions.UserName.Trim(), smtpOptions.Password);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Password reset email sent. UserId={UserId}; ResetUrlConfigured={ResetUrlConfigured}.", user.Id, !string.IsNullOrWhiteSpace(resetUrl));
    }

    private static bool IsSmtpConfigured(SmtpEmailOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Host)
            && options.Port > 0
            && !string.IsNullOrWhiteSpace(options.FromAddress);
    }

    private static string BuildBody(string resetCode, string resetUrl)
    {
        var instructions = string.IsNullOrWhiteSpace(resetUrl)
            ? "Open Language Voice Tutor Desktop, choose Forgot password?, and enter the reset code below."
            : $"Open this reset link or enter the reset code in Language Voice Tutor Desktop.\n\nReset link: {resetUrl}";

        return $"We received a request to reset your Language Voice Tutor password.\n\n{instructions}\n\nReset code: {resetCode}\n\nThis code expires soon and can be used only once. If you did not request this reset, you can ignore this email.";
    }
}
