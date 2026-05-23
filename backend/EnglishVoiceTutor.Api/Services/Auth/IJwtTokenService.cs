using EnglishVoiceTutor.Api.Contracts.Auth;
using EnglishVoiceTutor.Api.Data.Entities;

namespace EnglishVoiceTutor.Api.Services.Auth;

public interface IJwtTokenService
{
    AuthResponse CreateAuthResponse(UserEntity user, string? displayName, DateTimeOffset createdAt);
}
