using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Auth;

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
