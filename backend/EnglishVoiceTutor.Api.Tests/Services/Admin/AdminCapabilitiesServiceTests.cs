using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
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
    private static readonly string AdminJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));
    private static readonly string AdminIndex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/index.html"));

    [Fact]
    public void MissingBillingConfigReportsPaddleUnavailable()
    {
        var response = CreateService(new BillingOptions(), new PaddleBillingOptions(), new PaddleWebhookOptions()).GetCapabilities();

        Assert.True(response.Capabilities.CmsUiAvailable);
        Assert.False(response.Capabilities.BillingProviderConfigured);
        Assert.False(response.Capabilities.PaddleCheckoutAvailable);
        Assert.False(response.Capabilities.PaddleWebhooksAvailable);
        Assert.False(response.Capabilities.PaddleLiveConfigured);
        Assert.True(response.Capabilities.BillingLivePaymentTestComplete);
        Assert.False(response.Capabilities.BillingPaidLaunchReleaseComplete);
        Assert.False(response.Capabilities.MobileStoreEntitlementBridgeAvailable);
    }

    [Fact]
    public void CompleteGooglePlayFoundationReportsMobileStoreBridgeWithoutExposingConfiguration()
    {
        const string packageName = "com.example.languagevoicetutor";
        const string audience = "https://example.test/google-play/rtdn";
        const string serviceAccountEmail = "google-play-rtdn@example.test";
        const string subscription = "projects/example/subscriptions/google-play-rtdn";
        var response = CreateService(
            new BillingOptions(),
            new PaddleBillingOptions(),
            new PaddleWebhookOptions(),
            new GooglePlayBillingOptions
            {
                Enabled = true,
                PackageName = packageName,
                AllowedProductIds = [SubscriptionConstants.Billing.GooglePlayPremiumProductId]
            },
            new GooglePlayRtdnOptions
            {
                Enabled = true,
                ExpectedAudience = audience,
                ExpectedServiceAccountEmail = serviceAccountEmail,
                ExpectedPubSubSubscription = subscription
            },
            new GooglePlayReconciliationOptions { Enabled = true }).GetCapabilities();

        Assert.True(response.Capabilities.MobileStoreEntitlementBridgeAvailable);
        Assert.True(response.Capabilities.BillingLivePaymentTestComplete);
        Assert.False(response.Capabilities.BillingPaidLaunchReleaseComplete);
        Assert.False(response.Capabilities.BillingProviderConfigured);
        Assert.False(response.Capabilities.PaddleCheckoutAvailable);

        var json = JsonSerializer.Serialize(response);
        Assert.DoesNotContain(packageName, json, StringComparison.Ordinal);
        Assert.DoesNotContain(audience, json, StringComparison.Ordinal);
        Assert.DoesNotContain(serviceAccountEmail, json, StringComparison.Ordinal);
        Assert.DoesNotContain(subscription, json, StringComparison.Ordinal);
    }

    [Fact]
    public void IncompleteGooglePlayRtdnReportsMobileStoreBridgeUnavailable()
    {
        var response = CreateService(
            new BillingOptions(),
            new PaddleBillingOptions(),
            new PaddleWebhookOptions(),
            CompleteGooglePlayBilling(),
            new GooglePlayRtdnOptions
            {
                Enabled = true,
                ExpectedAudience = "https://example.test/google-play/rtdn",
                ExpectedServiceAccountEmail = "google-play-rtdn@example.test"
            },
            new GooglePlayReconciliationOptions { Enabled = true }).GetCapabilities();

        Assert.False(response.Capabilities.MobileStoreEntitlementBridgeAvailable);
    }

    [Fact]
    public void DisabledGooglePlayReconciliationReportsMobileStoreBridgeUnavailable()
    {
        var response = CreateService(
            new BillingOptions(),
            new PaddleBillingOptions(),
            new PaddleWebhookOptions(),
            CompleteGooglePlayBilling(),
            CompleteGooglePlayRtdn(),
            new GooglePlayReconciliationOptions { Enabled = false }).GetCapabilities();

        Assert.False(response.Capabilities.MobileStoreEntitlementBridgeAvailable);
    }

    [Fact]
    public void AdminSystemCardRendersGooglePlayBridgeCapabilityDynamically()
    {
        Assert.Contains("Release / Capability Status", AdminIndex, StringComparison.Ordinal);
        Assert.Contains("Mobile Store / Google Play", AdminIndex, StringComparison.Ordinal);
        Assert.Contains("id=\"system-mobile-store-google-play-status\"", AdminIndex, StringComparison.Ordinal);
        Assert.DoesNotContain("Future Sections / Deferred Scope", AdminIndex, StringComparison.Ordinal);
        Assert.DoesNotContain("Mobile Store Bridge", AdminIndex, StringComparison.Ordinal);

        Assert.Contains("Boolean(capabilities.mobileStoreEntitlementBridgeAvailable)", AdminJs, StringComparison.Ordinal);
        Assert.Contains("renderMobileStoreGooglePlayStatus(capabilitiesPayload.capabilities || {})", AdminJs, StringComparison.Ordinal);
        Assert.Contains("available ? \"AVAILABLE\" : \"DISABLED / INCOMPLETE\"", AdminJs, StringComparison.Ordinal);
        Assert.Contains("checkoutAvailable && webhooksAvailable && paymentTestComplete && !paidLaunchComplete", AdminJs, StringComparison.Ordinal);
        Assert.Contains("LIVE PAYMENT VERIFIED / PAID LAUNCH READINESS PENDING", AdminJs, StringComparison.Ordinal);
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
        Assert.True(response.Capabilities.BillingLivePaymentTestComplete);
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
        PaddleWebhookOptions paddleWebhookOptions,
        GooglePlayBillingOptions? googlePlayBillingOptions = null,
        GooglePlayRtdnOptions? googlePlayRtdnOptions = null,
        GooglePlayReconciliationOptions? googlePlayReconciliationOptions = null)
    {
        return new AdminCapabilitiesService(
            new TestWebHostEnvironment(),
            new AdminRolePermissionCatalogService(),
            Microsoft.Extensions.Options.Options.Create(billingOptions),
            Microsoft.Extensions.Options.Options.Create(paddleBillingOptions),
            Microsoft.Extensions.Options.Options.Create(paddleWebhookOptions),
            Microsoft.Extensions.Options.Options.Create(googlePlayBillingOptions ?? new GooglePlayBillingOptions()),
            Microsoft.Extensions.Options.Options.Create(googlePlayRtdnOptions ?? new GooglePlayRtdnOptions()),
            Microsoft.Extensions.Options.Options.Create(googlePlayReconciliationOptions ?? new GooglePlayReconciliationOptions()),
            new ConfigurationBuilder().Build());
    }

    private static GooglePlayBillingOptions CompleteGooglePlayBilling() => new()
    {
        Enabled = true,
        PackageName = "com.example.languagevoicetutor",
        AllowedProductIds = [SubscriptionConstants.Billing.GooglePlayPremiumProductId]
    };

    private static GooglePlayRtdnOptions CompleteGooglePlayRtdn() => new()
    {
        Enabled = true,
        ExpectedAudience = "https://example.test/google-play/rtdn",
        ExpectedServiceAccountEmail = "google-play-rtdn@example.test",
        ExpectedPubSubSubscription = "projects/example/subscriptions/google-play-rtdn"
    };

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
