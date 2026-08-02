using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayReconciliationWorker(IServiceScopeFactory scopeFactory, IOptions<GooglePlayReconciliationOptions> optionsAccessor, ILogger<GooglePlayReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(optionsAccessor.Value.PollIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<GooglePlayReconciliationIterationService>().RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
            catch (Exception)
            {
                logger.LogWarning("Google Play reconciliation iteration failed. ResultCode={ResultCode}.", GooglePlayRtdnSafeErrorCodes.ProviderUnavailable);
            }
            await Task.Delay(delay, stoppingToken);
        }
    }
}
