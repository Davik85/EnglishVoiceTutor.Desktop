namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public interface ISubscriptionPlanCatalogService
{
    Task EnsureDefaultPlansAsync(CancellationToken cancellationToken);
}
