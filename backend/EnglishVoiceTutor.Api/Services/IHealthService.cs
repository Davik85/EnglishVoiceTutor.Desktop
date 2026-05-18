using EnglishVoiceTutor.Api.Contracts.Health;

namespace EnglishVoiceTutor.Api.Services;

public interface IHealthService
{
    HealthResponse GetHealth();

    Task<DatabaseHealthResponse> GetDatabaseHealthAsync(CancellationToken cancellationToken);
}
