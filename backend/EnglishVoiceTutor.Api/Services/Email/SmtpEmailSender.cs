using System.Net;
using System.Net.Mail;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Email;

public sealed class SmtpEmailSender(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public bool IsConfigured => IsSmtpConfigured(options.Value);

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var smtpOptions = options.Value;
        if (!IsSmtpConfigured(smtpOptions))
        {
            throw new EmailDeliveryException();
        }

        using var mailMessage = CreateMailMessage(message, smtpOptions);
        using var client = new SmtpClient(smtpOptions.Host.Trim(), smtpOptions.Port)
        {
            EnableSsl = smtpOptions.UseStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrWhiteSpace(smtpOptions.UserName))
        {
            client.Credentials = new NetworkCredential(smtpOptions.UserName.Trim(), smtpOptions.Password);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(mailMessage, cancellationToken);
            logger.LogInformation("Email delivery succeeded. RecipientDomain={RecipientDomain}.", GetRecipientDomain(message.RecipientEmail));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            SafeFailureLogger.LogEmailDeliveryFailed(logger);
            throw new EmailDeliveryException();
        }
    }

    internal static MailMessage CreateMailMessage(EmailMessage message, SmtpEmailOptions smtpOptions)
    {
        var body = message.HtmlBody ?? message.PlainTextBody;
        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpOptions.FromAddress.Trim(), smtpOptions.FromName.Trim()),
            Subject = message.Subject,
            Body = body,
            IsBodyHtml = message.HtmlBody is not null
        };
        mailMessage.To.Add(new MailAddress(message.RecipientEmail));
        return mailMessage;
    }

    private static bool IsSmtpConfigured(SmtpEmailOptions smtpOptions) =>
        !string.IsNullOrWhiteSpace(smtpOptions.Host)
        && smtpOptions.Port > 0
        && !string.IsNullOrWhiteSpace(smtpOptions.FromAddress);

    private static string GetRecipientDomain(string email)
    {
        var separator = email.LastIndexOf('@');
        return separator > 0 && separator < email.Length - 1 ? email[(separator + 1)..] : "invalid";
    }
}
