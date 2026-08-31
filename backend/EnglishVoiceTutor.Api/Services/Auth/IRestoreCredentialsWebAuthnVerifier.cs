using Fido2NetLib;
using Fido2NetLib.Objects;
using EnglishVoiceTutor.Api.Options;

namespace EnglishVoiceTutor.Api.Services.Auth;

public interface IRestoreCredentialsWebAuthnVerifier
{
    CredentialCreateOptions CreateRegistrationOptions(Fido2User user, IReadOnlyList<PublicKeyCredentialDescriptor> excludedCredentials);
    Task<RegisteredPublicKeyCredential> VerifyRegistrationAsync(AuthenticatorAttestationRawResponse response, CredentialCreateOptions originalOptions, IsCredentialIdUniqueToUserAsyncDelegate uniquenessCallback, CancellationToken cancellationToken);
    AssertionOptions CreateAssertionOptions();
    Task<VerifyAssertionResult> VerifyAssertionAsync(AuthenticatorAssertionRawResponse response, AssertionOptions originalOptions, byte[] publicKey, uint signatureCounter, IsUserHandleOwnerOfCredentialIdAsync ownershipCallback, CancellationToken cancellationToken);
}

public sealed class RestoreCredentialsWebAuthnVerifier(RestoreCredentialsOptions options) : IRestoreCredentialsWebAuthnVerifier
{
    private Fido2 CreateFido2() => new(CreateConfiguration());

    internal Fido2Configuration CreateConfiguration() => new()
    {
        ServerDomain = options.RpId,
        ServerName = options.RpName,
        Origins = options.AllowedOrigins.ToHashSet(StringComparer.Ordinal)
    };

    public CredentialCreateOptions CreateRegistrationOptions(Fido2User user, IReadOnlyList<PublicKeyCredentialDescriptor> excludedCredentials) => CreateFido2().RequestNewCredential(new RequestNewCredentialParams
    {
        User = user, ExcludeCredentials = excludedCredentials, AuthenticatorSelection = new AuthenticatorSelection { ResidentKey = ResidentKeyRequirement.Required, UserVerification = UserVerificationRequirement.Discouraged }, AttestationPreference = AttestationConveyancePreference.None
    });

    public Task<RegisteredPublicKeyCredential> VerifyRegistrationAsync(AuthenticatorAttestationRawResponse response, CredentialCreateOptions originalOptions, IsCredentialIdUniqueToUserAsyncDelegate uniquenessCallback, CancellationToken cancellationToken) => CreateFido2().MakeNewCredentialAsync(new MakeNewCredentialParams { AttestationResponse = response, OriginalOptions = originalOptions, IsCredentialIdUniqueToUserCallback = uniquenessCallback }, cancellationToken);

    public AssertionOptions CreateAssertionOptions() => CreateFido2().GetAssertionOptions(new GetAssertionOptionsParams { AllowedCredentials = [], UserVerification = UserVerificationRequirement.Discouraged });

    public Task<VerifyAssertionResult> VerifyAssertionAsync(AuthenticatorAssertionRawResponse response, AssertionOptions originalOptions, byte[] publicKey, uint signatureCounter, IsUserHandleOwnerOfCredentialIdAsync ownershipCallback, CancellationToken cancellationToken) => CreateFido2().MakeAssertionAsync(new MakeAssertionParams { AssertionResponse = response, OriginalOptions = originalOptions, StoredPublicKey = publicKey, StoredSignatureCounter = signatureCounter, IsUserHandleOwnerOfCredentialIdCallback = ownershipCallback }, cancellationToken);
}
