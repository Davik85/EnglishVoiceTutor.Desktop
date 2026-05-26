namespace EnglishVoiceTutor.Api.Contracts.Billing;

public sealed class CreateBillingCheckoutSessionRequest
{
    public string PlanId { get; init; } = string.Empty;
    public string ReturnUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
}
