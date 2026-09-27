using System.Reflection;
using EnglishVoiceTutor.Api.Contracts.Auth;
using EnglishVoiceTutor.Api.Contracts.Subscription;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Endpoints;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using EnglishVoiceTutor.Shared.UserProfiles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class AuthRegistrationDisplayNameTests
{
    [Theory]
    [InlineData("David")]
    [InlineData("José")]
    [InlineData("Łukasz")]
    [InlineData("Давид")]
    [InlineData("ماركو")]
    [InlineData("محمد")]
    [InlineData("山田")]
    [InlineData("민수")]
    [InlineData("𐐀")]
    public void SharedPolicyAcceptsUnicodeLetters(string name)
    {
        Assert.True(UserDisplayNamePolicy.TryNormalize(name, out var normalized));
        Assert.Equal(name, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SharedPolicyPreservesOptionalNames(string? name)
    {
        Assert.True(UserDisplayNamePolicy.TryNormalize(name, out var normalized));
        Assert.Null(normalized);
    }

    [Theory]
    [InlineData("David123")]
    [InlineData("David Smith")]
    [InlineData("David-Smith")]
    [InlineData("O'Connor")]
    [InlineData("David_")]
    [InlineData("David!")]
    [InlineData("@David")]
    [InlineData("😀")]
    [InlineData("David😀")]
    [InlineData("123")]
    [InlineData(" David")]
    [InlineData("David ")]
    public void SharedPolicyRejectsNonLetters(string name)
    {
        Assert.False(UserDisplayNamePolicy.TryNormalize(name, out _));
    }

    [Theory]
    [InlineData("David")]
    [InlineData("José")]
    [InlineData("Łukasz")]
    [InlineData("Давид")]
    [InlineData("Марко")]
    [InlineData("محمد")]
    [InlineData("山田")]
    [InlineData("민수")]
    [InlineData("𐐀")]
    public async Task UnicodeLetterNamesArePersisted(string name)
    {
        await using var db = CreateDb();
        var response = await CreateService(db).RegisterAsync(Request(name), TestContext.Current.CancellationToken);

        Assert.Equal(name, response.User.DisplayName);
        Assert.Equal(name, Assert.Single(db.UserProfiles).DisplayName);
        Assert.Single(db.Users);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OptionalNameDoesNotCreateProfile(string? name)
    {
        await using var db = CreateDb();
        var response = await CreateService(db).RegisterAsync(Request(name), TestContext.Current.CancellationToken);

        Assert.Null(response.User.DisplayName);
        Assert.Empty(db.UserProfiles);
        Assert.Single(db.Users);
    }

    [Theory]
    [InlineData("David123")]
    [InlineData("David Smith")]
    [InlineData("David-Smith")]
    [InlineData("O'Connor")]
    [InlineData("David_")]
    [InlineData("David!")]
    [InlineData("@David")]
    [InlineData("😀")]
    [InlineData("David😀")]
    [InlineData("123")]
    [InlineData(" David")]
    [InlineData("David ")]
    [InlineData("David\n")]
    public async Task InvalidNameReturnsBadRequestWithoutCreatingAccount(string name)
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var method = typeof(AuthEndpoints).GetMethod("RegisterAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var pending = (Task<IResult>)method.Invoke(null, [Request(name), service, NullLoggerFactory.Instance, TestContext.Current.CancellationToken])!;
        var result = await pending;

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        Assert.Empty(db.Users);
        Assert.Empty(db.UserProfiles);
        await Assert.ThrowsAsync<AuthInvalidDisplayNameException>(() => service.RegisterAsync(Request(name), TestContext.Current.CancellationToken));
        Assert.Empty(db.Users);
    }

    private static RegisterRequest Request(string? name) => new()
    {
        Email = "learner@example.test",
        Password = "password",
        DisplayName = name
    };

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static AuthService CreateService(AppDbContext db)
    {
        var jwt = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "test",
            Audience = "test",
            SigningKey = "12345678901234567890123456789012"
        });
        return new AuthService(db, new PasswordHasher<UserEntity>(), new JwtTokenService(jwt), jwt,
            new NoopTrialClaimService(), new NoopDevelopmentTestAccountService(), NullLogger<AuthService>.Instance);
    }

    private sealed class NoopTrialClaimService : ITrialClaimService
    {
        public Task<TrialClaimResponse> ClaimTrialAsync(Guid userId, string source, CancellationToken cancellationToken) =>
            Task.FromResult(new TrialClaimResponse { UserId = userId });
    }

    private sealed class NoopDevelopmentTestAccountService : IDevelopmentTestAccountService
    {
        public Task EnsureUnlimitedPremiumAccessIfConfiguredAsync(Guid userId, string email, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
