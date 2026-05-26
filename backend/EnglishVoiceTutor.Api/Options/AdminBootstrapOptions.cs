namespace EnglishVoiceTutor.Api.Options;

public sealed class AdminBootstrapOptions
{
    public const string SectionName = "AdminBootstrap";

    public bool Enabled { get; init; }

    public string[] AdminEmails { get; init; } = [];
}
