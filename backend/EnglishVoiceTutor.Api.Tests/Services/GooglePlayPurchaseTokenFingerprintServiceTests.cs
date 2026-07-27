using EnglishVoiceTutor.Api.Services.Billing;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayPurchaseTokenFingerprintServiceTests
{
    [Fact]
    public void ExactTokenProducesStableLowercaseSha256Fingerprint()
    {
        var fingerprint = new GooglePlayPurchaseTokenFingerprintService().CreateFingerprint("fake-token");

        Assert.Equal("e1466187c844c921b622aff2197444cfdc2c87489f7a6e71cef47b31a1602ced", fingerprint);
        Assert.Matches("^[0-9a-f]{64}$", fingerprint);
    }

    [Fact]
    public void LeadingAndTrailingWhitespaceAreSignificant()
    {
        var service = new GooglePlayPurchaseTokenFingerprintService();

        Assert.NotEqual(service.CreateFingerprint("fake-token"), service.CreateFingerprint(" fake-token "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankTokenIsRejected(string token)
    {
        Assert.Throws<ArgumentException>(() => new GooglePlayPurchaseTokenFingerprintService().CreateFingerprint(token));
    }
}
