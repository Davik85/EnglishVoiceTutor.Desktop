namespace EnglishVoiceTutor.Api.Options;

public sealed class FreeLimitOptions
{
    public const string SectionName = "FreeLimits";
    public const string EnforcementModeEnforcing = "enforcing";
    public const string EnforcementModeDiagnosticsOnly = "diagnostics_only";

    public bool EnforcementEnabled { get; set; }
}
