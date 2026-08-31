using System.Text.Json;
using EnglishVoiceTutor.Api.Contracts.Auth;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Fido2NetLib;
using Fido2NetLib.Exceptions;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class RestoreCredentialsServiceTests
{
    [Fact]
    public async Task ClassifiedRegistrationRejectionDoesNotPersistCredentialOrLogRawExceptionText()
    {
        await using var db = CreateDb();
        var user = new UserEntity { Id = Guid.NewGuid(), Email = "registration@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        const string sensitiveMessage = "Fully qualified origin https://sensitive.example.test is not equal to fully qualified original origin https://other.example.test";
        var verifier = new RegistrationVerifier(new Fido2VerificationException(Fido2ErrorCode.InvalidAttestation, sensitiveMessage));
        var logger = new RecordingLogger<RestoreCredentialsService>();
        var service = CreateService(db, true, verifier, logger: logger);
        var ceremony = await service.CreateRegistrationOptionsAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(ceremony);
        Assert.False(await service.VerifyRegistrationAsync(user.Id, RegistrationRequest(ceremony!.CeremonyId), TestContext.Current.CancellationToken));
        Assert.Empty(db.RestoreCredentials);
        var output = Assert.Single(logger.Entries);
        Assert.Equal("Restore credential registration rejected. Result=InvalidCredential Reason=InvalidAttestation", output.Message);
        Assert.Null(output.Exception);
        Assert.DoesNotContain(sensitiveMessage, output.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownOriginRegistrationRejectionUsesFixedOriginMismatchReason()
    {
        await using var db = CreateDb();
        var user = new UserEntity { Id = Guid.NewGuid(), Email = "origin@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        const string sensitiveMessage = "Fully qualified origin https://sensitive.example.test is not equal to fully qualified original origin https://other.example.test";
        var logger = new RecordingLogger<RestoreCredentialsService>();
        var service = CreateService(db, true, new RegistrationVerifier(new Fido2VerificationException(Fido2ErrorCode.Unknown, sensitiveMessage)), logger: logger);
        var ceremony = await service.CreateRegistrationOptionsAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(ceremony);
        Assert.False(await service.VerifyRegistrationAsync(user.Id, RegistrationRequest(ceremony!.CeremonyId), TestContext.Current.CancellationToken));
        Assert.Empty(db.RestoreCredentials);
        Assert.Equal("Restore credential registration rejected. Result=InvalidCredential Reason=OriginMismatch", Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task MalformedRegistrationCredentialJsonUsesFixedSafeReason()
    {
        await using var db = CreateDb();
        var user = new UserEntity { Id = Guid.NewGuid(), Email = "json@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var logger = new RecordingLogger<RestoreCredentialsService>();
        var service = CreateService(db, true, new RegistrationVerifier(new InvalidOperationException()), logger: logger);
        var ceremony = await service.CreateRegistrationOptionsAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(ceremony);
        var malformedRequest = new RestoreCredentialVerifyRequest { CeremonyId = ceremony!.CeremonyId, Credential = JsonDocument.Parse("{\"response\":\"not-an-object\"}").RootElement.Clone() };
        Assert.False(await service.VerifyRegistrationAsync(user.Id, malformedRequest, TestContext.Current.CancellationToken));
        Assert.Empty(db.RestoreCredentials);
        Assert.Equal("Restore credential registration rejected. Result=InvalidCredential Reason=MalformedCredentialJson", Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task RegistrationArgumentRejectionUsesFixedSafeReasonWithoutRawExceptionText()
    {
        await using var db = CreateDb();
        var user = new UserEntity { Id = Guid.NewGuid(), Email = "argument@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        const string sensitiveMessage = "credential-id-sensitive-value";
        var logger = new RecordingLogger<RestoreCredentialsService>();
        var service = CreateService(db, true, new RegistrationVerifier(new ArgumentException(sensitiveMessage)), logger: logger);
        var ceremony = await service.CreateRegistrationOptionsAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(ceremony);
        Assert.False(await service.VerifyRegistrationAsync(user.Id, RegistrationRequest(ceremony!.CeremonyId), TestContext.Current.CancellationToken));
        Assert.Empty(db.RestoreCredentials);
        var output = Assert.Single(logger.Entries);
        Assert.Equal("Restore credential registration rejected. Result=InvalidCredential Reason=InvalidCredentialArgument", output.Message);
        Assert.DoesNotContain(sensitiveMessage, output.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledFeatureReturnsUnavailableWithoutPersistingCeremonies()
    {
        await using var db = CreateDb();
        var service = CreateService(db, enabled: false);
        Assert.Null(await service.CreateAssertionOptionsAsync(TestContext.Current.CancellationToken));
        Assert.False(await service.VerifyRegistrationAsync(Guid.NewGuid(), new RestoreCredentialVerifyRequest(), TestContext.Current.CancellationToken));
        Assert.Empty(db.RestoreCredentialCeremonies);
    }

    [Fact]
    public async Task ExpiredOrConsumedAssertionCeremonyCannotBeUsedAgain()
    {
        await using var db = CreateDb();
        var service = CreateService(db, enabled: true);
        var expired = new RestoreCredentialCeremonyEntity { Id = Guid.NewGuid(), CeremonyType = "assertion", OptionsJson = "{}", CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2), ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1) };
        var consumed = new RestoreCredentialCeremonyEntity { Id = Guid.NewGuid(), CeremonyType = "assertion", OptionsJson = "{}", CreatedAtUtc = DateTimeOffset.UtcNow, ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(1), ConsumedAtUtc = DateTimeOffset.UtcNow };
        db.RestoreCredentialCeremonies.AddRange(expired, consumed);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Null(await service.VerifyAssertionAsync(new RestoreCredentialVerifyRequest { CeremonyId = expired.Id }, TestContext.Current.CancellationToken));
        Assert.Null(await service.VerifyAssertionAsync(new RestoreCredentialVerifyRequest { CeremonyId = consumed.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExistingSessionIssuerCreatesNormalHashedRefreshTokenForActiveUserOnly()
    {
        await using var db = CreateDb();
        var active = new UserEntity { Id = Guid.NewGuid(), Email = "active@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        var inactive = new UserEntity { Id = Guid.NewGuid(), Email = "inactive@example.test", PasswordHash = "hash", Status = "deleted", CreatedAt = DateTimeOffset.UtcNow };
        db.Users.AddRange(active, inactive);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var auth = CreateAuthService(db);
        var response = await auth.IssueSessionForActiveUserAsync(active.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(response);
        Assert.NotEmpty(response!.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        var stored = Assert.Single(db.UserRefreshTokens);
        Assert.NotEqual(response.RefreshToken, stored.TokenHash);
        Assert.Null(await auth.IssueSessionForActiveUserAsync(inactive.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task VerifiedAssertionIssuesNormalSessionUpdatesCredentialAndRejectsReplay()
    {
        await using var db = CreateDb();
        var user = await SeedUserAndCredentialAsync(db, "active");
        var verifier = new AssertionVerifier(user.CredentialId, 7);
        var service = CreateService(db, true, verifier, CreateAuthService(db));
        var ceremony = await SeedAssertionCeremonyAsync(db);

        var first = await service.VerifyAssertionAsync(AssertionRequest(ceremony.Id, user.CredentialId), TestContext.Current.CancellationToken);
        Assert.NotNull(first);
        Assert.NotEmpty(first!.AccessToken);
        Assert.NotEqual(Convert.ToBase64String(user.CredentialId), first.RefreshToken);
        var stored = await db.RestoreCredentials.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal((uint)7, stored.SignatureCounter);
        Assert.NotNull(stored.LastUsedAtUtc);
        var refresh = Assert.Single(db.UserRefreshTokens);
        Assert.NotEqual(first.RefreshToken, refresh.TokenHash);

        Assert.Null(await service.VerifyAssertionAsync(AssertionRequest(ceremony.Id, user.CredentialId), TestContext.Current.CancellationToken));
        Assert.Single(db.UserRefreshTokens);
        Assert.Equal(1, verifier.AssertionCalls);
    }

    [Fact]
    public async Task UnknownRevokedAndInactiveCredentialOwnersNeverReachSessionIssuance()
    {
        foreach (var state in new[] { "unknown", "revoked", "deleted" })
        {
            await using var db = CreateDb();
            var user = state == "unknown" ? null : await SeedUserAndCredentialAsync(db, state);
            var credentialId = user?.CredentialId ?? [9, 9, 9];
            var verifier = new AssertionVerifier(credentialId, 3);
            var service = CreateService(db, true, verifier, CreateAuthService(db));
            var ceremony = await SeedAssertionCeremonyAsync(db);
            if (state == "revoked")
            {
                var credential = await db.RestoreCredentials.SingleAsync(TestContext.Current.CancellationToken);
                credential.RevokedAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            Assert.Null(await service.VerifyAssertionAsync(AssertionRequest(ceremony.Id, credentialId), TestContext.Current.CancellationToken));
            Assert.Empty(db.UserRefreshTokens);
            Assert.Equal(0, verifier.AssertionCalls);
        }
    }

    private static RestoreCredentialsService CreateService(AppDbContext db, bool enabled, IRestoreCredentialsWebAuthnVerifier? verifier = null, IAuthService? authService = null, ILogger<RestoreCredentialsService>? logger = null) => new(db, authService ?? new NoopAuthService(), verifier ?? new NoopVerifier(), Microsoft.Extensions.Options.Options.Create(new RestoreCredentialsOptions { Enabled = enabled, RpId = "example.test", RpName = "Example", AllowedOrigins = ["https://example.test"] }), logger ?? NullLogger<RestoreCredentialsService>.Instance);
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
    private static AuthService CreateAuthService(AppDbContext db) => new(db, new PasswordHasher<UserEntity>(), new JwtTokenService(Microsoft.Extensions.Options.Options.Create(new JwtOptions { Issuer = "test", Audience = "test", SigningKey = "12345678901234567890123456789012" })), Microsoft.Extensions.Options.Options.Create(new JwtOptions { Issuer = "test", Audience = "test", SigningKey = "12345678901234567890123456789012" }), new NoopTrialClaimService(), new NoopDevelopmentTestAccountService(), NullLogger<AuthService>.Instance);
    private static async Task<RestoreCredentialEntity> SeedUserAndCredentialAsync(AppDbContext db, string status)
    {
        var user = new UserEntity { Id = Guid.NewGuid(), Email = $"{status}@example.test", PasswordHash = "hash", Status = status == "active" || status == "revoked" ? "active" : status, CreatedAt = DateTimeOffset.UtcNow };
        var credential = new RestoreCredentialEntity { Id = Guid.NewGuid(), UserId = user.Id, CredentialId = [1, 2, 3], UserHandle = user.Id.ToByteArray(), PublicKey = [4], CredentialKind = "restore", CreatedAtUtc = DateTimeOffset.UtcNow };
        db.Users.Add(user); db.RestoreCredentials.Add(credential); await db.SaveChangesAsync(TestContext.Current.CancellationToken); return credential;
    }
    private static async Task<RestoreCredentialCeremonyEntity> SeedAssertionCeremonyAsync(AppDbContext db)
    {
        var ceremony = new RestoreCredentialCeremonyEntity { Id = Guid.NewGuid(), CeremonyType = "assertion", OptionsJson = "{}", CreatedAtUtc = DateTimeOffset.UtcNow, ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(1) };
        db.RestoreCredentialCeremonies.Add(ceremony); await db.SaveChangesAsync(TestContext.Current.CancellationToken); return ceremony;
    }
    private static RestoreCredentialVerifyRequest AssertionRequest(Guid ceremonyId, byte[] credentialId) => new() { CeremonyId = ceremonyId, Credential = JsonDocument.Parse($"{{\"id\":\"AQ\",\"rawId\":\"{Convert.ToBase64String(credentialId)}\",\"type\":\"public-key\",\"response\":{{\"authenticatorData\":\"AA\",\"clientDataJSON\":\"e30\",\"signature\":\"AA\"}}}}").RootElement.Clone() };
    private static RestoreCredentialVerifyRequest RegistrationRequest(Guid ceremonyId) => new() { CeremonyId = ceremonyId, Credential = JsonDocument.Parse("{\"id\":\"AQ\",\"rawId\":\"AQ\",\"type\":\"public-key\",\"response\":{\"clientDataJSON\":\"e30\",\"attestationObject\":\"AA\"}}").RootElement.Clone() };

    private class NoopVerifier : IRestoreCredentialsWebAuthnVerifier
    {
        public virtual CredentialCreateOptions CreateRegistrationOptions(Fido2User user, IReadOnlyList<PublicKeyCredentialDescriptor> excludedCredentials) => throw new InvalidOperationException();
        public virtual Task<RegisteredPublicKeyCredential> VerifyRegistrationAsync(AuthenticatorAttestationRawResponse response, CredentialCreateOptions originalOptions, IsCredentialIdUniqueToUserAsyncDelegate uniquenessCallback, CancellationToken cancellationToken) => throw new InvalidOperationException();
        public AssertionOptions CreateAssertionOptions() => throw new InvalidOperationException();
        public virtual Task<VerifyAssertionResult> VerifyAssertionAsync(AuthenticatorAssertionRawResponse response, AssertionOptions originalOptions, byte[] publicKey, uint signatureCounter, IsUserHandleOwnerOfCredentialIdAsync ownershipCallback, CancellationToken cancellationToken) => throw new InvalidOperationException();
    }
    private sealed class AssertionVerifier(byte[] expectedCredentialId, uint signCount) : NoopVerifier
    {
        public int AssertionCalls { get; private set; }
        public override Task<VerifyAssertionResult> VerifyAssertionAsync(AuthenticatorAssertionRawResponse response, AssertionOptions originalOptions, byte[] publicKey, uint signatureCounter, IsUserHandleOwnerOfCredentialIdAsync ownershipCallback, CancellationToken cancellationToken)
        {
            AssertionCalls++;
            Assert.Equal(expectedCredentialId, response.RawId);
            return Task.FromResult(new VerifyAssertionResult { CredentialId = expectedCredentialId, SignCount = signCount });
        }
    }

    private sealed class RegistrationVerifier(Exception exception) : NoopVerifier
    {
        public override CredentialCreateOptions CreateRegistrationOptions(Fido2User user, IReadOnlyList<PublicKeyCredentialDescriptor> excludedCredentials) => new()
        {
            Challenge = [1],
            Rp = new PublicKeyCredentialRpEntity("example.test", "Example"),
            User = user,
            PubKeyCredParams = []
        };

        public override Task<RegisteredPublicKeyCredential> VerifyRegistrationAsync(AuthenticatorAttestationRawResponse response, CredentialCreateOptions originalOptions, IsCredentialIdUniqueToUserAsyncDelegate uniquenessCallback, CancellationToken cancellationToken) => Task.FromException<RegisteredPublicKeyCredential>(exception);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Entries.Add((formatter(state, exception), exception));
    }

    private sealed class NoopAuthService : IAuthService
    {
        public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task RevokeRefreshTokenAsync(RevokeRefreshTokenRequest request, CancellationToken cancellationToken) => Task.CompletedTask; public Task<AuthUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task<AuthResponse?> IssueSessionForActiveUserAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult<AuthResponse?>(null);
    }
    private sealed class NoopTrialClaimService : ITrialClaimService { public Task<EnglishVoiceTutor.Api.Contracts.Subscription.TrialClaimResponse> ClaimTrialAsync(Guid userId, string source, CancellationToken cancellationToken) => throw new NotSupportedException(); }
    private sealed class NoopDevelopmentTestAccountService : IDevelopmentTestAccountService { public Task EnsureUnlimitedPremiumAccessIfConfiguredAsync(Guid userId, string email, CancellationToken cancellationToken) => Task.CompletedTask; }
}
