using EnglishVoiceTutor.Api.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public static class GooglePlayPendingRefundReviewServiceCollectionExtensions
{
    public static IServiceCollection AddGooglePlayPendingRefundReview(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GooglePlayPendingRefundReviewOptions>(configuration.GetSection(GooglePlayPendingRefundReviewOptions.SectionName));
        var options = configuration.GetSection(GooglePlayPendingRefundReviewOptions.SectionName).Get<GooglePlayPendingRefundReviewOptions>() ?? new();
        if (!options.Enabled) return services;
        options.ValidateForEnabledMode();
        if (configuration.GetSection(GooglePlayBillingOptions.SectionName).Get<GooglePlayBillingOptions>()?.Enabled != true || configuration.GetSection(GooglePlayRtdnOptions.SectionName).Get<GooglePlayRtdnOptions>()?.Enabled != true)
            throw new InvalidOperationException("Google Play pending-refund review requires Google Play billing and RTDN to be enabled.");
        if (!services.Any(x => x.ServiceType == typeof(IGooglePlayPendingRefundReviewProtectionService)) || !services.Any(x => x.ServiceType == typeof(IGooglePlayAndroidPublisherServiceFactory)))
            throw new InvalidOperationException("Google Play pending-refund review requires Backend Data Protection and Android Publisher authentication.");
        services.AddScoped<GooglePlayPendingRefundReviewPersistenceService>();
        services.AddScoped<GooglePlayPendingRefundReviewIterationService>();
        services.AddScoped<IGooglePlayReviewRefundClient, GooglePlayReviewRefundClient>();
        services.AddHostedService<GooglePlayPendingRefundReviewWorker>();
        return services;
    }
}
