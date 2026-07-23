using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class AccountAnonymizationExecutionServiceTests
{
    [Fact]
    public async Task FreshProcessingPreflightExecutesOnceAndLeavesANonLoginShell()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        var target = new UserEntity { Id = Guid.NewGuid(), Email = "learner@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        var actorUser = new UserEntity { Id = Guid.NewGuid(), Email = "admin@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        var actor = new AdminUserEntity { Id = Guid.NewGuid(), UserId = actorUser.Id, Status = "active", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow };
        var report = new UserFeedbackReportEntity { Id = Guid.NewGuid(), UserId = target.Id, Category = UserFeedbackReportConstants.AccountDeletionCategory, Message = "private reason", Status = UserFeedbackReportConstants.ProcessingStatus, ClientPlatform = "test", ClientVersion = "v1", CreatedAtUtc = DateTimeOffset.UtcNow };
        db.AddRange(target, actorUser, actor, report);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var preflight = (await new AccountAnonymizationPreflightService(db).CreateOrRefreshAsync(actor.Id, report.Id, true, TestContext.Current.CancellationToken)).Response!;

        var result = await new AccountAnonymizationExecutionService(db).ExecuteAsync(actor.Id, report.Id, new AccountAnonymizationExecuteRequest { OperationId = preflight.OperationId, PreflightFingerprint = preflight.PreflightFingerprint }, TestContext.Current.CancellationToken);
        var retry = await new AccountAnonymizationExecutionService(db).ExecuteAsync(actor.Id, report.Id, new AccountAnonymizationExecuteRequest { OperationId = preflight.OperationId, PreflightFingerprint = preflight.PreflightFingerprint }, TestContext.Current.CancellationToken);

        Assert.True(result.IsCompleted, result.Error);
        Assert.True(retry.IsCompleted);
        var deleted = await db.Users.SingleAsync(item => item.Id == target.Id, TestContext.Current.CancellationToken);
        Assert.Equal("deleted", deleted.Status);
        Assert.Equal($"deleted+{preflight.OperationId:N}@deleted.invalid", deleted.Email);
        Assert.Equal(UserFeedbackReportConstants.ResolvedStatus, (await db.UserFeedbackReports.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }
}
