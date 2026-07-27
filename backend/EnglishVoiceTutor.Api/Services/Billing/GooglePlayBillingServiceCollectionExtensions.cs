using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public static class GooglePlayBillingServiceCollectionExtensions
{
    public static IServiceCollection AddGooglePlayBilling(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GooglePlayBillingOptions>(configuration.GetSection(GooglePlayBillingOptions.SectionName));
        var options = configuration.GetSection(GooglePlayBillingOptions.SectionName).Get<GooglePlayBillingOptions>() ?? new GooglePlayBillingOptions();
        if (!options.Enabled)
        {
            services.AddScoped<IGooglePlayPurchaseVerifier, DisabledGooglePlayPurchaseVerifier>();
            return services;
        }

        services.AddScoped<IGooglePlayPurchaseVerifier, GooglePlayPurchaseVerifier>();
        services.AddScoped<IGooglePlaySubscriptionsV2Client, GooglePlaySubscriptionsV2Client>();
        services.AddSingleton<IGooglePlayAndroidPublisherServiceFactory, GooglePlayAndroidPublisherServiceFactory>();
        return services;
    }
}
