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
        await dbContext.SaveChangesAsync();

        var response = await new AdminActivityService(dbContext).ListActivityAsync(new AdminActivityQuery(null, null, null, null, null, null, null, null, null, 10), CancellationToken.None);

        Assert.Equal(new[] { "cms_content_audit_logs", "admin_role_assignment_events", "admin_actions" }, response.Items.Select(item => item.Source));
    }

    [Fact]
    public async Task LimitValidationRejectsTooLargeLimit()
    {
        await using var dbContext = CreateDbContext();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new AdminActivityService(dbContext).ListActivityAsync(new AdminActivityQuery(null, null, null, null, null, null, null, null, null, 201), CancellationToken.None));
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
        await dbContext.SaveChangesAsync();
        return id;
    }
}
