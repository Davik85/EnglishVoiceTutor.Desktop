using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Auth;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishVoiceTutor.Api.Services.Auth;

public sealed class AuthService(
    AppDbContext dbContext,
    IPasswordHasher<UserEntity> passwordHasher,
    IJwtTokenService jwtTokenService,
    ITrialClaimService trialClaimService,
    IDevelopmentTestAccountService developmentTestAccountService,
    ILogger<AuthService> logger) : IAuthService
{
    private const string UniqueViolationSqlState = "23505";

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
                NativeLanguage = "unknown",
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

        return jwtTokenService.CreateAuthResponse(user, displayName, createdAt);
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

        return jwtTokenService.CreateAuthResponse(user, user.Profile?.DisplayName, user.CreatedAt);
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
