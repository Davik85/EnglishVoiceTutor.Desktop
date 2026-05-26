namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminManualPremiumGrantRequest
{
    public int DurationDays { get; init; }
    public string? Reason { get; init; }
}
