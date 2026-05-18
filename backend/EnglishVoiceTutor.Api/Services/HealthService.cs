using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Health;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services;

public sealed class HealthService : IHealthService
{
    private const string UnknownProvider = "Unknown";

    private readonly AppDbContext dbContext;
    private readonly IHostEnvironment environment;
    private readonly ILogger<HealthService> logger;

    public HealthService(
        AppDbContext dbContext,
        IHostEnvironment environment,
        ILogger<HealthService> logger)
    {
        this.dbContext = dbContext;
        this.environment = environment;
        this.logger = logger;
    }

    public HealthResponse GetHealth()
    {
        return new HealthResponse(
            ApiConstants.HealthyStatus,
            environment.EnvironmentName,
            DateTimeOffset.UtcNow);
    }

    public async Task<DatabaseHealthResponse> GetDatabaseHealthAsync(CancellationToken cancellationToken)
    {
        var provider = dbContext.Database.ProviderName ?? UnknownProvider;

        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return new DatabaseHealthResponse(
                canConnect ? ApiConstants.HealthyStatus : ApiConstants.UnhealthyStatus,
                canConnect,
                provider,
                DateTimeOffset.UtcNow,
                canConnect ? null : ApiConstants.DatabaseHealthUnavailableError);
        }
        catch (Exception exception) when (exception is InvalidOperationException or DbUpdateException or TimeoutException or OperationCanceledException or Npgsql.NpgsqlException)
        {
            logger.LogWarning(exception, "Database health check failed safely without exposing connection details.");

            return new DatabaseHealthResponse(
                ApiConstants.UnhealthyStatus,
                false,
                provider,
                DateTimeOffset.UtcNow,
                ApiConstants.DatabaseHealthUnavailableError);
        }
    }
}
