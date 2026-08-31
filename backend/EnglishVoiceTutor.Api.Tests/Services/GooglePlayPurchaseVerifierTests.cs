using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayPurchaseVerifierTests
{
    [Fact]
    public async Task DisabledOrIncompleteConfigurationReturnsNotConfiguredWithoutCallingClient()
    {
        foreach (var options in new[]
        {
            Options(enabled: false),
            Options(enabled: true, packageName: ""),
            Options(enabled: true, allowedProductIds: [])
        })
        {
            var client = new RecordingClient();
            var result = await CreateVerifier(client, options).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
            Assert.Equal(GooglePlayPurchaseVerificationResultCode.NotConfigured, result.Code);
            Assert.Empty(client.Calls);
        }
    }

    [Fact]
    public async Task ExactPackageAndTokenArePassedToClientWithoutTrimming()
    {
        const string token = "  fake-token-with-whitespace  ";
        var client = new RecordingClient(Snapshot("SUBSCRIPTION_STATE_PENDING"));

        await CreateVerifier(client, Options()).VerifyAsync(Guid.NewGuid(), token, TestContext.Current.CancellationToken);

        var call = Assert.Single(client.Calls);
        Assert.Equal("com.example.test", call.PackageName);
        Assert.Equal(token, call.Token);
    }

    [Fact]
    public async Task ActiveAllowedProductWithPendingAcknowledgementReturnsCompleteVerifiedMetadata()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", startTime: Timestamp("2026-07-27T10:00:00+02:00"), lineItems: [LineItem("server-product", Timestamp("2026-08-27T11:00:00+02:00"))])), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, result.Code);
        Assert.Equal("server-product", result.VerifiedPurchase!.ProductId);
        Assert.Equal(Timestamp("2026-07-27T08:00:00Z"), result.VerifiedPurchase.StartedAtUtc);
        Assert.Equal(Timestamp("2026-08-27T09:00:00Z"), result.VerifiedPurchase.ExpiresAtUtc);
        Assert.Equal(GooglePlayPurchaseAcknowledgementState.Pending, result.VerifiedPurchase.AcknowledgementState);
        Assert.False(result.VerifiedPurchase.IsTestPurchase);
        Assert.Equal(TimeSpan.Zero, result.VerifiedPurchase.StartedAtUtc.Offset);
        Assert.Equal(TimeSpan.Zero, result.VerifiedPurchase.ExpiresAtUtc.Offset);
    }

    [Fact]
    public async Task TestPurchaseIsRejectedByDefault()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))], GooglePlayPurchaseAcknowledgementState.Acknowledged, isTestPurchase: true)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Fact]
    public async Task TestPurchaseRequiresEnabledGateAndAllowlistedAuthenticatedUser()
    {
        var userId = Guid.NewGuid();
        var snapshot = Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))], GooglePlayPurchaseAcknowledgementState.Acknowledged, isTestPurchase: true);

        var disabledGate = await CreateVerifier(new RecordingClient(snapshot), Options(testPurchasesEnabled: false, allowedTestPurchaseUserIds: [userId.ToString("D")])).VerifyAsync(userId, "fake-token", TestContext.Current.CancellationToken);
        var wrongUser = await CreateVerifier(new RecordingClient(snapshot), Options(testPurchasesEnabled: true, allowedTestPurchaseUserIds: [Guid.NewGuid().ToString("D")])).VerifyAsync(userId, "fake-token", TestContext.Current.CancellationToken);
        var allowed = await CreateVerifier(new RecordingClient(snapshot), Options(testPurchasesEnabled: true, allowedTestPurchaseUserIds: [userId.ToString("D")])).VerifyAsync(userId, "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, disabledGate.Code);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, wrongUser.Code);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, allowed.Code);
        Assert.True(allowed.VerifiedPurchase!.IsTestPurchase);
    }

    [Fact]
    public async Task AllowlistedLicenseTesterGetsAcceleratedDeferralEvidenceOnlyThroughExistingTestControls()
    {
        var userId = Guid.NewGuid();
        var snapshot = Snapshot(
            "SUBSCRIPTION_STATE_ACTIVE",
            Timestamp("2026-08-03T00:00:00Z"),
            [EligibleMonthlyLineItem("premium", Timestamp("2026-08-03T00:05:00Z"))],
            GooglePlayPurchaseAcknowledgementState.Acknowledged,
            isTestPurchase: true) with { Etag = "test-etag" };

        var allowed = await CreateVerifier(
            new RecordingClient(snapshot),
            Options(
                allowedProductIds: ["premium"],
                testPurchasesEnabled: true,
                allowedTestPurchaseUserIds: [userId.ToString("D")]))
            .VerifyAsync(userId, "fake-token", TestContext.Current.CancellationToken);
        var wrongUser = await CreateVerifier(
            new RecordingClient(snapshot),
            Options(
                allowedProductIds: ["premium"],
                testPurchasesEnabled: true,
                allowedTestPurchaseUserIds: [Guid.NewGuid().ToString("D")]))
            .VerifyAsync(userId, "fake-token", TestContext.Current.CancellationToken);

        Assert.True(allowed.VerifiedPurchase!.InitialPremiumDeferralEvidence!.IsLicenseTestPurchase);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, wrongUser.Code);
    }

    [Fact]
    public async Task ProductionAcceleratedPeriodCannotReceiveLicenseTestDeferralEvidence()
    {
        var snapshot = Snapshot(
            "SUBSCRIPTION_STATE_ACTIVE",
            Timestamp("2026-08-03T00:00:00Z"),
            [EligibleMonthlyLineItem("premium", Timestamp("2026-08-03T00:05:00Z"))],
            GooglePlayPurchaseAcknowledgementState.Acknowledged) with { Etag = "production-etag" };

        var result = await CreateVerifier(
            new RecordingClient(snapshot),
            Options(allowedProductIds: ["premium"], testPurchasesEnabled: true, allowedTestPurchaseUserIds: [Guid.NewGuid().ToString("D")]))
            .VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, result.Code);
        Assert.Null(result.VerifiedPurchase!.InitialPremiumDeferralEvidence);
    }

    [Fact]
    public async Task MalformedTestPurchaseAllowlistEntryFailsClosed()
    {
        var snapshot = Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))], GooglePlayPurchaseAcknowledgementState.Acknowledged, isTestPurchase: true);

        var result = await CreateVerifier(new RecordingClient(snapshot), Options(testPurchasesEnabled: true, allowedTestPurchaseUserIds: ["not-a-guid"])).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Fact]
    public async Task PendingReturnsNoVerifiedMetadata()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_PENDING")), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Pending, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Theory]
    [InlineData("SUBSCRIPTION_STATE_IN_GRACE_PERIOD", GooglePlaySubscriptionLifecycleState.InGracePeriod, "2026-08-27T10:00:00Z")]
    [InlineData("SUBSCRIPTION_STATE_ON_HOLD", GooglePlaySubscriptionLifecycleState.OnHold, "2026-08-27T10:00:00Z")]
    [InlineData("SUBSCRIPTION_STATE_PAUSED", GooglePlaySubscriptionLifecycleState.Paused, "2026-08-27T10:00:00Z")]
    [InlineData("SUBSCRIPTION_STATE_CANCELED", GooglePlaySubscriptionLifecycleState.Canceled, "2026-08-27T10:00:00Z")]
    [InlineData("SUBSCRIPTION_STATE_EXPIRED", GooglePlaySubscriptionLifecycleState.Expired, "2026-08-02T10:00:00Z")]
    public async Task SupportedLifecycleStatesReturnSanitizedVerifiedMetadata(string state, GooglePlaySubscriptionLifecycleState expected, string expiry)
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot(
            state,
            Timestamp("2026-07-27T10:00:00Z"),
            [LineItem("server-product", Timestamp(expiry))])), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, result.Code);
        Assert.Equal(expected, result.VerifiedPurchase!.LifecycleState);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SUBSCRIPTION_STATE_UNSPECIFIED")]
    [InlineData("SUBSCRIPTION_STATE_PENDING_PURCHASE_CANCELED")]
    public async Task UnknownOrNonEntitlingLifecycleStatesFailClosed(string state)
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot(state)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.InvalidPurchase, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Fact]
    public async Task MissingUnsupportedOrMultipleProductsFailClosed()
    {
        foreach (var lineItems in new[] { Array.Empty<GooglePlaySubscriptionLineItemSnapshot>(), new[] { LineItem("   ", Timestamp("2026-08-27T10:00:00Z")) }, new[] { LineItem("unsupported-product", Timestamp("2026-08-27T10:00:00Z")) }, new[] { LineItem("server-product", Timestamp("2026-08-27T10:00:00Z")), LineItem("other-product", Timestamp("2026-08-27T10:00:00Z")) } })
        {
            var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), lineItems)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
            Assert.Equal(lineItems.Length == 0 || lineItems.Any(item => string.IsNullOrWhiteSpace(item.ProductId)) ? GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable : lineItems.Any(item => !string.Equals(item.ProductId, "server-product", StringComparison.Ordinal)) ? GooglePlayPurchaseVerificationResultCode.UnsupportedProduct : GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
        }
    }

    [Fact]
    public async Task DuplicateIdenticalCurrentItemsFailClosed()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z")), LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))])), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task MissingOrMalformedActivePeriodMetadataFailsClosed(bool missingStart, bool missingExpiry, bool ambiguousExpiry)
    {
        DateTimeOffset? start = missingStart ? null : Timestamp("2026-07-27T10:00:00Z");
        DateTimeOffset? expiry = missingExpiry ? null : Timestamp("2026-08-27T10:00:00Z");
        var lineItems = ambiguousExpiry
            ? new[] { LineItem("server-product", expiry), LineItem("server-product", Timestamp("2026-09-27T10:00:00Z")) }
            : new[] { LineItem("server-product", expiry) };

        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", start, lineItems)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Theory]
    [InlineData("2026-07-27T10:00:00Z")]
    [InlineData("2026-07-27T09:59:59Z")]
    public async Task ExpiryEqualToOrEarlierThanStartFailsClosed(string expiryTimestamp)
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot(
            "SUBSCRIPTION_STATE_ACTIVE",
            Timestamp("2026-07-27T10:00:00Z"),
            [LineItem("server-product", Timestamp(expiryTimestamp))])), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Fact]
    public async Task UnknownOrUnspecifiedAcknowledgementFailsClosed()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))], acknowledgementState: null)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Fact]
    public async Task UndefinedAcknowledgementStateFailsClosed()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot(
            "SUBSCRIPTION_STATE_ACTIVE",
            Timestamp("2026-07-27T10:00:00Z"),
            [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))],
            (GooglePlayPurchaseAcknowledgementState)999)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Fact]
    public async Task LinkedPurchaseTokenIsInternalAndImmediateReplacementIsAccepted()
    {
        const string currentToken = "fake-current-token";
        const string linkedToken = "fake-linked-token";
        var snapshot = new GooglePlaySubscriptionV2Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))], GooglePlayPurchaseAcknowledgementState.Pending, false, linkedToken);
        var result = await CreateVerifier(new RecordingClient(snapshot), Options()).VerifyAsync(Guid.NewGuid(), currentToken, TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, result.Code);
        Assert.DoesNotContain("LinkedPurchaseToken", typeof(GooglePlayPurchaseVerificationResult).GetProperties().Select(property => property.Name));
        Assert.DoesNotContain(currentToken, snapshot.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(linkedToken, snapshot.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(currentToken, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(linkedToken, result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeferredReplacementKeepsCurrentProductEffectiveUntilItsExpiry()
    {
        var snapshot = new GooglePlaySubscriptionV2Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"), "replacement-product"), LineItem("replacement-product", null)], GooglePlayPurchaseAcknowledgementState.Pending, false, "linked-old-token");
        var result = await CreateVerifier(new RecordingClient(snapshot), Options(allowedProductIds: ["server-product", "replacement-product"])).VerifyAsync(Guid.NewGuid(), "new-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, result.Code);
        Assert.Equal("server-product", result.VerifiedPurchase!.ProductId);
        Assert.Equal(Timestamp("2026-08-27T10:00:00Z"), result.VerifiedPurchase.ExpiresAtUtc);
    }

    [Fact]
    public async Task DeferredReplacementMismatchAndSelfLinkedTokenFailClosed()
    {
        var mismatch = new GooglePlaySubscriptionV2Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"), "replacement-product"), LineItem("other-product", null)], GooglePlayPurchaseAcknowledgementState.Pending, false, "linked-old-token");
        var mismatchResult = await CreateVerifier(new RecordingClient(mismatch), Options(allowedProductIds: ["server-product", "replacement-product"])).VerifyAsync(Guid.NewGuid(), "new-token", TestContext.Current.CancellationToken);
        var selfResult = await CreateVerifier(new RecordingClient(new GooglePlaySubscriptionV2Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))], GooglePlayPurchaseAcknowledgementState.Pending, false, "new-token")), Options()).VerifyAsync(Guid.NewGuid(), "new-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, mismatchResult.Code);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, selfResult.Code);
    }

    [Fact]
    public async Task PostActivationDeferredReplacementSelectsAllowedCurrentProduct()
    {
        var snapshot = new GooglePlaySubscriptionV2Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-01T00:00:00Z"), [LineItem("old-product", Timestamp("2026-08-02T00:00:00Z")), LineItem("replacement-product", Timestamp("2026-09-01T00:00:00Z"))], GooglePlayPurchaseAcknowledgementState.Pending, false, "linked-old-token");
        var result = await CreateVerifier(new RecordingClient(snapshot), Options(allowedProductIds: ["old-product", "replacement-product"])).VerifyAsync(Guid.NewGuid(), "new-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, result.Code);
        Assert.Equal("replacement-product", result.VerifiedPurchase!.ProductId);
    }

    [Theory]
    [InlineData("unexpected-next-product", null)]
    [InlineData(null, "stale-next-product")]
    public async Task PostActivationDeferredReplacementRejectsResidualDeferredMetadata(string? currentDeferredProduct, string? historicalDeferredProduct)
    {
        var snapshot = new GooglePlaySubscriptionV2Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-01T00:00:00Z"), [LineItem("old-product", Timestamp("2026-08-02T00:00:00Z"), historicalDeferredProduct), LineItem("replacement-product", Timestamp("2026-09-01T00:00:00Z"), currentDeferredProduct)], GooglePlayPurchaseAcknowledgementState.Pending, false, "linked-old-token");
        var result = await CreateVerifier(new RecordingClient(snapshot), Options(allowedProductIds: ["old-product", "replacement-product"])).VerifyAsync(Guid.NewGuid(), "new-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
    }

    [Theory]
    [InlineData("old-product", "replacement-product", "replacement-product")]
    [InlineData("old-product", "replacement-product", "old-product")]
    public async Task LinkedLifecycleRejectsAnyUnsupportedLineItem(string historicalProduct, string currentProduct, string allowedProduct)
    {
        var snapshot = new GooglePlaySubscriptionV2Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-01T00:00:00Z"), [LineItem(historicalProduct, Timestamp("2026-08-02T00:00:00Z")), LineItem(currentProduct, Timestamp("2026-09-01T00:00:00Z"))], GooglePlayPurchaseAcknowledgementState.Pending, false, "linked-old-token");
        var result = await CreateVerifier(new RecordingClient(snapshot), Options(allowedProductIds: [allowedProduct])).VerifyAsync(Guid.NewGuid(), "new-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, result.Code);
    }

    [Fact]
    public async Task PreActivationDeferredReplacementRejectsUnsupportedFutureProduct()
    {
        var snapshot = new GooglePlaySubscriptionV2Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-01T00:00:00Z"), [LineItem("old-product", Timestamp("2026-09-01T00:00:00Z"), "future-product"), LineItem("future-product", null)], GooglePlayPurchaseAcknowledgementState.Pending, false, "linked-old-token");
        var result = await CreateVerifier(new RecordingClient(snapshot), Options(allowedProductIds: ["old-product"])).VerifyAsync(Guid.NewGuid(), "new-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, result.Code);
    }

    [Fact]
    public async Task ImmediateReplacementRejectsUnsupportedCurrentProduct()
    {
        var snapshot = new GooglePlaySubscriptionV2Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-01T00:00:00Z"), [LineItem("unsupported-product", Timestamp("2026-09-01T00:00:00Z"))], GooglePlayPurchaseAcknowledgementState.Pending, false, "linked-old-token");
        var result = await CreateVerifier(new RecordingClient(snapshot), Options(allowedProductIds: ["server-product"])).VerifyAsync(Guid.NewGuid(), "new-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, result.Code);
    }

    [Fact]
    public void SanitizedContractsExcludeTokensPayloadOrderAndCredentialData()
    {
        Assert.Equal(
            ["AcknowledgementState", "IsTestPurchase", "LineItems", "StartTimeUtc", "SubscriptionState"],
            typeof(GooglePlaySubscriptionV2Snapshot).GetProperties().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Equal(
            ["DeferredItemReplacementProductId", "ExpiryTimeUtc", "ProductId"],
            typeof(GooglePlaySubscriptionLineItemSnapshot).GetProperties().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Equal(
            ["AcknowledgementState", "ExpiresAtUtc", "IsTestPurchase", "LifecycleState", "PackageName", "ProductId", "StartedAtUtc"],
            typeof(GooglePlayVerifiedPurchase).GetProperties().Select(property => property.Name).OrderBy(name => name).ToArray());
    }

    [Theory]
    [InlineData(GooglePlaySubscriptionsV2ClientFailure.InvalidPurchase, GooglePlayPurchaseVerificationResultCode.InvalidPurchase)]
    [InlineData(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable, GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable)]
    public async Task SanitizedProviderFailuresMapSafely(GooglePlaySubscriptionsV2ClientFailure failure, GooglePlayPurchaseVerificationResultCode expected)
    {
        var result = await CreateVerifier(new FailingClient(failure), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
        Assert.Equal(expected, result.Code);
    }

    [Fact]
    public async Task CancellationPropagatesAndNoTokenAppearsInLogs()
    {
        const string token = "fake-token-not-for-logs";
        var logger = new RecordingLogger<GooglePlayPurchaseVerifier>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateVerifier(new CancelingClient(), Options(), logger).VerifyAsync(Guid.NewGuid(), token, TestContext.Current.CancellationToken));
        Assert.DoesNotContain(logger.Messages, message => message.Contains(token, StringComparison.Ordinal));
    }

    [Fact]
    public void ProductionRegistrationRemainsDisabledAndNoCredentialsAreConfigured()
    {
        var program = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");
        var registration = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Services/Billing/GooglePlayBillingServiceCollectionExtensions.cs");
        var settings = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/appsettings.json");
        Assert.Contains("AddGooglePlayBilling(builder.Configuration)", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<IGooglePlayPurchaseVerifier, DisabledGooglePlayPurchaseVerifier>()", registration, StringComparison.Ordinal);
        Assert.Contains("AddScoped<IGooglePlayPurchaseVerifier, GooglePlayPurchaseVerifier>()", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("GoogleCredential.FromFile", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("GoogleCredential.FromStream", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("AndroidPublisherService", program, StringComparison.Ordinal);
        Assert.Contains("\"TestPurchasesEnabled\": false", settings, StringComparison.Ordinal);
        Assert.Contains("\"AllowedTestPurchaseUserIds\": []", settings, StringComparison.Ordinal);
        foreach (var forbidden in new[] { "private_key", "client_email", "service_account", "GOOGLE_APPLICATION_CREDENTIALS" }) Assert.DoesNotContain(forbidden, settings, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"type\": \"service_account\"", settings, StringComparison.OrdinalIgnoreCase);
    }

    private static GooglePlayPurchaseVerifier CreateVerifier(IGooglePlaySubscriptionsV2Client client, GooglePlayBillingOptions options, RecordingLogger<GooglePlayPurchaseVerifier>? logger = null) => new(client, Microsoft.Extensions.Options.Options.Create(options), new TestClock(Timestamp("2026-08-03T00:00:00Z")), logger ?? new RecordingLogger<GooglePlayPurchaseVerifier>());
    private static GooglePlayBillingOptions Options(bool enabled = true, string packageName = "com.example.test", List<string>? allowedProductIds = null, bool testPurchasesEnabled = false, List<string>? allowedTestPurchaseUserIds = null) => new() { Enabled = enabled, PackageName = packageName, AllowedProductIds = allowedProductIds ?? ["server-product"], TestPurchasesEnabled = testPurchasesEnabled, AllowedTestPurchaseUserIds = allowedTestPurchaseUserIds ?? [] };
    private static GooglePlaySubscriptionV2Snapshot Snapshot(string? state, DateTimeOffset? startTime = null, IReadOnlyList<GooglePlaySubscriptionLineItemSnapshot>? lineItems = null, GooglePlayPurchaseAcknowledgementState? acknowledgementState = GooglePlayPurchaseAcknowledgementState.Pending, bool isTestPurchase = false, bool hasLinkedPurchaseToken = false) => new(state, startTime, lineItems ?? [], acknowledgementState, isTestPurchase, hasLinkedPurchaseToken ? "linked-old-token" : null);
    private static GooglePlaySubscriptionLineItemSnapshot LineItem(string? productId, DateTimeOffset? expiryTime, string? deferredItemReplacementProductId = null) => new(productId, expiryTime, deferredItemReplacementProductId);
    private static GooglePlaySubscriptionLineItemSnapshot EligibleMonthlyLineItem(string productId, DateTimeOffset expiryTime) => new(productId, expiryTime)
    {
        HasAutoRenewingPlan = true,
        AutoRenewEnabled = true,
        BasePlanId = SubscriptionConstants.Billing.GooglePlayPremiumBasePlanId,
        OfferPhase = GooglePlaySubscriptionOfferPhase.BasePrice,
        HasLatestSuccessfulOrderId = true
    };
    private static DateTimeOffset Timestamp(string value) => DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime();

    private sealed class RecordingClient(GooglePlaySubscriptionV2Snapshot? snapshot = null) : IGooglePlaySubscriptionsV2Client
    {
        public List<(string PackageName, string Token)> Calls { get; } = [];
        public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) { Calls.Add((packageName, purchaseToken)); return Task.FromResult(snapshot); }
        public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    private sealed class FailingClient(GooglePlaySubscriptionsV2ClientFailure failure) : IGooglePlaySubscriptionsV2Client { public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) => throw new GooglePlaySubscriptionsV2ClientException(failure); public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) => throw new GooglePlaySubscriptionsV2ClientException(failure); }
    private sealed class CancelingClient : IGooglePlaySubscriptionsV2Client { public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) => throw new OperationCanceledException(); public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) => throw new OperationCanceledException(); }
    private sealed class TestClock(DateTimeOffset now) : IUtcClock { public DateTimeOffset UtcNow => now; }
    private sealed class RecordingLogger<T> : ILogger<T> { public List<string> Messages { get; } = []; public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null; public bool IsEnabled(LogLevel logLevel) => true; public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception)); }
    private static string ReadRepositoryFile(string relativePath) { var directory = new DirectoryInfo(AppContext.BaseDirectory); while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath))) directory = directory.Parent; Assert.NotNull(directory); return File.ReadAllText(Path.Combine(directory!.FullName, relativePath)); }
}
