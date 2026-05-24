using EnglishVoiceTutor.Api.Contracts.SubscriptionDiagnostics;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public interface ISubscriptionDiagnosticsService
{
    Task<SubscriptionDiagnosticScenarioResponse> ApplyScenarioAsync(string scenario, Guid userId, string source, CancellationToken cancellationToken);
}
