using Google.Apis.Auth;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayPubSubOidcTokenValidator
{
    Task<GooglePlayPubSubOidcValidationResult> ValidateAsync(string token, CancellationToken cancellationToken);
}

public sealed record GooglePlayPubSubOidcValidationResult(bool IsValid)
{
    public static readonly GooglePlayPubSubOidcValidationResult Valid = new(true);
    public static readonly GooglePlayPubSubOidcValidationResult Invalid = new(false);
}

internal sealed record GooglePlayPubSubOidcClaims(string? Issuer, string? Email, bool EmailVerified);

internal interface IGoogleJsonWebSignatureValidator
{
    Task<GooglePlayPubSubOidcClaims?> ValidateAsync(string token, string expectedAudience, CancellationToken cancellationToken);
}

internal sealed class GoogleJsonWebSignatureValidator : IGoogleJsonWebSignatureValidator
{
    public async Task<GooglePlayPubSubOidcClaims?> ValidateAsync(string token, string expectedAudience, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var payload = await GoogleJsonWebSignature.ValidateAsync(token, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [expectedAudience]
        });
        return new(payload.Issuer, payload.Email, payload.EmailVerified);
    }
}

internal sealed class GooglePlayPubSubOidcTokenValidator(
    IOptions<GooglePlayRtdnOptions> optionsAccessor,
    IGoogleJsonWebSignatureValidator googleValidator) : IGooglePlayPubSubOidcTokenValidator
{
    public async Task<GooglePlayPubSubOidcValidationResult> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return GooglePlayPubSubOidcValidationResult.Invalid;

        var options = optionsAccessor.Value;
        try
        {
            var payload = await googleValidator.ValidateAsync(token, options.ExpectedAudience, cancellationToken);

            if (payload is null ||
                payload.Issuer is not ("accounts.google.com" or "https://accounts.google.com") ||
                !string.Equals(payload.Email, options.ExpectedServiceAccountEmail, StringComparison.Ordinal) ||
                !payload.EmailVerified)
            {
                return GooglePlayPubSubOidcValidationResult.Invalid;
            }

            return GooglePlayPubSubOidcValidationResult.Valid;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return GooglePlayPubSubOidcValidationResult.Invalid;
        }
    }
}
