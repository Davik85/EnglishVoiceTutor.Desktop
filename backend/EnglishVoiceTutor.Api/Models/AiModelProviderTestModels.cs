namespace EnglishVoiceTutor.Api.Models;

public sealed record AiModelProviderTestResponse(
    string OverallStatus,
    IReadOnlyList<AiModelProviderTestResult> Results);

public sealed record AiModelProviderTestResult(
    string RoleId,
    string RoleLabel,
    string ModelId,
    bool SyntaxValid,
    bool ProviderTested,
    bool? ProviderOk,
    string SafeCategory,
    string SafeMessage,
    int? StatusCode,
    long? DurationMs);

public static class AiModelProviderTestCategories
{
    public const string Ok = "ok";
    public const string NotTested = "not_tested";
    public const string UnavailableOrNotFound = "unavailable_or_not_found";
    public const string UnauthorizedOrForbidden = "unauthorized_or_forbidden";
    public const string RateLimited = "rate_limited";
    public const string QuotaOrBilling = "quota_or_billing";
    public const string InvalidRequest = "invalid_request";
    public const string ProviderError = "provider_error";
    public const string Timeout = "timeout";
    public const string Unknown = "unknown";
}
