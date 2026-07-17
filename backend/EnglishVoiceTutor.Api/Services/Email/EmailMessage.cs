namespace EnglishVoiceTutor.Api.Services.Email;

public sealed class EmailMessage
{
    public const int SubjectMaxLength = 256;
    public const int BodyMaxLength = 20_000;

    public EmailMessage(string recipientEmail, string subject, string? plainTextBody, string? htmlBody = null)
    {
        RecipientEmail = NormalizeRequired(recipientEmail, 320, "recipient");
        Subject = NormalizeRequired(subject, SubjectMaxLength, "subject");
        PlainTextBody = plainTextBody?.Trim() ?? string.Empty;
        HtmlBody = string.IsNullOrWhiteSpace(htmlBody) ? null : htmlBody.Trim();
        if (PlainTextBody.Length == 0 && HtmlBody is null)
        {
            throw new EmailMessageValidationException("An email body is required.");
        }

        if (PlainTextBody.Length > BodyMaxLength || (HtmlBody?.Length ?? 0) > BodyMaxLength)
        {
            throw new EmailMessageValidationException("The email body is too long.");
        }
    }

    public string RecipientEmail { get; }
    public string Subject { get; }
    public string PlainTextBody { get; }
    public string? HtmlBody { get; }

    private static string NormalizeRequired(string? value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maxLength)
        {
            throw new EmailMessageValidationException($"The email {fieldName} is invalid.");
        }

        return normalized;
    }
}

public sealed class EmailMessageValidationException(string message) : Exception(message);
public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException() : base("Email delivery is unavailable.") { }
}
