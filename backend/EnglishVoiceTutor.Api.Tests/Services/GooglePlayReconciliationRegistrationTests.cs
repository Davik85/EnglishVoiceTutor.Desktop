using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayReconciliationRegistrationTests
{
    [Fact]
    public void DisabledModeDoesNotRegisterHostedWorker()
    {
        var services = new ServiceCollection();
        services.AddGooglePlayReconciliation(Configuration());
        Assert.DoesNotContain(services, item => item.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void EnabledModeRequiresBillingAndProtection()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddGooglePlayReconciliation(Configuration(("GooglePlayReconciliation:Enabled", "true"))));
    }

    [Fact]
    public void ValidEnabledModeRegistersWorker()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGooglePlayPurchaseTokenProtectionService, FakeProtection>();
        services.AddScoped<IGooglePlayPurchaseProcessor, FakeProcessor>();
        services.AddScoped<GooglePlayRtdnEventPersistenceService>();
        services.AddScoped<GooglePlayPurchaseTokenSecretPersistenceService>();
        services.AddGooglePlayReconciliation(Configuration(("GooglePlayReconciliation:Enabled", "true"), ("GooglePlayBilling:Enabled", "true")));
        Assert.Contains(services, item => item.ServiceType == typeof(IHostedService) && item.ImplementationType == typeof(GooglePlayReconciliationWorker));
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) => new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(item => item.Key, item => (string?)item.Value)).Build();
    private sealed class FakeProtection : IGooglePlayPurchaseTokenProtectionService { public string Protect(string purchaseToken) => "protected"; public GooglePlayPurchaseTokenUnprotectResult TryUnprotect(string protectedPurchaseToken) => GooglePlayPurchaseTokenUnprotectResult.Failure; }
    private sealed class FakeProcessor : IGooglePlayPurchaseProcessor { public Task<GooglePlayPurchaseProcessingResult> ProcessAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) => Task.FromResult(new GooglePlayPurchaseProcessingResult(GooglePlayPurchaseProcessingResultCode.Verified)); }
}
