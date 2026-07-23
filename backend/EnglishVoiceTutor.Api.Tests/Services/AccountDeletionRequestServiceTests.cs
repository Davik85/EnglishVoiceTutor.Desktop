using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class AccountDeletionRequestServiceTests
{
    [Fact]
    public async Task ValidPasswordCreatesNewRequestWithTrimmedOptionalReason()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "correct password");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(db).SubmitAsync(user.Id, "correct password", "  Please delete my account.  ", TestContext.Current.CancellationToken);

        var report = await db.UserFeedbackReports.SingleAsync(TestContext.Current.CancellationToken);
        Assert.False(result.IsPasswordRejected);
        Assert.False(result.IsAlreadyRequested);
        Assert.Equal(report.Id, result.Response?.ReportId);
        Assert.Equal(UserFeedbackReportConstants.AccountDeletionCategory, report.Category);
        Assert.Equal(UserFeedbackReportConstants.NewStatus, report.Status);
        Assert.Equal("Please delete my account.", report.Message);
        Assert.DoesNotContain("correct password", report.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WrongPasswordDoesNotCreateRequest()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "correct password");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(db).SubmitAsync(user.Id, "wrong password", null, TestContext.Current.CancellationToken);

        Assert.True(result.IsPasswordRejected);
        Assert.Equal(0, await db.UserFeedbackReports.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BlankReasonIsStoredAsEmptyAndReturnedWithoutSensitiveInput()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "correct password");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(db).SubmitAsync(user.Id, "correct password", "  ", TestContext.Current.CancellationToken);

        var report = await db.UserFeedbackReports.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(string.Empty, report.Message);
        Assert.NotNull(result.Response);
        Assert.Equal(UserFeedbackReportConstants.NewStatus, result.Response!.Status);
        Assert.False(result.Response.AlreadyRequested);
    }

    [Fact]
    public async Task OverlongReasonIsRejectedWithoutCreatingRequest()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "correct password");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(db).SubmitAsync(
            user.Id,
            "correct password",
            new string('a', EntityConstants.Lengths.FeedbackReportMessageMaxLength + 1),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsInvalid);
        Assert.Equal(0, await db.UserFeedbackReports.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExistingActiveRequestIsReturnedInsteadOfCreatingDuplicate()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "correct password");
        var active = CreateReport(user.Id, UserFeedbackReportConstants.ProcessingStatus);
        db.UserFeedbackReports.Add(active);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(db).SubmitAsync(user.Id, "correct password", "another request", TestContext.Current.CancellationToken);

        Assert.True(result.IsAlreadyRequested);
        Assert.Equal(active.Id, result.Response?.ReportId);
        Assert.Equal(UserFeedbackReportConstants.ProcessingStatus, result.Response?.Status);
        Assert.Equal(1, await db.UserFeedbackReports.CountAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(UserFeedbackReportConstants.ResolvedStatus)]
    [InlineData(UserFeedbackReportConstants.RejectedStatus)]
    public async Task TerminalRequestDoesNotPreventANewRequest(string terminalStatus)
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "correct password");
        db.UserFeedbackReports.Add(CreateReport(user.Id, terminalStatus));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(db).SubmitAsync(user.Id, "correct password", null, TestContext.Current.CancellationToken);

        Assert.False(result.IsAlreadyRequested);
        Assert.Equal(2, await db.UserFeedbackReports.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RequestsAreBoundToTheSpecifiedAuthenticatedUser()
    {
        await using var db = CreateDbContext();
        var firstUser = AddUser(db, "first password");
        var secondUser = AddUser(db, "second password");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateService(db).SubmitAsync(firstUser.Id, "first password", null, TestContext.Current.CancellationToken);
        var result = await CreateService(db).SubmitAsync(secondUser.Id, "second password", null, TestContext.Current.CancellationToken);

        Assert.False(result.IsAlreadyRequested);
        Assert.Equal(2, await db.UserFeedbackReports.Select(report => report.UserId).Distinct().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AdminSubmissionCreatesNormalRequestWithoutLearnerPassword()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "correct password");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(db).SubmitAdminAsync(user.Id, "  Request received by email.  ", TestContext.Current.CancellationToken);

        var report = await db.UserFeedbackReports.SingleAsync(TestContext.Current.CancellationToken);
        Assert.False(result.IsInvalid);
        Assert.False(result.IsPasswordRejected);
        Assert.Equal("Request received by email.", report.Message);
        Assert.Equal("admin_account_deletion_request", report.ClientPlatform);
        Assert.Equal(UserFeedbackReportConstants.AccountDeletionCategory, report.Category);
        Assert.Equal(UserFeedbackReportConstants.NewStatus, report.Status);
    }

    [Fact]
    public async Task AdminSubmissionRejectsBlankOrOversizedComment()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "correct password");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var blank = await CreateService(db).SubmitAdminAsync(user.Id, "  ", TestContext.Current.CancellationToken);
        var oversized = await CreateService(db).SubmitAdminAsync(user.Id, new string('a', EntityConstants.Lengths.FeedbackReportMessageMaxLength + 1), TestContext.Current.CancellationToken);

        Assert.True(blank.IsInvalid);
        Assert.True(oversized.IsInvalid);
        Assert.Equal(0, await db.UserFeedbackReports.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AdminSubmissionReturnsSafeNotFoundForMissingUser()
    {
        await using var db = CreateDbContext();

        var result = await CreateService(db).SubmitAdminAsync(Guid.NewGuid(), "Request received by email.", TestContext.Current.CancellationToken);

        Assert.True(result.IsUserUnavailable);
    }

    [Fact]
    public async Task LearnerAndAdminSubmissionsShareActiveRequestDuplicateProtection()
    {
        await using var db = CreateDbContext();
        var user = AddUser(db, "correct password");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        var learner = await service.SubmitAsync(user.Id, "correct password", "learner request", TestContext.Current.CancellationToken);
        var admin = await service.SubmitAdminAsync(user.Id, "Request received by email.", TestContext.Current.CancellationToken);

        Assert.False(learner.IsAlreadyRequested);
        Assert.True(admin.IsAlreadyRequested);
        Assert.Equal(learner.Response?.ReportId, admin.Response?.ReportId);
        Assert.Equal(1, await db.UserFeedbackReports.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AdminSubmissionReturnsTheConcurrentActiveRequest()
    {
        var databaseName = Guid.NewGuid().ToString();
        Guid userId;
        await using (var seedDb = CreateDbContext(databaseName))
        {
            var user = AddUser(seedDb, "correct password");
            userId = user.Id;
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using var db = CreateDbContext(databaseName, new ConcurrentRequestInterceptor(databaseName, userId));

        var result = await CreateService(db).SubmitAdminAsync(userId, "Request received by email.", TestContext.Current.CancellationToken);

        Assert.True(result.IsAlreadyRequested);
        Assert.True(result.Response?.AlreadyRequested);
        Assert.Equal(1, await db.UserFeedbackReports.CountAsync(TestContext.Current.CancellationToken));
    }

    private static AccountDeletionRequestService CreateService(AppDbContext db) => new(db, new PasswordHasher<UserEntity>());

    private static UserEntity AddUser(AppDbContext db, string password)
    {
        var user = new UserEntity { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.test", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        user.PasswordHash = new PasswordHasher<UserEntity>().HashPassword(user, password);
        db.Users.Add(user);
        return user;
    }

    private static UserFeedbackReportEntity CreateReport(Guid userId, string status) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, Category = UserFeedbackReportConstants.AccountDeletionCategory,
        Message = string.Empty, Status = status, ClientPlatform = "account_deletion_request", ClientVersion = "v1", CreatedAtUtc = DateTimeOffset.UtcNow
    };

    private static AppDbContext CreateDbContext(string? databaseName = null, IInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString());
        if (interceptor is not null) options.AddInterceptors(interceptor);
        return new AppDbContext(options.Options);
    }

    private sealed class ConcurrentRequestInterceptor(string databaseName, Guid userId) : SaveChangesInterceptor
    {
        private bool hasCreatedConcurrentRequest;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!hasCreatedConcurrentRequest)
            {
                hasCreatedConcurrentRequest = true;
                using var concurrentDb = CreateDbContext(databaseName);
                concurrentDb.UserFeedbackReports.Add(CreateReport(userId, UserFeedbackReportConstants.NewStatus));
                concurrentDb.SaveChanges();
                throw new DbUpdateException("Simulated duplicate constraint race.");
            }
            return ValueTask.FromResult(result);
        }
    }
}
