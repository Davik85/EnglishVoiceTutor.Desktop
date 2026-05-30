namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendCheckoutSessionRequest
{
    public string PlanId { get; init; } = string.Empty;
    public string ReturnUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
}
