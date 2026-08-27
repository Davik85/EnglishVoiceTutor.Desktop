namespace EnglishVoiceTutor.Api.Services.Billing;

internal static class GooglePlayTrialDeferralStatuses
{
    public const string Pending = "pending";
    public const string ProviderOutcomeUnknown = "outcome_unknown";
    public const string ProviderAppliedAwaitingRefresh = "awaiting_refresh";
    public const string Completed = "completed";
    public const string AmbiguousTerminal = "ambiguous_terminal";
}

internal static class GooglePlayTrialDeferralSafeErrorCodes
{
    public const string ProviderUnavailable = "trial_deferral_provider_unavailable";
    public const string ProviderOutcomeUnknown = "trial_deferral_provider_outcome_unknown";
    public const string ProviderStateDiverged = "trial_deferral_provider_state_diverged";
    public const string ProviderRejected = "trial_deferral_provider_rejected";
    public const string PersistenceUnavailable = "trial_deferral_persistence_unavailable";
}
