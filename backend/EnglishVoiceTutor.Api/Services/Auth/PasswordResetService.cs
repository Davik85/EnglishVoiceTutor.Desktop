using System.Security.Cryptography;
using System.Text;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Auth;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Auth;

public sealed class PasswordResetService(
    AppDbContext dbContext,
    IPasswordHasher<UserEntity> passwordHasher,
    IPasswordResetEmailSender emailSender,
    IOptions<PasswordResetOptions> options,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    private const int ResetTokenByteLength = 32;

    public async Task RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Password reset request accepted but feature is disabled.");
            return;
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return;
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Email == normalizedEmail, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("Password reset request accepted for non-existing account.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var rawToken = GenerateToken();
        var tokenHash = HashToken(rawToken);
        var lifetimeMinutes = GetTokenLifetimeMinutes();
        var resetToken = new PasswordResetTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(lifetimeMinutes)
        };

        dbContext.PasswordResetTokens.Add(resetToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var resetUrl = BuildResetUrl(rawToken);
        await emailSender.SendPasswordResetAsync(user, rawToken, resetUrl, cancellationToken);
        logger.LogInformation("Password reset token created. UserId={UserId}; ExpiresAtUtc={ExpiresAtUtc}.", user.Id, resetToken.ExpiresAtUtc);
    }

    public async Task<bool> ConfirmPasswordResetAsync(PasswordResetConfirmRequest request, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Password reset confirm rejected because feature is disabled.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return false;
        }

        if (request.NewPassword.Length < AuthConstants.MinimumPasswordLength)
        {
            return false;
        }

        var tokenHash = HashToken(request.Token);
        var now = DateTimeOffset.UtcNow;
        var resetToken = await dbContext.PasswordResetTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null || resetToken.UsedAtUtc is not null || resetToken.RevokedAtUtc is not null || resetToken.ExpiresAtUtc <= now)
        {
            return false;
        }

        resetToken.User.PasswordHash = passwordHasher.HashPassword(resetToken.User, request.NewPassword);
        resetToken.UsedAtUtc = now;

        await dbContext.PasswordResetTokens
            .Where(token => token.UserId == resetToken.UserId && token.Id != resetToken.Id && token.UsedAtUtc == null && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, now), cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Password reset confirmed. UserId={UserId}; TokenId={TokenId}.", resetToken.UserId, resetToken.Id);
        return true;
    }

    private int GetTokenLifetimeMinutes()
    {
        return options.Value.TokenLifetimeMinutes > 0
            ? options.Value.TokenLifetimeMinutes
            : PasswordResetOptions.DefaultTokenLifetimeMinutes;
    }

    private string BuildResetUrl(string rawToken)
    {
        var resetUrlBase = options.Value.ResetUrlBase?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resetUrlBase))
        {
            return string.Empty;
        }

        var separator = resetUrlBase.Contains('?') ? '&' : '?';
        return $"{resetUrlBase}{separator}token={Uri.EscapeDataString(rawToken)}";
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(ResetTokenByteLength);
        return Base64UrlEncode(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string NormalizeEmail(string email)
    {
        return string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();
    }
}
