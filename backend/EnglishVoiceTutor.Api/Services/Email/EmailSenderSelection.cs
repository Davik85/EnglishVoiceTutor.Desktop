using EnglishVoiceTutor.Api.Options;

namespace EnglishVoiceTutor.Api.Services.Email;

internal static class EmailSenderSelection
{
    internal static bool ShouldUseSmtp(SmtpEmailOptions smtpOptions) =>
        smtpOptions.Enabled
        && !string.IsNullOrWhiteSpace(smtpOptions.Host)
        && smtpOptions.Port > 0
        && !string.IsNullOrWhiteSpace(smtpOptions.FromAddress);
}
