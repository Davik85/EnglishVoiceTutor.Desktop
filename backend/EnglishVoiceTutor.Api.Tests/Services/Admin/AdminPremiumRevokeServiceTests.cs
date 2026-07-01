using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishVoiceTutor.Api.Tests.Services.Admin;

public sealed class AdminPremiumRevokeServiceTests
{
    [Fact]
    public void SuperAdminPermissionCatalogIncludesPremiumRevokeButSupportAndBillingSupportDoNot()
    {
        var permissions = new AdminRolePermissionCatalogService().GetProductionRolePermissions();

        Assert.Contains(AdminPermissionConstants.PremiumRevoke, permissions[AdminRoleConstants.SuperAdmin]);
        Assert.DoesNotContain(AdminPermissionConstants.PremiumRevoke, permissions[AdminRoleConstants.Support]);
        Assert.DoesNotContain(AdminPermissionConstants.PremiumRevoke, permissions[AdminRoleConstants.BillingSupport]);
    }

    [Fact]
    public async Task RevokePremiumRevokesProviderBackedActivePremiumWithoutDeletingPaymentsAndWritesAudit()
    {
        await using var dbContext = CreateDbContext();
        var adminUserId = await AddUserAsync(dbContext, "admin@example.test");
        var targetUserId = await AddUserAsync(dbContext, "paid@example.test");
        var entitlementId = Guid.NewGuid();
        dbContext.Entitlements.Add(new EntitlementEntity
        {
            Id = entitlementId,
            UserId = targetUserId,
            PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
            Source = SubscriptionConstants.Entitlements.SourceProviderEvent,
            Status = SubscriptionConstants.Entitlements.StatusActive,
            StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        });
        var paymentId = Guid.NewGuid();
        dbContext.Payments.Add(new PaymentEntity
        {
            Id = paymentId,
            UserId = targetUserId,
            InternalPlanId = SubscriptionConstants.Plans.PremiumPlanId,
            Amount = 10,
            Currency = "USD",
            Status = "completed",
            Provider = SubscriptionConstants.BillingProviders.Paddle,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new AdminPremiumRevokeService(dbContext, new AdminAuditService(dbContext)).RevokePremiumAsync(
            adminUserId,
            targetUserId,
            entitlementId,
            new AdminManualPremiumRevokeRequest { Reason = "chargeback emergency" },
            TestContext.Current.CancellationToken);

        Assert.NotNull(result.Response);
        var entitlement = await dbContext.Entitlements.SingleAsync(item => item.Id == entitlementId, TestContext.Current.CancellationToken);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusRevoked, entitlement.Status);
        Assert.Equal(1, await dbContext.Payments.CountAsync(TestContext.Current.CancellationToken));
        Assert.True(await dbContext.Payments.AnyAsync(payment => payment.Id == paymentId, TestContext.Current.CancellationToken));
        var audit = await dbContext.AdminActions.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AdminAuditConstants.ActionTypes.ManualPremiumRevoke, audit.ActionType);
        Assert.Equal(adminUserId, audit.AdminUserId);
        Assert.Equal(targetUserId, audit.TargetUserId);
        Assert.Equal("chargeback emergency", audit.Reason);
        Assert.Contains("\"accessControlOnly\":true", audit.SafeMetadataJson);
    }

    [Fact]
    public async Task RevokePremiumRequiresReason()
    {
        await using var dbContext = CreateDbContext();
        var result = await new AdminPremiumRevokeService(dbContext, new AdminAuditService(dbContext)).RevokePremiumAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new AdminManualPremiumRevokeRequest { Reason = " " },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsInvalid);
        Assert.Equal(nameof(AdminPremiumRevokeConstants.ReasonRequiredError), result.ErrorCode);
    }

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private static async Task<Guid> AddUserAsync(AppDbContext dbContext, string email)
    {
        var id = Guid.NewGuid();
        dbContext.Users.Add(new UserEntity { Id = id, Email = email, PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }
}
