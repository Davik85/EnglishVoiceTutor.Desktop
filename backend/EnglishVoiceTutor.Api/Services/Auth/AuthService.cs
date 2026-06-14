using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Auth;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using EnglishVoiceTutor.Shared.NativeLanguages;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishVoiceTutor.Api.Services.Auth;

public sealed class AuthService(
    AppDbContext dbContext,
    IPasswordHasher<UserEntity> passwordHasher,
    IJwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptionsAccessor,
    ITrialClaimService trialClaimService,
    IDevelopmentTestAccountService developmentTestAccountService,
    ILogger<AuthService> logger) : IAuthService
{
    private const string UniqueViolationSqlState = "23505";
    private readonly JwtOptions jwtOptions = jwtOptionsAccessor.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var existingUser = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(candidate => candidate.Email == normalizedEmail, cancellationToken);

        if (existingUser)
        {
            throw new AuthDuplicateEmailException();
        }

        var createdAt = DateTimeOffset.UtcNow;

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            Status = AuthConstants.ActiveUserStatus,
            CreatedAt = createdAt
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        var displayName = NormalizeDisplayName(request.DisplayName);
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            dbContext.UserProfiles.Add(new UserProfileEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DisplayName = displayName,
                NativeLanguage = NativeLanguageCatalog.DefaultLanguageId,
                CurrentLevel = "unknown",
                Timezone = "UTC",
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });
        }

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueEmailViolation(exception))
        {
            throw new AuthDuplicateEmailException();
        }

        var trialClaimResult = await trialClaimService.ClaimTrialAsync(
            user.Id,
            SubscriptionConstants.AccountRegistrationTrialSource,
            cancellationToken);

        logger.LogInformation(
            "Registration trial claim completed. UserId={UserId}; TrialClaimed={TrialClaimed}; TrialEndsAtUtc={TrialEndsAtUtc}",
            user.Id,
            trialClaimResult.Claimed,
            trialClaimResult.TrialEndsAtUtc);

        await developmentTestAccountService.EnsureUnlimitedPremiumAccessIfConfiguredAsync(user.Id, user.Email, cancellationToken);

        var refreshToken = IssueRefreshToken(user.Id, createdAt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return jwtTokenService.CreateAuthResponse(user, displayName, createdAt, refreshToken.Token, refreshToken.ExpiresAtUtc);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await dbContext.Users
            .AsTracking()
            .Include(candidate => candidate.Profile)
            .SingleOrDefaultAsync(candidate => candidate.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await developmentTestAccountService.EnsureUnlimitedPremiumAccessIfConfiguredAsync(user.Id, user.Email, cancellationToken);

        var refreshToken = IssueRefreshToken(user.Id, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return jwtTokenService.CreateAuthResponse(user, user.Profile?.DisplayName, user.CreatedAt, refreshToken.Token, refreshToken.ExpiresAtUtc);
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var tokenHash = RefreshTokenHasher.HashToken(request.RefreshToken);
        var storedToken = await dbContext.UserRefreshTokens
            .AsTracking()
            .Include(token => token.User)
            .ThenInclude(user => user.Profile)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            return null;
        }

        if (storedToken.RevokedAtUtc is not null)
        {
            await RevokeTokenFamilyAsync(storedToken.UserId, now, "refresh_token_reuse", cancellationToken);
            return null;
        }

        if (storedToken.ExpiresAtUtc <= now || !string.Equals(storedToken.User.Status, AuthConstants.ActiveUserStatus, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var replacement = IssueRefreshToken(storedToken.UserId, now);
        storedToken.RevokedAtUtc = now;
        storedToken.ReplacedByTokenHash = RefreshTokenHasher.HashToken(replacement.Token);
        storedToken.RevocationReason = "rotated";
        await dbContext.SaveChangesAsync(cancellationToken);

        return jwtTokenService.CreateAuthResponse(
            storedToken.User,
            storedToken.User.Profile?.DisplayName,
            storedToken.User.CreatedAt,
            replacement.Token,
            replacement.ExpiresAtUtc);
    }

    public async Task RevokeRefreshTokenAsync(RevokeRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = RefreshTokenHasher.HashToken(request.RefreshToken);
        var storedToken = await dbContext.UserRefreshTokens
            .AsTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (storedToken is null || storedToken.RevokedAtUtc is not null)
        {
            return;
        }

        storedToken.RevokedAtUtc = DateTimeOffset.UtcNow;
        storedToken.RevocationReason = "logout";
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private RefreshTokenIssueResult IssueRefreshToken(Guid userId, DateTimeOffset now)
    {
        var rawToken = RefreshTokenHasher.GenerateToken();
        var expiresAt = now.AddDays(Math.Max(1, jwtOptions.RefreshTokenLifetimeDays));
        dbContext.UserRefreshTokens.Add(new UserRefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = RefreshTokenHasher.HashToken(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAt
        });
        return new RefreshTokenIssueResult(rawToken, expiresAt);
    }

    private async Task RevokeTokenFamilyAsync(Guid userId, DateTimeOffset now, string reason, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.UserRefreshTokens
            .AsTracking()
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAtUtc = now;
            token.RevocationReason = reason;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword)
            || string.IsNullOrWhiteSpace(request.NewPassword)
            || !string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            return ChangePasswordResult.InvalidRequest;
        }

        if (request.NewPassword.Length < AuthConstants.MinimumPasswordLength)
        {
            return ChangePasswordResult.InvalidPasswordLength;
        }

        var user = await dbContext.Users
            .AsTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return ChangePasswordResult.UserNotFound;
        }

        var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return ChangePasswordResult.InvalidCurrentPassword;
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Password changed. UserId={UserId}", user.Id);
        return ChangePasswordResult.Success;
    }

    public async Task<AuthUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new AuthUserDto
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.Profile != null ? user.Profile.DisplayName : null,
                CreatedAt = user.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? NormalizeDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        return displayName.Trim();
    }

    private static bool IsUniqueEmailViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == UniqueViolationSqlState;
    }
}

public sealed class AuthDuplicateEmailException : Exception;
