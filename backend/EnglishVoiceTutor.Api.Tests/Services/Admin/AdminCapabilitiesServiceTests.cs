using System.Text.Json;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services.Admin;

public sealed class AdminCapabilitiesServiceTests
{
    [Fact]
    public void MissingBillingConfigReportsPaddleUnavailable()
    {
        var response = CreateService(new BillingOptions(), new PaddleBillingOptions(), new PaddleWebhookOptions()).GetCapabilities();

        Assert.False(response.Capabilities.BillingProviderConfigured);
        Assert.False(response.Capabilities.PaddleCheckoutAvailable);
        Assert.False(response.Capabilities.PaddleWebhooksAvailable);
        Assert.False(response.Capabilities.PaddleLiveConfigured);
        Assert.False(response.Capabilities.BillingLivePaymentTestComplete);
        Assert.False(response.Capabilities.BillingPaidLaunchReleaseComplete);
    }

    [Fact]
    public void LivePaddleServerConfigReportsConfiguredWithoutSecretsOrReleaseComplete()
    {
        const string apiKey = "pdl_live_secret_test_api_key";
        const string webhookSecret = "pdl_ntfset_secret_test_webhook";
        var response = CreateService(
            new BillingOptions { CheckoutEnabled = true, Provider = "paddle" },
            new PaddleBillingOptions
            {
                CheckoutAdapterEnabled = true,
                Environment = "live",
                ApiKey = apiKey,
                PremiumPriceId = "pri_live_current",
                PremiumLivePriceId = "pri_live_current",
                PremiumLiveProductId = "pro_live_current",
                CheckoutUrl = "https://languagevoicetutor.com/pay.html",
                ExpectedCustomDataApp = "language_voice_tutor",
                ExpectedCustomDataProduct = "language_voice_tutor_pro"
            },
            new PaddleWebhookOptions { Enabled = true, SecretKey = webhookSecret, TimestampToleranceSeconds = 300 }).GetCapabilities();

        Assert.True(response.Capabilities.BillingProviderConfigured);
        Assert.True(response.Capabilities.PaddleCheckoutAvailable);
        Assert.True(response.Capabilities.PaddleWebhooksAvailable);
        Assert.True(response.Capabilities.PaddleLiveConfigured);
        Assert.True(response.Capabilities.PaddleCheckoutUrlConfigured);
        Assert.True(response.Capabilities.PaddleLivePriceConfigured);
        Assert.True(response.Capabilities.PaddleLiveProductConfigured);
        Assert.True(response.Capabilities.PaddleExpectedCustomDataConfigured);
        Assert.True(response.Capabilities.PaddlePublicCheckoutPageConfigured);
        Assert.False(response.Capabilities.BillingLivePaymentTestComplete);
        Assert.False(response.Capabilities.BillingPaidLaunchReleaseComplete);

        var json = JsonSerializer.Serialize(response);
        Assert.DoesNotContain(apiKey, json, StringComparison.Ordinal);
        Assert.DoesNotContain(webhookSecret, json, StringComparison.Ordinal);
        Assert.DoesNotContain("pri_live_current", json, StringComparison.Ordinal);
        Assert.DoesNotContain("pro_live_current", json, StringComparison.Ordinal);
    }

    private static AdminCapabilitiesService CreateService(
        BillingOptions billingOptions,
        PaddleBillingOptions paddleBillingOptions,
        PaddleWebhookOptions paddleWebhookOptions)
    {
        return new AdminCapabilitiesService(
            new TestWebHostEnvironment(),
            new AdminRolePermissionCatalogService(),
            Microsoft.Extensions.Options.Options.Create(billingOptions),
            Microsoft.Extensions.Options.Options.Create(paddleBillingOptions),
            Microsoft.Extensions.Options.Options.Create(paddleWebhookOptions),
            new ConfigurationBuilder().Build());
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "EnglishVoiceTutor.Api.Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
