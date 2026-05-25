namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public interface IDevelopmentTestAccountService
{
    Task EnsureUnlimitedPremiumAccessIfConfiguredAsync(Guid userId, string email, CancellationToken cancellationToken);
}
