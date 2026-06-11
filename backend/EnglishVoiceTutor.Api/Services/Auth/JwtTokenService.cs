using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Auth;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EnglishVoiceTutor.Api.Services.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptionsAccessor) : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptionsAccessor.Value;

    public AuthResponse CreateAuthResponse(UserEntity user, string? displayName, DateTimeOffset createdAt, string refreshToken, DateTimeOffset refreshTokenExpiresAtUtc)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(AuthClaimTypes.UserId, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email)
        };

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            claims.Add(new Claim(AuthClaimTypes.DisplayName, displayName));
        }

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return new AuthResponse
        {
            AccessToken = accessToken,
            TokenType = AuthConstants.TokenTypeBearer,
            ExpiresAtUtc = expiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
            User = new AuthUserDto
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = displayName,
                CreatedAt = createdAt
            }
        };
    }
}
