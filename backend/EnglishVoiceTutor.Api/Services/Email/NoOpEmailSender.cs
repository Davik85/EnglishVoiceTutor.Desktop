namespace EnglishVoiceTutor.Api.Services.Email;

public sealed class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    public bool IsConfigured => false;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Email delivery is not configured. Message was not sent.");
        return Task.CompletedTask;
    }
}
