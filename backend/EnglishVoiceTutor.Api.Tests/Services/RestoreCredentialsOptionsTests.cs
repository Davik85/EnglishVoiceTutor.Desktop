using System.Text.Json;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Auth;
using Fido2NetLib;
using Fido2NetLib.Objects;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class RestoreCredentialsOptionsTests
{
    private const string ValidAndroidOrigin = "android:apk-key-hash:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";

    [Fact]
    public void ValidHttpsAndAndroidOriginsAreAcceptedAndRetainedForFido2Verification()
    {
        var options = EnabledOptions("https://example.test", ValidAndroidOrigin);

        options.ValidateWhenEnabled();

        var configuration = new RestoreCredentialsWebAuthnVerifier(options).CreateConfiguration();
        Assert.Equal("example.test", configuration.ServerDomain);
        Assert.Contains("https://example.test", configuration.Origins);
        Assert.Contains(ValidAndroidOrigin, configuration.Origins);
        Assert.Equal(2, configuration.Origins.Count);
    }

    [Fact]
    public void RegistrationOptionsRequireResidentKeyAndDiscourageUserVerification()
    {
        var registration = new RestoreCredentialsWebAuthnVerifier(EnabledOptions("https://example.test")).CreateRegistrationOptions(
            new Fido2User { Id = Guid.NewGuid().ToByteArray(), Name = "user@example.test", DisplayName = "User" }, []);

        Assert.NotNull(registration.AuthenticatorSelection);
        Assert.Equal(ResidentKeyRequirement.Required, registration.AuthenticatorSelection.ResidentKey);
        Assert.Equal(UserVerificationRequirement.Discouraged, registration.AuthenticatorSelection.UserVerification);
        var serialized = JsonSerializer.Serialize(registration, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"userVerification\":\"discouraged\"", serialized, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("android:apk-key-hash:")]
    [InlineData("android:apk-key-hash:*")]
    [InlineData("android:apk-key-hash:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("android:apk-key-hash:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("android:apk-key-hash:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("android:apk-key-hash:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8/path")]
    [InlineData("android:apk-key-hash:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8?query")]
    [InlineData("android:something")]
    [InlineData("http://example.test")]
    [InlineData("custom://example.test")]
    [InlineData("https://")]
    public void InvalidAllowedOriginsAreRejected(string origin)
    {
        Assert.Throws<InvalidOperationException>(() => EnabledOptions(origin).ValidateWhenEnabled());
    }

    [Theory]
    [InlineData("android:apk-key-hash:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8")]
    [InlineData("https://example.test")]
    [InlineData("example.test/path")]
    public void AndroidOriginsAndUrlsCannotBeUsedAsRelyingPartyIds(string rpId)
    {
        var options = EnabledOptions("https://example.test");
        options.RpId = rpId;

        Assert.Throws<InvalidOperationException>(() => options.ValidateWhenEnabled());
    }

    private static RestoreCredentialsOptions EnabledOptions(params string[] origins) => new()
    {
        Enabled = true,
        RpId = "example.test",
        RpName = "Example",
        AllowedOrigins = origins.ToList()
    };
}
