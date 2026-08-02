using EnglishVoiceTutor.Api.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public static class GooglePlayReconciliationServiceCollectionExtensions
{
    public static IServiceCollection AddGooglePlayReconciliation(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GooglePlayReconciliationOptions>(configuration.GetSection(GooglePlayReconciliationOptions.SectionName));
        var options = configuration.GetSection(GooglePlayReconciliationOptions.SectionName).Get<GooglePlayReconciliationOptions>() ?? new GooglePlayReconciliationOptions();
        if (!options.Enabled) return services;
        options.ValidateForEnabledMode();
        if (configuration.GetSection(GooglePlayBillingOptions.SectionName).Get<GooglePlayBillingOptions>()?.Enabled != true)
            throw new InvalidOperationException("Google Play reconciliation requires Google Play billing to be enabled.");
        if (!services.Any(item => item.ServiceType == typeof(IGooglePlayPurchaseTokenProtectionService)))
            throw new InvalidOperationException("Google Play reconciliation requires Backend Data Protection.");
        if (!services.Any(item => item.ServiceType == typeof(IGooglePlayPurchaseProcessor)) ||
            !services.Any(item => item.ServiceType == typeof(GooglePlayRtdnEventPersistenceService)) ||
            !services.Any(item => item.ServiceType == typeof(GooglePlayPurchaseTokenSecretPersistenceService)))
            throw new InvalidOperationException("Google Play reconciliation requires the existing Google Play processing and persistence services.");
        services.AddScoped<GooglePlayReconciliationIterationService>();
        services.AddHostedService<GooglePlayReconciliationWorker>();
        return services;
    }
}
