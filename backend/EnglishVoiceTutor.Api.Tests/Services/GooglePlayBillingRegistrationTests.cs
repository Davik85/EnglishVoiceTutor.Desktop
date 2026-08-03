using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayBillingRegistrationTests
{
    [Fact]
    public void MissingOrDisabledConfigurationRegistersOnlyDisabledGooglePlayBoundaries()
    {
        foreach (var configuration in new[] { Configuration(), Configuration(("GooglePlayBilling:Enabled", "false")) })
        {
            var services = new ServiceCollection();
            services.AddGooglePlayBilling(configuration);

            Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IGooglePlayPurchaseVerifier) && descriptor.ImplementationType == typeof(DisabledGooglePlayPurchaseVerifier));
            Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IGooglePlaySubscriptionsV2Client) && descriptor.ImplementationType == typeof(DisabledGooglePlaySubscriptionsV2Client));
            Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IGooglePlayAndroidPublisherServiceFactory));
        }
    }

    [Fact]
    public void EnabledConfigurationRegistersOnlyDormantLiveGraphWithoutLoadingCredentials()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGooglePlayBilling(Configuration(("GooglePlayBilling:Enabled", "true"), ("GooglePlayBilling:PackageName", "")));
        var countingFactory = new CountingFactory();
        services.Replace(ServiceDescriptor.Singleton<IGooglePlayAndroidPublisherServiceFactory>(countingFactory));

        using var provider = services.BuildServiceProvider();
        var verifier = provider.GetRequiredService<IGooglePlayPurchaseVerifier>();

        Assert.IsType<GooglePlayPurchaseVerifier>(verifier);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IGooglePlaySubscriptionsV2Client) && descriptor.ImplementationType == typeof(GooglePlaySubscriptionsV2Client));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IGooglePlayPurchaseVerifier) && descriptor.ImplementationType == typeof(DisabledGooglePlayPurchaseVerifier));
        Assert.Equal(0, countingFactory.Calls);
    }

    [Fact]
    public async Task EnabledIncompleteConfigurationReturnsNotConfiguredWithoutCreatingService()
    {
        var factory = new CountingFactory();
        var client = new GooglePlaySubscriptionsV2Client(factory);
        var verifier = new GooglePlayPurchaseVerifier(client, Microsoft.Extensions.Options.Options.Create(new GooglePlayBillingOptions { Enabled = true, PackageName = "", AllowedProductIds = ["server-product"] }), new TestClock(), Microsoft.Extensions.Logging.Abstractions.NullLogger<GooglePlayPurchaseVerifier>.Instance);

        var result = await verifier.VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.NotConfigured, result.Code);
        Assert.Equal(0, factory.Calls);
    }

    private sealed class TestClock : IUtcClock { public DateTimeOffset UtcNow => new(2026, 8, 3, 0, 0, 0, TimeSpan.Zero); }

    [Fact]
    public async Task FactoryCancellationPropagatesWithoutLoadingCredentials()
    {
        var client = new GooglePlaySubscriptionsV2Client(new CancelingFactory());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("com.example.test", "fake-token", TestContext.Current.CancellationToken));
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) => new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(value => value.Key, value => (string?)value.Value)).Build();
    private sealed class CountingFactory : IGooglePlayAndroidPublisherServiceFactory { public int Calls { get; private set; } public Task<Google.Apis.AndroidPublisher.v3.AndroidPublisherService> CreateAsync(CancellationToken cancellationToken) { Calls++; throw new InvalidOperationException(); } }
    private sealed class CancelingFactory : IGooglePlayAndroidPublisherServiceFactory { public Task<Google.Apis.AndroidPublisher.v3.AndroidPublisherService> CreateAsync(CancellationToken cancellationToken) => throw new OperationCanceledException(); }
}
