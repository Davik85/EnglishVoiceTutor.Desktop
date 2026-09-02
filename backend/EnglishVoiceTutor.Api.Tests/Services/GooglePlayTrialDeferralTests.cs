using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayTrialDeferralTests
{
    private static readonly DateTimeOffset PurchaseStart = new(2026, 8, 27, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset BaselineExpiry = PurchaseStart.AddDays(31);

    [Fact]
    public void OrdinaryPaidMonthlySnapshotIsEligible()
    {
        var snapshot = Snapshot(BaselineExpiry, "initial-etag");

        var evidence = GooglePlayTrialDeferralEligibility.Select(snapshot, snapshot.LineItems[0], PurchaseStart, BaselineExpiry, false);

        Assert.NotNull(evidence);
    }

    [Fact]
    public void ExplicitlyAllowedLicenseTestPurchaseMayUseAcceleratedMonthlyPeriod()
    {
        var acceleratedExpiry = PurchaseStart.AddMinutes(5);
        var snapshot = Snapshot(acceleratedExpiry, "test-etag", isTestPurchase: true);

        var evidence = GooglePlayTrialDeferralEligibility.Select(
            snapshot,
            snapshot.LineItems[0],
            PurchaseStart,
            acceleratedExpiry,
            explicitlyAllowedLicenseTestPurchase: true);

        Assert.NotNull(evidence);
        Assert.True(evidence.IsLicenseTestPurchase);
    }

    [Fact]
    public void ProductionPurchaseCannotUseAcceleratedLicenseTestPeriodPath()
    {
        var acceleratedExpiry = PurchaseStart.AddMinutes(5);
        var snapshot = Snapshot(acceleratedExpiry, "production-etag");

        var evidence = GooglePlayTrialDeferralEligibility.Select(
            snapshot,
            snapshot.LineItems[0],
            PurchaseStart,
            acceleratedExpiry,
            explicitlyAllowedLicenseTestPurchase: true);

        Assert.Null(evidence);
    }

    [Theory]
    [InlineData("free_trial")]
    [InlineData("introductory")]
    [InlineData("prepaid")]
    [InlineData("promotion")]
    [InlineData("multi_item")]
    [InlineData("deferred_replacement")]
    [InlineData("deferred_removal")]
    [InlineData("item_replacement")]
    [InlineData("ambiguous_phase")]
    [InlineData("offer")]
    [InlineData("missing_order")]
    [InlineData("not_auto_renewing")]
    [InlineData("linked_purchase")]
    [InlineData("wrong_product")]
    [InlineData("wrong_base_plan")]
    public void NonOrdinaryOrAmbiguousSnapshotIsNotEligible(string scenario)
    {
        var item = EligibleLineItem(BaselineExpiry);
        var snapshot = Snapshot(BaselineExpiry, "initial-etag");
        switch (scenario)
        {
            case "free_trial": item = item with { OfferPhase = GooglePlaySubscriptionOfferPhase.FreeTrial }; break;
            case "introductory": item = item with { OfferPhase = GooglePlaySubscriptionOfferPhase.IntroductoryPrice }; break;
            case "prepaid": item = item with { HasAutoRenewingPlan = false, AutoRenewEnabled = null, HasPrepaidPlan = true }; break;
            case "promotion": item = item with { HasSignupPromotion = true }; break;
            case "multi_item": snapshot = snapshot with { LineItems = [item, item] }; break;
            case "deferred_replacement": item = item with { DeferredItemReplacementProductId = "premium" }; break;
            case "deferred_removal": item = item with { HasDeferredItemRemoval = true }; break;
            case "item_replacement": item = item with { HasItemReplacement = true }; break;
            case "ambiguous_phase": item = item with { OfferPhase = GooglePlaySubscriptionOfferPhase.Ambiguous }; break;
            case "offer": item = item with { OfferId = "discount" }; break;
            case "missing_order": item = item with { HasLatestSuccessfulOrderId = false }; break;
            case "not_auto_renewing": item = item with { AutoRenewEnabled = false }; break;
            case "linked_purchase": snapshot = snapshot with { LinkedPurchaseToken = "old-token" }; break;
            case "wrong_product": item = item with { ProductId = "premium-plus" }; break;
            case "wrong_base_plan": item = item with { BasePlanId = "annual" }; break;
        }
        if (scenario != "multi_item") snapshot = snapshot with { LineItems = [item] };

        Assert.Null(GooglePlayTrialDeferralEligibility.Select(snapshot, snapshot.LineItems[0], PurchaseStart, BaselineExpiry, false));
    }

    [Fact]
    public async Task ActiveTrialCapturesImmutableActualRemainingDuration()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var trial = await AddTrialAsync(db, userId, PurchaseStart.AddDays(5).AddMinutes(7));

        await CreatePersistence(db).PersistAsync(Request(userId, "token-one"), TestContext.Current.CancellationToken);

        var plan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(trial.GrantedAtUtc, plan.ExistingCoverageStartsAtUtc);
        Assert.Equal(trial.ExpiresAtUtc, plan.ExistingCoverageTailUtc);
        Assert.Equal(TimeSpan.FromDays(5).Add(TimeSpan.FromMinutes(7)).Ticks, plan.ApprovedDeferDurationTicks);
        Assert.Equal(BaselineExpiry.AddDays(5).AddMinutes(7), plan.TargetProviderExpiryUtc);
        Assert.Null(plan.CommandEtag);
        Assert.Equal(GooglePlayTrialDeferralStatuses.Pending, plan.Status);
    }

    [Fact]
    public async Task TrialAndScheduledManualPremiumCaptureCompleteExistingCoverageTail()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var trialEnd = PurchaseStart.AddDays(5);
        var manualEnd = trialEnd.AddDays(30);
        await AddTrialAsync(db, userId, trialEnd);
        await AddEntitlementAsync(
            db,
            userId,
            trialEnd,
            manualEnd,
            SubscriptionConstants.Entitlements.SourceManualAdmin);

        await CreatePersistence(db).PersistAsync(Request(userId, "token-one"), TestContext.Current.CancellationToken);

        var plan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(manualEnd, plan.ExistingCoverageTailUtc);
        Assert.Equal((manualEnd - PurchaseStart).Ticks, plan.ApprovedDeferDurationTicks);
        Assert.Equal(BaselineExpiry.Add(manualEnd - PurchaseStart), plan.TargetProviderExpiryUtc);
    }

    [Fact]
    public async Task PositiveSubDayTrialUsesExactlyTwentyFourHours()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddTrialAsync(db, userId, PurchaseStart.AddHours(2));

        await CreatePersistence(db).PersistAsync(Request(userId, "token-one"), TestContext.Current.CancellationToken);

        var plan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(TimeSpan.FromDays(1).Ticks, plan.ApprovedDeferDurationTicks);
        Assert.Equal(BaselineExpiry.AddDays(1), plan.TargetProviderExpiryUtc);
    }

    [Fact]
    public async Task PurchaseAtTrialExpiryCapturesNoPlan()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddTrialAsync(db, userId, PurchaseStart);

        await CreatePersistence(db).PersistAsync(Request(userId, "token-one"), TestContext.Current.CancellationToken);

        Assert.Empty(db.GooglePlayInitialPremiumDeferrals);
    }

    [Fact]
    public async Task RepeatedPersistenceForOneClaimCapturesOnlyOneImmutablePlan()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddTrialAsync(db, userId, PurchaseStart.AddDays(5));
        var persistence = CreatePersistence(db);

        await persistence.PersistAsync(Request(userId, "token-one"), TestContext.Current.CancellationToken);
        await persistence.PersistAsync(Request(userId, "token-one"), TestContext.Current.CancellationToken);

        Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Single(db.GooglePlayPurchaseClaims);
        var entity = db.Model.FindEntityType(typeof(GooglePlayInitialPremiumDeferralEntity))!;
        Assert.True(entity.GetIndexes().Single(index => index.Properties.Count == 1 && index.Properties[0].Name == nameof(GooglePlayInitialPremiumDeferralEntity.GooglePlayPurchaseClaimId)).IsUnique);
    }

    [Fact]
    public async Task FreshPostAcknowledgementEtagDrivesDeferAndAuthoritativeRefreshPersistence()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "post-ack-etag"), Snapshot(target, "post-defer-etag")],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, target)]));
        var service = CreateDeferralService(db, clock, client);

        var result = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, result.Code);
        var call = Assert.Single(client.DeferCalls);
        Assert.Equal("post-ack-etag", call.Etag);
        Assert.Equal(TimeSpan.FromDays(5), call.Duration);
        Assert.Equal(target, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(target, Assert.Single(db.Entitlements).ExpiresAtUtc);
        var plan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(GooglePlayTrialDeferralStatuses.Completed, plan.Status);
        Assert.Equal(target, plan.AuthoritativeProviderExpiryUtc);
    }

    [Fact]
    public async Task AllowedLicenseTestPlanUsesAcceleratedProviderPeriodWithoutWeakeningShapeChecks()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var userId = await AddUserAsync(db);
        await AddTrialAsync(db, userId, PurchaseStart.AddDays(5));
        var testBaseline = PurchaseStart.AddMinutes(5);
        const string token = "license-test-token";
        const string protectedToken = "protected-license-test-token";
        await CreatePersistence(db, clock).PersistAsync(
            Request(userId, token, testBaseline, isLicenseTestPurchase: true) with { ProtectedPurchaseToken = protectedToken },
            TestContext.Current.CancellationToken);
        var target = testBaseline.AddDays(5);
        var client = new ScriptedClient(
            [Snapshot(testBaseline, "test-command-etag", isTestPurchase: true), Snapshot(target, "test-target-etag", isTestPurchase: true)],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, target)]));

        var result = await CreateDeferralService(db, clock, client).ProcessAsync(
            userId,
            token,
            protectedToken,
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, result.Code);
        Assert.Equal(TimeSpan.FromDays(5), Assert.Single(client.DeferCalls).Duration);
        Assert.True(Assert.Single(db.GooglePlayInitialPremiumDeferrals).IsLicenseTestPurchase);
        Assert.Equal(target, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
    }

    [Fact]
    public async Task SuccessfulDeferWithTemporaryRefreshFailureRetriesGetOnly()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "post-ack-etag"), new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable)],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, target)]));
        var service = CreateDeferralService(db, clock, client);

        var first = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Pending, first.Code);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(GooglePlayTrialDeferralStatuses.ProviderAppliedAwaitingRefresh, Assert.Single(db.GooglePlayInitialPremiumDeferrals).Status);
        Assert.Single(client.DeferCalls);

        clock.UtcNow = clock.UtcNow.AddMinutes(2);
        client.GetSteps.Enqueue(Snapshot(target, "post-defer-etag"));
        var second = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, second.Code);
        Assert.Single(client.DeferCalls);
        Assert.Equal(target, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
    }

    [Fact]
    public async Task SuccessfulDeferWithTemporarilyUnusableRefreshRetriesGetOnly()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var temporarilyUnusable = Snapshot(target, "post-defer-etag") with
        {
            SubscriptionState = "SUBSCRIPTION_STATE_UNSPECIFIED"
        };
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "post-ack-etag"), temporarilyUnusable],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, target)]));
        var service = CreateDeferralService(db, clock, client);

        var first = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Pending, first.Code);
        var pendingPlan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(GooglePlayTrialDeferralStatuses.ProviderAppliedAwaitingRefresh, pendingPlan.Status);
        Assert.Equal(target, pendingPlan.ProviderResponseExpiryUtc);
        Assert.Null(pendingPlan.AuthoritativeProviderExpiryUtc);
        Assert.Single(client.DeferCalls);

        clock.UtcNow = clock.UtcNow.AddMinutes(2);
        client.GetSteps.Enqueue(Snapshot(target, "converged-etag"));
        var second = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, second.Code);
        Assert.Single(client.DeferCalls);
        Assert.Equal(target, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
    }

    [Fact]
    public async Task FastCancellationAtExactTargetCompletesWithoutRestoringActiveLifecycle()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var canceled = Snapshot(target, "canceled-etag") with
        {
            SubscriptionState = "SUBSCRIPTION_STATE_CANCELED",
            LineItems = [EligibleLineItem(target) with { AutoRenewEnabled = false }]
        };
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "post-ack-etag"), canceled],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, target)]));

        var result = await CreateDeferralService(db, clock, client).ProcessAsync(
            seeded.UserId,
            seeded.Token,
            seeded.ProtectedToken,
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, result.Code);
        Assert.Single(client.DeferCalls);
        var subscription = Assert.Single(db.Subscriptions);
        Assert.Equal(SubscriptionConstants.SubscriptionStatuses.Canceled, subscription.Status);
        Assert.True(subscription.CancelAtPeriodEnd);
        Assert.Equal(SubscriptionConstants.ScheduledChangeActions.Cancel, subscription.ScheduledChangeAction);
        Assert.Equal(target, subscription.ScheduledChangeEffectiveAtUtc);
        var entitlement = Assert.Single(db.Entitlements);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, entitlement.Status);
        Assert.Equal(target, entitlement.ExpiresAtUtc);
    }

    [Fact]
    public async Task SuccessfulDeferWithoutExactTargetResponseTreatsIntermediateExpiryAsTerminal()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "post-ack-etag"), Snapshot(target.AddDays(1), "contradictory-etag")],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, target.AddDays(2))]));

        var result = await CreateDeferralService(db, clock, client).ProcessAsync(
            seeded.UserId,
            seeded.Token,
            seeded.ProtectedToken,
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.AmbiguousTerminal, result.Code);
        Assert.Single(client.DeferCalls);
        Assert.Equal(GooglePlayTrialDeferralStatuses.AmbiguousTerminal, Assert.Single(db.GooglePlayInitialPremiumDeferrals).Status);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
    }

    [Fact]
    public async Task SuccessfulExactTargetDeferResponseRetriesIntermediateExpiryGetOnlyUntilAuthoritativeTargetConverges()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var intermediate = BaselineExpiry.AddDays(2);
        var logger = new RecordingLogger<GooglePlayTrialDeferralService>();
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "command-etag"), Snapshot(intermediate, "intermediate-etag")],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, target)]));
        var service = CreateDeferralService(db, clock, client, logger);

        var first = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Pending, first.Code);
        Assert.Single(client.DeferCalls);
        var pendingPlan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(GooglePlayTrialDeferralStatuses.ProviderAppliedAwaitingRefresh, pendingPlan.Status);
        Assert.Equal(target, pendingPlan.ProviderResponseExpiryUtc);
        Assert.Null(pendingPlan.AuthoritativeProviderExpiryUtc);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Entitlements).ExpiresAtUtc);
        Assert.Contains(logger.Messages, message => message.Contains("intermediate_expiry_not_yet_converged", StringComparison.Ordinal));

        var ordinaryReconciliationPersistence = await CreatePersistence(db, clock).PersistAsync(
            Request(seeded.UserId, seeded.Token, intermediate) with { ProtectedPurchaseToken = seeded.ProtectedToken },
            TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent, ordinaryReconciliationPersistence.Code);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Entitlements).ExpiresAtUtc);

        clock.UtcNow = clock.UtcNow.AddMinutes(2);
        client.GetSteps.Enqueue(Snapshot(target, "target-etag"));
        var second = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, second.Code);
        Assert.Single(client.DeferCalls);
        Assert.Equal(target, pendingPlan.AuthoritativeProviderExpiryUtc);
        Assert.Equal(target, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(target, Assert.Single(db.Entitlements).ExpiresAtUtc);
        Assert.Contains(logger.Messages, message => message.Contains("target_converged", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message =>
            message.Contains(seeded.Token, StringComparison.Ordinal)
            || message.Contains(seeded.UserId.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProviderPrecisionTargetResponseRetriesIntermediateThenPersistsActualAuthoritativeExpiry()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var localTargetFractionTicks = 3_794_600L;
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5).AddTicks(localTargetFractionTicks));
        var target = BaselineExpiry.AddDays(5).AddTicks(localTargetFractionTicks);
        var providerExpiry = target.AddTicks(-4_600);
        var intermediate = BaselineExpiry.AddDays(2);
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "command-etag"), Snapshot(intermediate, "intermediate-etag")],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, providerExpiry)]));
        var service = CreateDeferralService(db, clock, client);

        var first = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Pending, first.Code);
        Assert.Single(client.DeferCalls);
        var plan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(target, plan.TargetProviderExpiryUtc);
        Assert.Equal(providerExpiry, plan.ProviderResponseExpiryUtc);
        Assert.Null(plan.AuthoritativeProviderExpiryUtc);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Entitlements).ExpiresAtUtc);

        var ordinaryReconciliationPersistence = await CreatePersistence(db, clock).PersistAsync(
            Request(seeded.UserId, seeded.Token, intermediate) with { ProtectedPurchaseToken = seeded.ProtectedToken },
            TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent, ordinaryReconciliationPersistence.Code);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Entitlements).ExpiresAtUtc);

        clock.UtcNow = clock.UtcNow.AddMinutes(2);
        client.GetSteps.Enqueue(Snapshot(providerExpiry, "provider-precision-etag"));
        var second = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, second.Code);
        Assert.Single(client.DeferCalls);
        Assert.Equal(providerExpiry, plan.AuthoritativeProviderExpiryUtc);
        Assert.Equal(providerExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(providerExpiry, Assert.Single(db.Entitlements).ExpiresAtUtc);
        Assert.NotEqual(target, plan.AuthoritativeProviderExpiryUtc);
    }

    [Fact]
    public async Task ProviderPrecisionTargetResponseAndImmediateGetCompleteWithActualProviderExpiry()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5).AddTicks(3_794_600));
        var target = BaselineExpiry.AddDays(5).AddTicks(3_794_600);
        var providerExpiry = target.AddTicks(-4_600);
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "command-etag"), Snapshot(providerExpiry, "provider-precision-etag")],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, providerExpiry)]));

        var result = await CreateDeferralService(db, clock, client).ProcessAsync(
            seeded.UserId,
            seeded.Token,
            seeded.ProtectedToken,
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, result.Code);
        Assert.Single(client.DeferCalls);
        var plan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(target, plan.TargetProviderExpiryUtc);
        Assert.Equal(providerExpiry, plan.AuthoritativeProviderExpiryUtc);
        Assert.Equal(providerExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(providerExpiry, Assert.Single(db.Entitlements).ExpiresAtUtc);
    }

    [Fact]
    public async Task ProviderExpiryExactlyOneMillisecondFromTargetIsNotEquivalent()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5).AddTicks(3_794_600));
        var target = BaselineExpiry.AddDays(5).AddTicks(3_794_600);
        var providerExpiry = target.AddTicks(-TimeSpan.TicksPerMillisecond);
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "command-etag"), Snapshot(providerExpiry, "boundary-etag")],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, providerExpiry)]));

        var result = await CreateDeferralService(db, clock, client).ProcessAsync(
            seeded.UserId,
            seeded.Token,
            seeded.ProtectedToken,
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.AmbiguousTerminal, result.Code);
        Assert.Single(client.DeferCalls);
        var plan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(GooglePlayTrialDeferralStatuses.AmbiguousTerminal, plan.Status);
        Assert.Null(plan.AuthoritativeProviderExpiryUtc);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Entitlements).ExpiresAtUtc);
    }

    [Fact]
    public async Task SuccessfulExactTargetDeferResponseFailsClosedWhenIntermediateExpiryNeverConverges()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var intermediate = BaselineExpiry.AddDays(2);
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "command-etag"), Snapshot(intermediate, "intermediate-etag")],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, target)]));
        var options = new GooglePlayReconciliationOptions { MaximumAttempts = 3 };
        var service = CreateDeferralService(db, clock, client, options: options);

        var first = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);
        clock.UtcNow = clock.UtcNow.AddMinutes(2);
        client.GetSteps.Enqueue(Snapshot(intermediate, "still-intermediate-etag"));
        var second = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Pending, first.Code);
        Assert.Equal(GooglePlayTrialDeferralResultCode.AmbiguousTerminal, second.Code);
        Assert.Single(client.DeferCalls);
        var plan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(GooglePlayTrialDeferralStatuses.AmbiguousTerminal, plan.Status);
        Assert.Null(plan.AuthoritativeProviderExpiryUtc);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Entitlements).ExpiresAtUtc);
    }

    [Fact]
    public async Task SuccessfulExactTargetDeferResponseStillFailsClosedOnWrongProductIdentity()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var logger = new RecordingLogger<GooglePlayTrialDeferralService>();
        var wrongProduct = Snapshot(target, "wrong-product-etag") with
        {
            LineItems = [EligibleLineItem(target) with { ProductId = "premium-plus" }]
        };
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "command-etag"), wrongProduct],
            new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, target)]));

        var result = await CreateDeferralService(db, clock, client, logger).ProcessAsync(
            seeded.UserId,
            seeded.Token,
            seeded.ProtectedToken,
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.AmbiguousTerminal, result.Code);
        Assert.Single(client.DeferCalls);
        Assert.Equal(GooglePlayTrialDeferralStatuses.AmbiguousTerminal, Assert.Single(db.GooglePlayInitialPremiumDeferrals).Status);
        Assert.Contains(logger.Messages, message => message.Contains("identity_contradiction", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LostDeferResponseConvergesFromTargetWithoutSecondMutation()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "command-etag"), Snapshot(target, "new-etag")],
            new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.ProviderOutcomeUnknown));
        var service = CreateDeferralService(db, clock, client);

        var result = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, result.Code);
        Assert.Single(client.DeferCalls);
        Assert.Equal(target, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
    }

    [Fact]
    public async Task LostDeferResponseConvergesFromProviderPrecisionTargetWithoutSecondMutation()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5).AddTicks(3_794_600));
        var target = BaselineExpiry.AddDays(5).AddTicks(3_794_600);
        var providerExpiry = target.AddTicks(-4_600);
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "command-etag"), Snapshot(providerExpiry, "provider-precision-etag")],
            new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.ProviderOutcomeUnknown));

        var result = await CreateDeferralService(db, clock, client).ProcessAsync(
            seeded.UserId,
            seeded.Token,
            seeded.ProtectedToken,
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, result.Code);
        Assert.Single(client.DeferCalls);
        var plan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(providerExpiry, plan.AuthoritativeProviderExpiryUtc);
        Assert.Equal(providerExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(providerExpiry, Assert.Single(db.Entitlements).ExpiresAtUtc);
    }

    [Fact]
    public async Task LostDeferResponseConvergesFromCanceledTargetWithoutSecondMutation()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var canceled = Snapshot(target, "canceled-target-etag") with
        {
            SubscriptionState = "SUBSCRIPTION_STATE_CANCELED",
            LineItems = [EligibleLineItem(target) with { AutoRenewEnabled = false }]
        };
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "command-etag"), canceled],
            new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.ProviderOutcomeUnknown));

        var result = await CreateDeferralService(db, clock, client).ProcessAsync(
            seeded.UserId,
            seeded.Token,
            seeded.ProtectedToken,
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, result.Code);
        Assert.Single(client.DeferCalls);
        var subscription = Assert.Single(db.Subscriptions);
        Assert.Equal(SubscriptionConstants.SubscriptionStatuses.Canceled, subscription.Status);
        Assert.True(subscription.CancelAtPeriodEnd);
        Assert.Equal(SubscriptionConstants.ScheduledChangeActions.Cancel, subscription.ScheduledChangeAction);
        Assert.Equal(target, subscription.ScheduledChangeEffectiveAtUtc);
        var entitlement = Assert.Single(db.Entitlements);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, entitlement.Status);
        Assert.Equal(target, entitlement.ExpiresAtUtc);
        var plan = Assert.Single(db.GooglePlayInitialPremiumDeferrals);
        Assert.Equal(GooglePlayTrialDeferralStatuses.Completed, plan.Status);
        Assert.Equal(target, plan.AuthoritativeProviderExpiryUtc);
    }

    [Fact]
    public async Task LostDeferResponseWithTemporarilyUnusableTargetRetriesGetOnly()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var temporarilyUnusable = Snapshot(target, "unusable-target-etag") with
        {
            SubscriptionState = "SUBSCRIPTION_STATE_UNSPECIFIED"
        };
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "command-etag"), temporarilyUnusable],
            new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.ProviderOutcomeUnknown));
        var service = CreateDeferralService(db, clock, client);

        var first = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Pending, first.Code);
        Assert.Equal(GooglePlayTrialDeferralStatuses.ProviderAppliedAwaitingRefresh, Assert.Single(db.GooglePlayInitialPremiumDeferrals).Status);
        Assert.Single(client.DeferCalls);

        clock.UtcNow = clock.UtcNow.AddMinutes(2);
        client.GetSteps.Enqueue(Snapshot(target, "converged-target-etag"));
        var second = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.Completed, second.Code);
        Assert.Single(client.DeferCalls);
        Assert.Equal(target, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
    }

    [Fact]
    public async Task UnexpectedEtagDivergenceBecomesTerminalWithoutAnotherMutation()
    {
        await using var db = CreateDb();
        var clock = new TestClock(PurchaseStart);
        var seeded = await SeedPlanAsync(db, clock, PurchaseStart.AddDays(5));
        var client = new ScriptedClient(
            [Snapshot(BaselineExpiry, "command-etag"), Snapshot(BaselineExpiry, "unexpected-etag")],
            new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.PreconditionFailed));
        var service = CreateDeferralService(db, clock, client);

        var first = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);
        var second = await service.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayTrialDeferralResultCode.AmbiguousTerminal, first.Code);
        Assert.Equal(GooglePlayTrialDeferralResultCode.AmbiguousTerminal, second.Code);
        Assert.Single(client.DeferCalls);
        Assert.Equal(BaselineExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(GooglePlayTrialDeferralStatuses.AmbiguousTerminal, Assert.Single(db.GooglePlayInitialPremiumDeferrals).Status);
    }

    [Fact]
    public async Task ConcurrentProcessingIssuesAtMostOneStoredMutation()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var root = new InMemoryDatabaseRoot();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var clock = new TestClock(PurchaseStart);
        SeededPlan seeded;
        await using (var seedDb = new AppDbContext(options)) seeded = await SeedPlanAsync(seedDb, clock, PurchaseStart.AddDays(5));
        var target = BaselineExpiry.AddDays(5);
        var client = new CoordinatedClient(Snapshot(BaselineExpiry, "command-etag"), Snapshot(target, "target-etag"), target);
        await using var firstDb = new AppDbContext(options);
        await using var secondDb = new AppDbContext(options);
        var first = CreateDeferralService(firstDb, clock, client);
        var second = CreateDeferralService(secondDb, clock, client);

        var results = await Task.WhenAll(
            first.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken),
            second.ProcessAsync(seeded.UserId, seeded.Token, seeded.ProtectedToken, TestContext.Current.CancellationToken));

        Assert.Single(client.DeferCalls);
        Assert.Contains(results, result => result.Code == GooglePlayTrialDeferralResultCode.Completed);
        await using var verifyDb = new AppDbContext(options);
        Assert.Equal(GooglePlayTrialDeferralStatuses.Completed, Assert.Single(verifyDb.GooglePlayInitialPremiumDeferrals).Status);
    }

    private static GooglePlaySubscriptionV2Snapshot Snapshot(
        DateTimeOffset expiry,
        string etag,
        bool isTestPurchase = false) => new(
        "SUBSCRIPTION_STATE_ACTIVE",
        PurchaseStart,
        [EligibleLineItem(expiry)],
        GooglePlayPurchaseAcknowledgementState.Acknowledged,
        isTestPurchase)
    {
        Etag = etag
    };

    private static GooglePlaySubscriptionLineItemSnapshot EligibleLineItem(DateTimeOffset expiry) => new(
        SubscriptionConstants.Billing.GooglePlayPremiumProductId,
        expiry)
    {
        HasAutoRenewingPlan = true,
        AutoRenewEnabled = true,
        BasePlanId = "monthly",
        OfferPhase = GooglePlaySubscriptionOfferPhase.BasePrice,
        HasLatestSuccessfulOrderId = true
    };

    private static GooglePlayVerifiedPurchasePersistenceRequest Request(
        Guid userId,
        string token,
        DateTimeOffset? expiresAtUtc = null,
        bool isLicenseTestPurchase = false) => new(
        userId,
        token,
        new GooglePlayVerifiedPurchase(
            "com.languagevoicetutor.mobile",
            SubscriptionConstants.Billing.GooglePlayPremiumProductId,
            PurchaseStart,
            expiresAtUtc ?? BaselineExpiry,
            GooglePlayPurchaseAcknowledgementState.Acknowledged,
            isLicenseTestPurchase)
        {
            InitialPremiumDeferralEvidence = new GooglePlayInitialPremiumDeferralEvidence("initial-etag", isLicenseTestPurchase)
        },
        "protected-" + token);

    private static async Task<SeededPlan> SeedPlanAsync(AppDbContext db, TestClock clock, DateTimeOffset trialExpiry)
    {
        var userId = await AddUserAsync(db);
        await AddTrialAsync(db, userId, trialExpiry);
        const string token = "token-one";
        const string protectedToken = "protected-token-one";
        var request = Request(userId, token) with { ProtectedPurchaseToken = protectedToken };
        var result = await CreatePersistence(db, clock).PersistAsync(request, TestContext.Current.CancellationToken);
        Assert.True(result.Code is GooglePlayVerifiedPurchasePersistenceResultCode.Applied or GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent);
        return new SeededPlan(userId, token, protectedToken);
    }

    private static GooglePlayVerifiedPurchasePersistenceService CreatePersistence(AppDbContext db, TestClock? clock = null)
    {
        var actualClock = clock ?? new TestClock(PurchaseStart);
        var fingerprint = new GooglePlayPurchaseTokenFingerprintService();
        return new GooglePlayVerifiedPurchasePersistenceService(
            db,
            new GooglePlayPurchaseClaimService(db, fingerprint, actualClock),
            new GooglePlayPurchaseTokenSecretPersistenceService(db, actualClock),
            fingerprint,
            actualClock,
            NullLogger<GooglePlayVerifiedPurchasePersistenceService>.Instance);
    }

    private static GooglePlayTrialDeferralService CreateDeferralService(
        AppDbContext db,
        TestClock clock,
        IGooglePlaySubscriptionsV2Client client,
        ILogger<GooglePlayTrialDeferralService>? logger = null,
        GooglePlayReconciliationOptions? options = null)
    {
        var fingerprint = new GooglePlayPurchaseTokenFingerprintService();
        var secrets = new GooglePlayPurchaseTokenSecretPersistenceService(db, clock);
        return new GooglePlayTrialDeferralService(
            db,
            client,
            CreatePersistence(db, clock),
            secrets,
            fingerprint,
            clock,
            Microsoft.Extensions.Options.Options.Create(options ?? new GooglePlayReconciliationOptions()),
            logger ?? NullLogger<GooglePlayTrialDeferralService>.Instance);
    }

    private static async Task<Guid> AddUserAsync(AppDbContext db)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new UserEntity { Id = id, Email = $"{id:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = PurchaseStart });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private static async Task<TrialGrantEntity> AddTrialAsync(AppDbContext db, Guid userId, DateTimeOffset expiry)
    {
        var trial = new TrialGrantEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GrantedAtUtc = PurchaseStart.AddDays(-1),
            ExpiresAtUtc = expiry,
            SourcePlatform = "test",
            Status = SubscriptionConstants.Entitlements.StatusActive,
            CreatedAt = PurchaseStart.AddDays(-1)
        };
        db.TrialGrants.Add(trial);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return trial;
    }

    private static async Task AddEntitlementAsync(
        AppDbContext db,
        Guid userId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset expiresAtUtc,
        string source)
    {
        db.Entitlements.Add(new EntitlementEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
            Source = source,
            Status = SubscriptionConstants.Entitlements.StatusActive,
            StartsAtUtc = startsAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAt = PurchaseStart.AddDays(-1),
            UpdatedAt = PurchaseStart.AddDays(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private sealed record SeededPlan(Guid UserId, string Token, string ProtectedToken);
    private sealed class TestClock(DateTimeOffset now) : IUtcClock { public DateTimeOffset UtcNow { get; set; } = now; }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }

    private sealed class ScriptedClient(IEnumerable<object> getSteps, object deferStep) : IGooglePlaySubscriptionsV2Client
    {
        public Queue<object> GetSteps { get; } = new(getSteps);
        public List<(string PackageName, string Token, string Etag, TimeSpan Duration)> DeferCalls { get; } = [];
        public object DeferStep { get; set; } = deferStep;

        public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken)
        {
            var step = GetSteps.Dequeue();
            if (step is Exception exception) return Task.FromException<GooglePlaySubscriptionV2Snapshot?>(exception);
            return Task.FromResult<GooglePlaySubscriptionV2Snapshot?>((GooglePlaySubscriptionV2Snapshot)step);
        }

        public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<GooglePlaySubscriptionDeferResponseSnapshot> DeferAsync(string packageName, string purchaseToken, string etag, TimeSpan duration, CancellationToken cancellationToken)
        {
            DeferCalls.Add((packageName, purchaseToken, etag, duration));
            if (DeferStep is Exception exception) return Task.FromException<GooglePlaySubscriptionDeferResponseSnapshot>(exception);
            return Task.FromResult((GooglePlaySubscriptionDeferResponseSnapshot)DeferStep);
        }
    }

    private sealed class CoordinatedClient(
        GooglePlaySubscriptionV2Snapshot baseline,
        GooglePlaySubscriptionV2Snapshot target,
        DateTimeOffset targetExpiry) : IGooglePlaySubscriptionsV2Client
    {
        private readonly TaskCompletionSource firstTwoReads = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int getCalls;
        public List<(string PackageName, string Token, string Etag, TimeSpan Duration)> DeferCalls { get; } = [];

        public async Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref getCalls);
            if (call <= 2)
            {
                if (call == 2) firstTwoReads.TrySetResult();
                await firstTwoReads.Task.WaitAsync(cancellationToken);
                return baseline;
            }
            return target;
        }

        public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<GooglePlaySubscriptionDeferResponseSnapshot> DeferAsync(string packageName, string purchaseToken, string etag, TimeSpan duration, CancellationToken cancellationToken)
        {
            lock (DeferCalls) DeferCalls.Add((packageName, purchaseToken, etag, duration));
            return Task.FromResult(new GooglePlaySubscriptionDeferResponseSnapshot([new(SubscriptionConstants.Billing.GooglePlayPremiumProductId, targetExpiry)]));
        }
    }
}
