using EnglishVoiceTutor.Api.Contracts.Billing;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed record GooglePlayPurchaseVerificationServiceResult(int StatusCode, GooglePlayPurchaseVerificationResponse Response);
