using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class PaddleAdjustmentReprocessServiceTests
{
    [Fact]
    public async Task ExistingAdjustmentUpdatedPreviouslyBlockedCanBeReprocessedAndRevokesActiveProviderEventPremium()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedFullRefundAsync(dbContext, SubscriptionConstants.BillingEventStatuses.ReconciliationBlocked);
        var paymentCount = await dbContext.Payments.CountAsync(TestContext.Current.CancellationToken);
        var subscriptionCount = await dbContext.Subscriptions.CountAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(dbContext).ReprocessProviderEventAsync(fixture.ProviderEventId, TestContext.Current.CancellationToken);

        Assert.Equal(PaddleAdjustmentReprocessResults.Revoked, result.Result);
        Assert.Equal(1, result.RevokedCount);
        Assert.Equal(paymentCount, await dbContext.Payments.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(subscriptionCount, await dbContext.Subscriptions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await dbContext.BillingEvents.CountAsync(e => e.ProviderEventId == fixture.ProviderEventId, TestContext.Current.CancellationToken));
        var entitlement = await dbContext.Entitlements.SingleAsync(e => e.Id == fixture.EntitlementId, TestContext.Current.CancellationToken);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusExpired, entitlement.Status);
    }

    [Fact]
    public async Task DuplicateReplayRecoveryOfFullRefundAdjustmentRevokesPremium()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedFullRefundAsync(dbContext, SubscriptionConstants.BillingEventStatuses.Processed);

        var result = await CreateService(dbContext).ReprocessProviderEventAsync(fixture.ProviderEventId, TestContext.Current.CancellationToken);

        Assert.Equal(PaddleAdjustmentReprocessResults.Revoked, result.Result);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusExpired, (await dbContext.Entitlements.SingleAsync(e => e.Id == fixture.EntitlementId, TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task TransactionCompletedIsRefusedAndDoesNotCreateDuplicatePremium()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedFullRefundAsync(dbContext, SubscriptionConstants.BillingEventStatuses.Processed, SubscriptionConstants.BillingEventTypes.TransactionCompleted);
        var entitlementCount = await dbContext.Entitlements.CountAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(dbContext).ReprocessProviderEventAsync(fixture.ProviderEventId, TestContext.Current.CancellationToken);

        Assert.Equal(PaddleAdjustmentReprocessResults.RefusedEventType, result.Result);
        Assert.Equal(entitlementCount, await dbContext.Entitlements.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PartialRefundReprocessDoesNotRevokePremium()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedFullRefundAsync(dbContext, SubscriptionConstants.BillingEventStatuses.Processed, adjustmentType: "partial", adjustmentAmountMinor: 100);

        var result = await CreateService(dbContext).ReprocessProviderEventAsync(fixture.ProviderEventId, TestContext.Current.CancellationToken);

        Assert.Equal(PaddleAdjustmentReprocessResults.PartialRefundSkipped, result.Result);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, (await dbContext.Entitlements.SingleAsync(e => e.Id == fixture.EntitlementId, TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task ChargebackReprocessRevokesPremium()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedFullRefundAsync(dbContext, SubscriptionConstants.BillingEventStatuses.Processed, action: "chargeback");

        var result = await CreateService(dbContext).ReprocessProviderEventAsync(fixture.ProviderEventId, TestContext.Current.CancellationToken);

        Assert.Equal(PaddleAdjustmentReprocessResults.Revoked, result.Result);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusExpired, (await dbContext.Entitlements.SingleAsync(e => e.Id == fixture.EntitlementId, TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task AlreadyRevokedEntitlementIsIdempotent()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedFullRefundAsync(dbContext, SubscriptionConstants.BillingEventStatuses.Processed);
        dbContext.Entitlements.Single(e => e.Id == fixture.EntitlementId).Status = SubscriptionConstants.Entitlements.StatusExpired;
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(dbContext).ReprocessProviderEventAsync(fixture.ProviderEventId, TestContext.Current.CancellationToken);

        Assert.Equal(PaddleAdjustmentReprocessResults.AlreadyRevoked, result.Result);
        Assert.Equal(0, result.RevokedCount);
    }

    [Fact]
    public async Task UnknownProviderEventIdReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var result = await CreateService(dbContext).ReprocessProviderEventAsync("evt_missing", TestContext.Current.CancellationToken);
        Assert.Equal(PaddleAdjustmentReprocessResults.NotFound, result.Result);
    }

    [Fact]
    public async Task SafeResultMetadataDoesNotExposeRawPayloadsOrSecrets()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedFullRefundAsync(dbContext, SubscriptionConstants.BillingEventStatuses.Processed);
        dbContext.BillingEvents.Single(e => e.ProviderEventId == fixture.ProviderEventId).SafeMetadataJson = CreateMetadata(fixture.UserId, "refund", "approved", "full", 1000, 1000, includeForbidden: true);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(dbContext).ReprocessProviderEventAsync(fixture.ProviderEventId, TestContext.Current.CancellationToken);
        var json = JsonSerializer.Serialize(result);

        foreach (var forbidden in new[] { "rawPayload", "signature", "token", "cookie", "secret", "apiKey", "card", "411111" })
        {
            Assert.DoesNotContain(forbidden, json, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static IPaddleAdjustmentReprocessService CreateService(AppDbContext dbContext)
    {
        var options = Options.Create(new PaddleBillingOptions
        {
            PremiumPriceId = "pri_test",
            PremiumProductId = "pro_test",
            ExpectedCustomDataApp = "language_voice_tutor",
            ExpectedCustomDataProduct = "language_voice_tutor_pro"
        });
        var reconciliation = new BillingEventReconciliationDecisionService(dbContext, NullLogger<BillingEventReconciliationDecisionService>.Instance, options);
        var activation = new BillingEventEntitlementActivationService(dbContext, NullLogger<BillingEventEntitlementActivationService>.Instance, options);
        return new PaddleAdjustmentReprocessService(dbContext, reconciliation, activation, NullLogger<PaddleAdjustmentReprocessService>.Instance);
    }

    private static async Task<Fixture> SeedFullRefundAsync(AppDbContext dbContext, string eventStatus, string eventType = SubscriptionConstants.BillingEventTypes.AdjustmentUpdated, string action = "refund", string adjustmentType = "full", long adjustmentAmountMinor = 1000)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var entitlementId = Guid.NewGuid();
        var providerEventId = "evt_" + Guid.NewGuid().ToString("N");
        dbContext.Users.Add(new UserEntity { Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = now });
        dbContext.Subscriptions.Add(new SubscriptionEntity { Id = subscriptionId, UserId = userId, PlanId = SubscriptionConstants.Plans.PremiumPlanId, Status = "active", Provider = SubscriptionConstants.BillingProviders.Paddle, ProviderSubscriptionId = "sub_test", ProviderPriceId = "pri_test", ProviderProductId = "pro_test", StartedAt = now.AddDays(-1), CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) });
        dbContext.Payments.Add(new PaymentEntity { Id = Guid.NewGuid(), UserId = userId, SubscriptionId = subscriptionId, InternalPlanId = SubscriptionConstants.Plans.PremiumPlanId, Amount = 10, AmountMinor = 1000, Currency = "USD", Status = SubscriptionConstants.PaymentStatuses.Completed, Provider = SubscriptionConstants.BillingProviders.Paddle, ProviderPaymentId = "txn_test", ProviderSubscriptionId = "sub_test", ProviderPriceId = "pri_test", ProviderProductId = "pro_test", CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) });
        dbContext.Entitlements.Add(new EntitlementEntity { Id = entitlementId, UserId = userId, SubscriptionId = subscriptionId, PlanId = SubscriptionConstants.Plans.PremiumPlanId, EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType, Source = SubscriptionConstants.Entitlements.SourceProviderEvent, Status = SubscriptionConstants.Entitlements.StatusActive, StartsAtUtc = now.AddDays(-1), ExpiresAtUtc = now.AddDays(30), CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) });
        dbContext.BillingEvents.Add(new BillingEventEntity { Id = Guid.NewGuid(), BillingProvider = SubscriptionConstants.BillingProviders.Paddle, EventType = eventType, ProviderEventId = providerEventId, ReceivedAtUtc = now.AddMinutes(-10), ProcessedAtUtc = now.AddMinutes(-5), Status = eventStatus, ErrorMessage = "old state", SafeMetadataJson = CreateMetadata(userId, action, "approved", adjustmentType, adjustmentAmountMinor, 1000) });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new Fixture(userId, entitlementId, providerEventId);
    }

    private static string CreateMetadata(Guid userId, string action, string status, string type, long adjustmentAmountMinor, long amountMinor, bool includeForbidden = false) => JsonSerializer.Serialize(new Dictionary<string, object?>
    {
        ["internalUserId"] = userId.ToString(),
        ["internalPlanId"] = SubscriptionConstants.Plans.PremiumPlanId,
        ["paddlePriceId"] = "pri_test",
        ["paddleProductId"] = "pro_test",
        ["customDataApp"] = "language_voice_tutor",
        ["customDataProduct"] = "language_voice_tutor_pro",
        ["paddleTransactionId"] = "txn_test",
        ["paddleSubscriptionId"] = "sub_test",
        ["adjustmentAction"] = action,
        ["adjustmentStatus"] = status,
        ["adjustmentType"] = type,
        ["adjustmentAmountMinor"] = adjustmentAmountMinor,
        ["amountMinor"] = amountMinor,
        ["rawPayload"] = includeForbidden ? "token secret cookie signature apiKey card 411111" : null
    });

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
    private sealed record Fixture(Guid UserId, Guid EntitlementId, string ProviderEventId);
}
