using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Admin;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services.Admin;

public sealed class AdminProductStatisticsServiceTests
{
    [Fact]
    public async Task PremiumEntitlementsCountAsPremiumUsersButNotTrackedDevices()
    {
        await using var dbContext = CreateDbContext();
        var premiumUserId = await AddUserAsync(dbContext, "premium@example.test");
        await AddPremiumEntitlementAsync(dbContext, premiumUserId);

        var overview = await new AdminProductStatisticsService(dbContext).GetOverviewAsync(CancellationToken.None);

        Assert.Equal(1, overview.ActivePremiumUsersNow);
        Assert.Equal(0, overview.TotalInstallations);
    }

    [Fact]
    public async Task TrialGrantsCountAsActiveTrialsButNotTrackedDevices()
    {
        await using var dbContext = CreateDbContext();
        var trialUserId = await AddUserAsync(dbContext, "trial@example.test");
        await AddActiveTrialGrantAsync(dbContext, trialUserId);

        var overview = await new AdminProductStatisticsService(dbContext).GetOverviewAsync(CancellationToken.None);

        Assert.Equal(1, overview.ActiveTrialsNow);
        Assert.Equal(0, overview.TotalInstallations);
    }

    [Fact]
    public async Task TrackedDeviceCountComesOnlyFromDeviceRows()
    {
        await using var dbContext = CreateDbContext();
        var deviceUserId = await AddUserAsync(dbContext, "device@example.test");
        var premiumOnlyUserId = await AddUserAsync(dbContext, "premium-only@example.test");
        var trialOnlyUserId = await AddUserAsync(dbContext, "trial-only@example.test");
        await AddDeviceAsync(dbContext, deviceUserId);
        await AddPremiumEntitlementAsync(dbContext, premiumOnlyUserId);
        await AddActiveTrialGrantAsync(dbContext, trialOnlyUserId);

        var overview = await new AdminProductStatisticsService(dbContext).GetOverviewAsync(CancellationToken.None);

        Assert.Equal(1, overview.TotalInstallations);
        Assert.Equal(1, overview.ActivePremiumUsersNow);
        Assert.Equal(1, overview.ActiveTrialsNow);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<Guid> AddUserAsync(AppDbContext dbContext, string email)
    {
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "test-hash",
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private static async Task AddPremiumEntitlementAsync(AppDbContext dbContext, Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        dbContext.Entitlements.Add(new EntitlementEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
            Source = SubscriptionConstants.Entitlements.SourceProviderEvent,
            Status = SubscriptionConstants.Entitlements.StatusActive,
            StartsAtUtc = now.AddDays(-1),
            ExpiresAtUtc = now.AddDays(30),
            CreatedAt = now,
            UpdatedAt = now
        });
        await dbContext.SaveChangesAsync();
    }

    private static async Task AddActiveTrialGrantAsync(AppDbContext dbContext, Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        dbContext.TrialGrants.Add(new TrialGrantEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GrantedAtUtc = now.AddDays(-1),
            ExpiresAtUtc = now.AddDays(7),
            SourcePlatform = "desktop",
            Status = SubscriptionConstants.Entitlements.StatusActive,
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync();
    }

    private static async Task AddDeviceAsync(AppDbContext dbContext, Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        dbContext.Devices.Add(new DeviceEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Platform = "Windows",
            DeviceName = "Desktop",
            AppVersion = "test",
            CreatedAt = now,
            LastSeenAt = now
        });
        await dbContext.SaveChangesAsync();
    }
}
