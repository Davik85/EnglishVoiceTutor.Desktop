using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class AccountAnonymizationPreflightServiceTests
{
    [Fact]
    public async Task ValidDeletionRequestCreatesOnlyFoundationRecordsAndNoPermanentPolicyBlockers()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var beforeUsers = await db.Users.CountAsync(TestContext.Current.CancellationToken);
        var beforeReports = await db.UserFeedbackReports.CountAsync(TestContext.Current.CancellationToken);

        var result = await new AccountAnonymizationPreflightService(db).CreateOrRefreshAsync(fixture.Admin.Id, fixture.Report.Id, false, TestContext.Current.CancellationToken);

        var response = Assert.IsType<EnglishVoiceTutor.Api.Contracts.Admin.AccountAnonymizationPreflightResponse>(result.Response);
        Assert.Equal(AccountAnonymizationPreflightService.PreflightState, response.State);
        Assert.Empty(response.BlockingReasonCodes);
        Assert.Equal(1, await db.AccountAnonymizationOperations.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await db.AccountAnonymizationPolicySnapshots.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(beforeUsers, await db.Users.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(beforeReports, await db.UserFeedbackReports.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal("learner@example.test", (await db.Users.SingleAsync(item => item.Id == fixture.User.Id, TestContext.Current.CancellationToken)).Email);
    }

    [Fact]
    public async Task ReuseRefreshAndExpiryUseOneOperationWithExpectedVersions()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var service = new AccountAnonymizationPreflightService(db);
        var first = (await service.CreateOrRefreshAsync(fixture.Admin.Id, fixture.Report.Id, false, TestContext.Current.CancellationToken)).Response!;
        var reused = (await service.CreateOrRefreshAsync(fixture.Admin.Id, fixture.Report.Id, false, TestContext.Current.CancellationToken)).Response!;
        var refreshed = (await service.CreateOrRefreshAsync(fixture.Admin.Id, fixture.Report.Id, true, TestContext.Current.CancellationToken)).Response!;
        var operation = await db.AccountAnonymizationOperations.SingleAsync(TestContext.Current.CancellationToken);
        operation.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var expired = (await service.CreateOrRefreshAsync(fixture.Admin.Id, fixture.Report.Id, false, TestContext.Current.CancellationToken)).Response!;

        Assert.Equal(first.OperationId, reused.OperationId);
        Assert.Equal(1, reused.PreflightVersion);
        Assert.Equal(2, refreshed.PreflightVersion);
        Assert.Equal(3, expired.PreflightVersion);
        Assert.Equal(1, await db.AccountAnonymizationOperations.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProviderAndLifecycleOutputUsesOnlySafeAllowlistedValues()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db, targetIsAdmin: true);
        const string sensitiveProvider = "customer-secret-provider@example.test";
        const string sensitiveStatus = "payment-token-secret-123";
        db.Subscriptions.AddRange(
            new SubscriptionEntity { Id = Guid.NewGuid(), UserId = fixture.User.Id, PlanId = "plan", Provider = sensitiveProvider, Status = sensitiveStatus, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new SubscriptionEntity { Id = Guid.NewGuid(), UserId = fixture.User.Id, PlanId = "plan", Provider = "PADDLE", Status = "active", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        db.Payments.Add(new PaymentEntity { Id = Guid.NewGuid(), UserId = fixture.User.Id, Provider = sensitiveProvider, Status = sensitiveStatus, Amount = 1, Currency = "USD", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = (await new AccountAnonymizationPreflightService(db).CreateOrRefreshAsync(fixture.Admin.Id, fixture.Report.Id, true, TestContext.Current.CancellationToken)).Response!;

        Assert.Contains(AccountAnonymizationPreflightService.ActiveAdminTarget, response.BlockingReasonCodes);
        Assert.Contains(response.ProviderStates, item => item.ProviderKey == "paddle" && item.StateCodes.Contains("active"));
        Assert.Contains(response.ProviderStates, item => item.ProviderKey == "unsupported" && item.StateCodes.Contains("unknown"));
        var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
        var operation = await db.AccountAnonymizationOperations.SingleAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(sensitiveProvider, responseJson, StringComparison.Ordinal);
        Assert.DoesNotContain(sensitiveStatus, responseJson, StringComparison.Ordinal);
        Assert.DoesNotContain(sensitiveProvider, operation.ProviderStatesJson, StringComparison.Ordinal);
        Assert.DoesNotContain(sensitiveStatus, operation.ProviderStatesJson, StringComparison.Ordinal);
        Assert.DoesNotContain(sensitiveProvider, operation.PreflightFingerprint, StringComparison.Ordinal);
        Assert.DoesNotContain(sensitiveStatus, operation.PreflightFingerprint, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminAndCmsDependenciesAreCountedOnlyForTheTargetUser()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var targetAdmin = new AdminUserEntity { Id = Guid.NewGuid(), UserId = fixture.User.Id, Status = "inactive", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow };
        var otherUser = new UserEntity { Id = Guid.NewGuid(), Email = "other@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        var otherAdmin = new AdminUserEntity { Id = Guid.NewGuid(), UserId = otherUser.Id, Status = "active", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow };
        db.AddRange(targetAdmin, otherUser, otherAdmin);
        db.AddRange(
            new AdminUserRoleEntity { Id = Guid.NewGuid(), AdminUserId = targetAdmin.Id, AssignedByAdminUserId = fixture.Admin.Id, RoleId = "admin", Reason = "test", AssignedAtUtc = DateTimeOffset.UtcNow },
            new AdminRoleAssignmentEventEntity { Id = Guid.NewGuid(), ActorAdminUserId = fixture.Admin.Id, TargetAdminUserId = targetAdmin.Id, ActionType = "grant", Result = "success", OccurredAtUtc = DateTimeOffset.UtcNow },
            new AdminAuthAuditEventEntity { Id = Guid.NewGuid(), ActorAdminUserId = targetAdmin.Id, OccurredAtUtc = DateTimeOffset.UtcNow, EventType = "login", Result = "success" },
            new EnglishVoiceTutor.Api.Data.Entities.Cms.ContentPackEntity { Id = Guid.NewGuid(), Slug = "target", Name = "target", Description = "", Status = "draft", CreatedByUserId = fixture.User.Id, UpdatedByUserId = fixture.User.Id, CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow },
            new EnglishVoiceTutor.Api.Data.Entities.Cms.PromptTemplateEntity { Id = Guid.NewGuid(), ContentPackId = Guid.NewGuid(), TemplateKey = "target", Body = "", AllowedPlaceholdersJson = "[]", RequiredPlaceholdersJson = "[]", UpdatedByUserId = fixture.User.Id, UpdatedAtUtc = DateTimeOffset.UtcNow, CreatedAtUtc = DateTimeOffset.UtcNow },
            new EnglishVoiceTutor.Api.Data.Entities.Cms.ContentVersionEntity { Id = Guid.NewGuid(), ContentPackId = Guid.NewGuid(), VersionNumber = 1, SnapshotHash = "hash", PublishStatus = "published", PublishedByUserId = fixture.User.Id, ValidationSummaryJson = "{}", ChangeSummary = "" },
            new EnglishVoiceTutor.Api.Data.Entities.Cms.ContentAuditLogEntity { Id = Guid.NewGuid(), ActorUserId = fixture.User.Id, Action = "update", EntityType = "pack", EntityId = Guid.NewGuid(), ChangedFieldsJson = "[]", Reason = "test", Source = "test", Status = "success", CreatedAtUtc = DateTimeOffset.UtcNow },
            new EnglishVoiceTutor.Api.Data.Entities.Cms.ContentPackEntity { Id = Guid.NewGuid(), Slug = "other", Name = "other", Description = "", Status = "draft", CreatedByUserId = otherUser.Id, CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow },
            new AdminUserRoleEntity { Id = Guid.NewGuid(), AdminUserId = otherAdmin.Id, AssignedByAdminUserId = fixture.Admin.Id, RoleId = "admin", Reason = "test", AssignedAtUtc = DateTimeOffset.UtcNow },
            new SubscriptionEntity { Id = Guid.NewGuid(), UserId = otherUser.Id, PlanId = "plan", Provider = "paddle", Status = "active", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new PaymentEntity { Id = Guid.NewGuid(), UserId = otherUser.Id, InternalPlanId = "plan", Provider = "paddle", Status = "completed", Amount = 1, Currency = "USD", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new UserFeedbackReportEntity { Id = Guid.NewGuid(), UserId = otherUser.Id, Category = "general", Message = "other learner support data", Status = UserFeedbackReportConstants.NewStatus, ClientPlatform = "test", ClientVersion = "v1", CreatedAtUtc = DateTimeOffset.UtcNow },
            new LessonSessionEntity { Id = Guid.NewGuid(), UserId = otherUser.Id, LessonContentId = "lesson", StudyLanguage = "en", TopicId = "topic", TopicTitle = "topic", SubtopicId = "subtopic", SubtopicTitle = "subtopic", Level = "a1", ModeUsed = "test", Status = "completed", StartedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = (await new AccountAnonymizationPreflightService(db).CreateOrRefreshAsync(fixture.Admin.Id, fixture.Report.Id, true, TestContext.Current.CancellationToken)).Response!;

        Assert.Equal(1, response.CategoryCounts["admin_user_mappings"]);
        Assert.Equal(1, response.CategoryCounts["admin_user_roles"]);
        Assert.Equal(1, response.CategoryCounts["admin_role_assignment_events"]);
        Assert.Equal(1, response.CategoryCounts["admin_auth_audit"]);
        Assert.Equal(1, response.CategoryCounts["cms_content_pack_authorship"]);
        Assert.Equal(1, response.CategoryCounts["cms_prompt_template_authorship"]);
        Assert.Equal(1, response.CategoryCounts["cms_content_version_authorship"]);
        Assert.Equal(1, response.CategoryCounts["cms_audit_logs"]);
        Assert.Equal(0, response.CategoryCounts["subscriptions"]);
        Assert.Equal(0, response.CategoryCounts["payments"]);
        Assert.Equal(0, response.CategoryCounts["lesson_sessions"]);
        Assert.Equal(1, response.CategoryCounts["feedback_reports"]);
        Assert.Contains(AccountAnonymizationPreflightService.AdminCmsDependencyUnclassified, response.BlockingReasonCodes);
    }

    [Fact]
    public async Task UnexpectedPersistenceFailureReturnsUnavailableInsteadOfStaleSuccess()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using (var seedDb = CreateDbContext(databaseName)) { await SeedAsync(seedDb); }
        await using var failingDb = CreateDbContext(databaseName, new ThrowingSaveChangesInterceptor());
        var report = await failingDb.UserFeedbackReports.SingleAsync(TestContext.Current.CancellationToken);
        var admin = await failingDb.AdminUsers.SingleAsync(TestContext.Current.CancellationToken);

        var result = await new AccountAnonymizationPreflightService(failingDb).CreateOrRefreshAsync(admin.Id, report.Id, false, TestContext.Current.CancellationToken);

        Assert.True(result.IsUnavailable);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task SelfTargetIsReportedAsASafeBlocker()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        fixture.Admin.UserId = fixture.User.Id;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = (await new AccountAnonymizationPreflightService(db).CreateOrRefreshAsync(fixture.Admin.Id, fixture.Report.Id, false, TestContext.Current.CancellationToken)).Response!;

        Assert.Contains(AccountAnonymizationPreflightService.SelfTarget, response.BlockingReasonCodes);
    }

    [Fact]
    public async Task StatusReadDoesNotCreateOrRefreshPreflight()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var service = new AccountAnonymizationPreflightService(db);
        var missing = await service.GetStatusAsync(fixture.Report.Id, TestContext.Current.CancellationToken);
        Assert.True(missing.IsNoOperation);
        await service.CreateOrRefreshAsync(fixture.Admin.Id, fixture.Report.Id, false, TestContext.Current.CancellationToken);
        var operation = await db.AccountAnonymizationOperations.SingleAsync(TestContext.Current.CancellationToken);
        var version = operation.PreflightVersion;
        var status = await service.GetStatusAsync(fixture.Report.Id, TestContext.Current.CancellationToken);
        Assert.Equal(version, status.Response?.PreflightVersion);
        Assert.Equal(1, await db.AccountAnonymizationOperations.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MissingWrongCategoryAndTerminalRequestsDoNotCreatePreflight()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedAsync(db);
        var service = new AccountAnonymizationPreflightService(db);
        var wrongCategory = new UserFeedbackReportEntity { Id = Guid.NewGuid(), UserId = fixture.User.Id, Category = "general", Message = "test", Status = UserFeedbackReportConstants.NewStatus, ClientPlatform = "test", ClientVersion = "v1", CreatedAtUtc = DateTimeOffset.UtcNow };
        var terminal = new UserFeedbackReportEntity { Id = Guid.NewGuid(), UserId = fixture.User.Id, Category = UserFeedbackReportConstants.AccountDeletionCategory, Message = "test", Status = UserFeedbackReportConstants.ResolvedStatus, ClientPlatform = "test", ClientVersion = "v1", CreatedAtUtc = DateTimeOffset.UtcNow };
        db.AddRange(wrongCategory, terminal);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var missing = await service.CreateOrRefreshAsync(fixture.Admin.Id, Guid.NewGuid(), false, TestContext.Current.CancellationToken);
        var wrong = await service.CreateOrRefreshAsync(fixture.Admin.Id, wrongCategory.Id, false, TestContext.Current.CancellationToken);
        var blocked = await service.CreateOrRefreshAsync(fixture.Admin.Id, terminal.Id, false, TestContext.Current.CancellationToken);

        Assert.True(missing.IsNotFound);
        Assert.True(wrong.IsWrongCategory);
        Assert.True(blocked.IsRequestStateBlocked);
        Assert.Equal(0, await db.AccountAnonymizationOperations.CountAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<Fixture> SeedAsync(AppDbContext db, bool targetIsAdmin = false)
    {
        var user = new UserEntity { Id = Guid.NewGuid(), Email = "learner@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        var actorUser = new UserEntity { Id = Guid.NewGuid(), Email = "admin@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        var admin = new AdminUserEntity { Id = Guid.NewGuid(), UserId = actorUser.Id, NormalizedEmail = "ADMIN@EXAMPLE.TEST", Status = "active", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow };
        var report = new UserFeedbackReportEntity { Id = Guid.NewGuid(), UserId = user.Id, Category = UserFeedbackReportConstants.AccountDeletionCategory, Message = "do not return", Status = UserFeedbackReportConstants.NewStatus, ClientPlatform = "test", ClientVersion = "v1", CreatedAtUtc = DateTimeOffset.UtcNow };
        db.AddRange(user, actorUser, admin, report);
        if (targetIsAdmin) db.AdminUsers.Add(new AdminUserEntity { Id = Guid.NewGuid(), UserId = user.Id, Status = "active", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new Fixture(user, admin, report);
    }

    private static AppDbContext CreateDbContext(string? databaseName = null, IInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"));
        if (interceptor is not null) options.AddInterceptors(interceptor);
        return new AppDbContext(options.Options);
    }
    private sealed record Fixture(UserEntity User, AdminUserEntity Admin, UserFeedbackReportEntity Report);
    private sealed class ThrowingSaveChangesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) => throw new DbUpdateException("Unexpected test persistence failure");
    }
}
