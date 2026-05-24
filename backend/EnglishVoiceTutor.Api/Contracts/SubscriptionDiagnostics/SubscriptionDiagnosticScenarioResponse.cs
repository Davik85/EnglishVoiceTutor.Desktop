using EnglishVoiceTutor.Api.Contracts.Subscription;

namespace EnglishVoiceTutor.Api.Contracts.SubscriptionDiagnostics;

public sealed class SubscriptionDiagnosticScenarioResponse
{
    public string Scenario { get; set; } = string.Empty;
    public string AppliedTo { get; set; } = string.Empty;
    public DateTimeOffset AppliedAtUtc { get; set; }
    public SubscriptionStatusResponse Status { get; set; } = new();
}
