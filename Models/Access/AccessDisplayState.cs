namespace EnglishVoiceTutor.Desktop.Models.Access;

public enum AccessDisplayState
{
    SignedOut,
    TrialActive,
    PremiumActive,
    FreeAllowanceAvailable,
    FreeAllowanceUsed,
    PastDue,
    CanceledOrPaused,
    CheckoutUnavailable,
    UnknownOrError
}
