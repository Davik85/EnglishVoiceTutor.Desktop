using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Auth;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using Fido2NetLib;
using Fido2NetLib.Exceptions;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Auth;

public sealed class RestoreCredentialsService(
    AppDbContext dbContext,
    IAuthService authService,
    IRestoreCredentialsWebAuthnVerifier webAuthnVerifier,
    IOptions<RestoreCredentialsOptions> optionsAccessor,
    ILogger<RestoreCredentialsService> logger) : IRestoreCredentialsService
{
    private const string RegistrationCeremony = "registration";
    private const string AssertionCeremony = "assertion";
    private const string RestoreCredentialKind = "restore";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RestoreCredentialsOptions options = optionsAccessor.Value;

    public async Task<RestoreCredentialCeremonyResponse?> CreateRegistrationOptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!IsAvailable()) return null;
        var user = await dbContext.Users.AsNoTracking().Include(item => item.Profile)
            .SingleOrDefaultAsync(item => item.Id == userId && item.Status == AuthConstants.ActiveUserStatus, cancellationToken);
        if (user is null) return null;

        var existing = await dbContext.RestoreCredentials.AsNoTracking().Where(item => item.UserId == userId && item.RevokedAtUtc == null)
            .Select(item => new PublicKeyCredentialDescriptor(item.CredentialId)).ToListAsync(cancellationToken);
        var fidoUser = new Fido2User { Id = user.Id.ToByteArray(), Name = user.Email, DisplayName = user.Profile?.DisplayName ?? user.Email };
        var optionsResult = webAuthnVerifier.CreateRegistrationOptions(fidoUser, existing);
        return await PersistCeremonyAsync(userId, RegistrationCeremony, optionsResult, cancellationToken);
    }

    public async Task<bool> VerifyRegistrationAsync(Guid userId, RestoreCredentialVerifyRequest request, CancellationToken cancellationToken)
    {
        if (!IsAvailable()) return false;
        var ceremony = await ConsumeCeremonyAsync(request.CeremonyId, RegistrationCeremony, userId, cancellationToken);
        if (ceremony is null) return false;
        try
        {
            var originalOptions = JsonSerializer.Deserialize<CredentialCreateOptions>(ceremony.OptionsJson, JsonOptions);
            var response = request.Credential.Deserialize<AuthenticatorAttestationRawResponse>(JsonOptions);
            if (originalOptions is null || response is null) return false;
            var result = await webAuthnVerifier.VerifyRegistrationAsync(response, originalOptions, async (args, token) => !await dbContext.RestoreCredentials.AnyAsync(item => item.CredentialId == args.CredentialId, token), cancellationToken);
            var duplicate = await dbContext.RestoreCredentials.SingleOrDefaultAsync(item => item.CredentialId == result.Id, cancellationToken);
            if (duplicate is not null) return duplicate.UserId == userId;
            dbContext.RestoreCredentials.Add(new RestoreCredentialEntity
            {
                Id = Guid.NewGuid(), UserId = userId, CredentialId = result.Id,
                UserHandle = result.User.Id, PublicKey = result.PublicKey, SignatureCounter = result.SignCount,
                CredentialKind = RestoreCredentialKind, CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Fido2VerificationException exception)
        {
            logger.LogInformation("Restore credential registration rejected. Result=InvalidCredential Reason={Reason}", ClassifyRegistrationFailure(exception));
            return false;
        }
        catch (JsonException)
        {
            logger.LogInformation("Restore credential registration rejected. Result=InvalidCredential Reason={Reason}", "MalformedCredentialJson");
            return false;
        }
        catch (ArgumentException)
        {
            logger.LogInformation("Restore credential registration rejected. Result=InvalidCredential Reason={Reason}", "InvalidCredentialArgument");
            return false;
        }
    }

    public async Task<RestoreCredentialCeremonyResponse?> CreateAssertionOptionsAsync(CancellationToken cancellationToken)
    {
        if (!IsAvailable()) return null;
        var assertionOptions = webAuthnVerifier.CreateAssertionOptions();
        return await PersistCeremonyAsync(null, AssertionCeremony, assertionOptions, cancellationToken);
    }

    public async Task<AuthResponse?> VerifyAssertionAsync(RestoreCredentialVerifyRequest request, CancellationToken cancellationToken)
    {
        if (!IsAvailable()) return null;
        var ceremony = await ConsumeCeremonyAsync(request.CeremonyId, AssertionCeremony, null, cancellationToken);
        if (ceremony is null) return null;
        try
        {
            var originalOptions = JsonSerializer.Deserialize<AssertionOptions>(ceremony.OptionsJson, JsonOptions);
            var response = request.Credential.Deserialize<AuthenticatorAssertionRawResponse>(JsonOptions);
            if (originalOptions is null || response is null) return null;
            var credential = await dbContext.RestoreCredentials.AsTracking().Include(item => item.User).ThenInclude(item => item.Profile)
                .SingleOrDefaultAsync(item => item.CredentialId == response.RawId && item.RevokedAtUtc == null && item.CredentialKind == RestoreCredentialKind, cancellationToken);
            if (credential is null || !string.Equals(credential.User.Status, AuthConstants.ActiveUserStatus, StringComparison.OrdinalIgnoreCase)) return null;
            var verification = await webAuthnVerifier.VerifyAssertionAsync(response, originalOptions, credential.PublicKey, credential.SignatureCounter, (args, _) => Task.FromResult(args.UserHandle is null || args.UserHandle.SequenceEqual(credential.UserHandle)), cancellationToken);
            credential.SignatureCounter = verification.SignCount;
            credential.LastUsedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return await authService.IssueSessionForActiveUserAsync(credential.UserId, cancellationToken);
        }
        catch (Exception exception) when (exception is Fido2VerificationException or JsonException or ArgumentException)
        {
            logger.LogInformation("Restore credential assertion rejected. Result=InvalidCredential");
            return null;
        }
    }

    private bool IsAvailable() => options.Enabled;

    private static string ClassifyRegistrationFailure(Fido2VerificationException exception)
    {
        if (exception.Code != Fido2ErrorCode.Unknown)
        {
            return exception.Code.ToString();
        }

        return exception.Message.StartsWith("Fully qualified origin ", StringComparison.Ordinal)
            ? "OriginMismatch"
            : "UnknownFido2Verification";
    }

    private async Task<RestoreCredentialCeremonyResponse> PersistCeremonyAsync(Guid? userId, string type, object ceremonyOptions, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new RestoreCredentialCeremonyEntity { Id = Guid.NewGuid(), UserId = userId, CeremonyType = type, OptionsJson = JsonSerializer.Serialize(ceremonyOptions, JsonOptions), CreatedAtUtc = now, ExpiresAtUtc = now.AddSeconds(options.ChallengeLifetimeSeconds) };
        dbContext.RestoreCredentialCeremonies.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new RestoreCredentialCeremonyResponse { CeremonyId = entity.Id, Options = JsonSerializer.SerializeToElement(ceremonyOptions, JsonOptions) };
    }

    private async Task<RestoreCredentialCeremonyEntity?> ConsumeCeremonyAsync(Guid id, string type, Guid? userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var ceremony = await dbContext.RestoreCredentialCeremonies.AsTracking().SingleOrDefaultAsync(item => item.Id == id && item.CeremonyType == type && item.UserId == userId && item.ConsumedAtUtc == null && item.ExpiresAtUtc > now, cancellationToken);
        if (ceremony is null) return null;
        ceremony.ConsumedAtUtc = now;
        ceremony.ConcurrencyRevision++;
        try { await dbContext.SaveChangesAsync(cancellationToken); return ceremony; }
        catch (DbUpdateConcurrencyException) { return null; }
    }
}
