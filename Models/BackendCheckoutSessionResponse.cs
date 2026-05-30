namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendCheckoutSessionResponse
{
    public bool Created { get; init; }
    public bool CheckoutEnabled { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string PlanId { get; init; } = string.Empty;
    public string CheckoutUrl { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset CheckedAtUtc { get; init; }
}
