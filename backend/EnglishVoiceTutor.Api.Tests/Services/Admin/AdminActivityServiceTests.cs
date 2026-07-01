using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Data.Entities.Cms;
using EnglishVoiceTutor.Api.Services.Admin;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services.Admin;

public sealed class AdminActivityServiceTests
{
    [Fact]
    public async Task ListActivityIncludesAllExistingAuditSourcesNewestFirst()
    {
        await using var dbContext = CreateDbContext();
        var actor = await AddUserAsync(dbContext, "actor@example.test");
        var target = await AddUserAsync(dbContext, "target@example.test");
        var adminUserId = Guid.NewGuid();
        dbContext.AdminUsers.Add(new AdminUserEntity { Id = adminUserId, UserId = actor, NormalizedEmail = "actor@example.test", Status = "active", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow });
        dbContext.AdminActions.Add(new AdminActionEntity { Id = Guid.NewGuid(), AdminUserId = actor, TargetUserId = target, ActionType = "manual_premium_grant", Reason = "safe", CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-3), SafeMetadataJson = "{\"entitlementId\":\"safe\"}" });
        dbContext.AdminRoleAssignmentEvents.Add(new AdminRoleAssignmentEventEntity { Id = Guid.NewGuid(), ActorAdminUserId = adminUserId, TargetAdminUserId = adminUserId, ActionType = "assign", Result = "succeeded", OccurredAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2), SafeMetadataJson = "{\"roleId\":\"support\"}" });
        dbContext.ContentAuditLogs.Add(new ContentAuditLogEntity { Id = Guid.NewGuid(), ActorUserId = actor, ActorEmail = "actor@example.test", Action = "DraftSaved", EntityType = "Topic", EntityId = Guid.NewGuid(), ChangedFieldsJson = "[]", Reason = "safe", Source = "AdminCms", Status = "succeeded", CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1), RequestMetadataJson = "{\"source\":\"AdminCms\"}" });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = await new AdminActivityService(dbContext).ListActivityAsync(new AdminActivityQuery(null, null, null, null, null, null, null, null, null, 10), TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "cms_content_audit_logs", "admin_role_assignment_events", "admin_actions" }, response.Items.Select(item => item.Source));
    }

    [Fact]
    public async Task ListActivityMapsAdminActionReasonIntoAdminNote()
    {
        await using var dbContext = CreateDbContext();
        var actor = await AddUserAsync(dbContext, "actor-action@example.test");
        var target = await AddUserAsync(dbContext, "target-action@example.test");
        dbContext.AdminActions.Add(new AdminActionEntity { Id = Guid.NewGuid(), AdminUserId = actor, TargetUserId = target, ActionType = "manual_premium_grant", Reason = "Manual grant for tester cohort", CreatedAtUtc = DateTimeOffset.UtcNow, SafeMetadataJson = "{\"entitlementId\":\"safe\"}" });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = await new AdminActivityService(dbContext).ListActivityAsync(new AdminActivityQuery(null, null, null, null, "admin_actions", null, null, null, null, 10), TestContext.Current.CancellationToken);

        var item = Assert.Single(response.Items);
        Assert.Equal("Manual grant for tester cohort", item.AdminNote);
        Assert.Equal("Manual grant for tester cohort", item.Reason);
        Assert.Equal("{\"entitlementId\":\"safe\"}", item.SafeMetadataJson);
    }

    [Fact]
    public async Task ListActivityMapsRoleAssignmentReasonIntoAdminNote()
    {
        await using var dbContext = CreateDbContext();
        var actorUserId = await AddUserAsync(dbContext, "actor-role@example.test");
        var targetUserId = await AddUserAsync(dbContext, "target-role@example.test");
        var actorAdminUserId = Guid.NewGuid();
        var targetAdminUserId = Guid.NewGuid();
        dbContext.AdminUsers.AddRange(
            new AdminUserEntity { Id = actorAdminUserId, UserId = actorUserId, NormalizedEmail = "actor-role@example.test", Status = "active", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow },
            new AdminUserEntity { Id = targetAdminUserId, UserId = targetUserId, NormalizedEmail = "target-role@example.test", Status = "active", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow });
        dbContext.AdminRoleAssignmentEvents.Add(new AdminRoleAssignmentEventEntity { Id = Guid.NewGuid(), ActorAdminUserId = actorAdminUserId, TargetAdminUserId = targetAdminUserId, ActionType = "disable_admin", Result = "succeeded", Reason = "Disable requested by owner", OccurredAtUtc = DateTimeOffset.UtcNow, SafeMetadataJson = "{\"roleCount\":1}" });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = await new AdminActivityService(dbContext).ListActivityAsync(new AdminActivityQuery(null, null, null, null, "admin_role_assignment_events", null, null, null, null, 10), TestContext.Current.CancellationToken);

        var item = Assert.Single(response.Items);
        Assert.Equal("Disable requested by owner", item.AdminNote);
        Assert.Equal("{\"roleCount\":1}", item.SafeMetadataJson);
    }

    [Fact]
    public async Task ListActivityResolvesAdminActionActorAdminUserAndFiltersByBothActorIds()
    {
        await using var dbContext = CreateDbContext();
        var actor = await AddUserAsync(dbContext, "mapped-actor@example.test");
        var target = await AddUserAsync(dbContext, "mapped-target@example.test");
        var actorAdminUserId = Guid.NewGuid();
        dbContext.AdminUsers.Add(new AdminUserEntity { Id = actorAdminUserId, UserId = actor, NormalizedEmail = "mapped-actor@example.test", Status = "active", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow });
        dbContext.AdminActions.Add(new AdminActionEntity { Id = Guid.NewGuid(), AdminUserId = actor, TargetUserId = target, ActionType = "manual_premium_revoke", Reason = "Emergency revoke", CreatedAtUtc = DateTimeOffset.UtcNow, SafeMetadataJson = "{\"accessControlOnly\":true}" });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var byAdminUser = await new AdminActivityService(dbContext).ListActivityAsync(new AdminActivityQuery(actorAdminUserId, null, null, null, "admin_actions", null, "manual_premium_revoke", null, null, 10), TestContext.Current.CancellationToken);
        var byAppUser = await new AdminActivityService(dbContext).ListActivityAsync(new AdminActivityQuery(null, actor, null, null, "admin_actions", null, "manual_premium_revoke", null, null, 10), TestContext.Current.CancellationToken);

        var item = Assert.Single(byAdminUser.Items);
        Assert.Equal(actorAdminUserId, item.ActorAdminUserId);
        Assert.Equal(actor, item.ActorUserId);
        Assert.Equal("mapped-actor@example.test", item.ActorEmail);
        Assert.Equal("manual_premium_revoke", item.ActionType);
        Assert.Single(byAppUser.Items);
    }

    [Fact]
    public async Task LimitValidationRejectsTooLargeLimit()
    {
        await using var dbContext = CreateDbContext();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new AdminActivityService(dbContext).ListActivityAsync(new AdminActivityQuery(null, null, null, null, null, null, null, null, null, 201), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void DtoDoesNotExposeForbiddenFields()
    {
        var names = typeof(AdminActivityEventSnapshot).GetProperties().Select(property => property.Name.ToLowerInvariant()).ToArray();
        foreach (var forbidden in new[] { "password", "token", "secret", "cookie", "authorization", "apikey", "api_key", "webhookrawpayload", "providerrawpayload" })
        {
            Assert.DoesNotContain(names, name => name.Contains(forbidden, StringComparison.Ordinal));
        }
    }

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<Guid> AddUserAsync(AppDbContext dbContext, string email)
    {
        var id = Guid.NewGuid();
        dbContext.Users.Add(new UserEntity { Id = id, Email = email, PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }
}
