namespace EnglishVoiceTutor.Api.Options;

public sealed class DevelopmentTestAccountOptions
{
    public const string SectionName = "DevelopmentTestAccounts";

    public string[] UnlimitedPremiumEmails { get; init; } = [];
}
