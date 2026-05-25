namespace EnglishVoiceTutor.Api.Options;

public sealed class SubscriptionEnforcementOptions
{
    public const string SectionName = "SubscriptionEnforcement";

    public bool Enabled { get; init; } = false;
}
