namespace EnglishVoiceTutor.Api.Services.Auth;

public sealed record RefreshTokenIssueResult(string Token, DateTimeOffset ExpiresAtUtc);
