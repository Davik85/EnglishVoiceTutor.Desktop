using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Auth;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Auth;

public sealed class AuthService(AppDbContext dbContext, IPasswordHasher<UserEntity> passwordHasher, IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
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
        await dbContext.SaveChangesAsync(cancellationToken);

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
}
