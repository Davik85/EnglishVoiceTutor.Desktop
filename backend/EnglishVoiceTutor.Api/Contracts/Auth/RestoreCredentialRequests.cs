using System.Text.Json;

namespace EnglishVoiceTutor.Api.Contracts.Auth;

public sealed class RestoreCredentialCeremonyResponse
{
    public Guid CeremonyId { get; set; }
    public JsonElement Options { get; set; }
}

public sealed class RestoreCredentialVerifyRequest
{
    public Guid CeremonyId { get; set; }
    public JsonElement Credential { get; set; }
}
