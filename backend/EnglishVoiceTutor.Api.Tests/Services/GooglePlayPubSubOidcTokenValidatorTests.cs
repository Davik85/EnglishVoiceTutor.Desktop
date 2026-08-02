using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Billing;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayPubSubOidcTokenValidatorTests
{
    [Fact]
    public async Task ExpectedAudienceIsPassedToGoogleValidationBoundaryAndValidClaimsAreAccepted()
    {
        var boundary = new StubGoogleValidator(new("https://accounts.google.com", "push@example.test", true));
        var result = await CreateValidator(boundary).ValidateAsync("jwt", TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
        Assert.Equal("https://example.test/rtdn", boundary.ExpectedAudience);
    }

    [Theory]
    [InlineData("invalid-google-validation")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-email")]
    [InlineData("email-unverified")]
    public async Task InvalidGoogleResultOrClaimsAreRejected(string scenario)
    {
        GooglePlayPubSubOidcClaims? claims = scenario switch
        {
            "wrong-issuer" => new("https://issuer.example.test", "push@example.test", true),
            "wrong-email" => new("accounts.google.com", "other@example.test", true),
            "email-unverified" => new("accounts.google.com", "push@example.test", false),
            _ => null
        };

        var result = await CreateValidator(new StubGoogleValidator(claims)).ValidateAsync("jwt", TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    private static GooglePlayPubSubOidcTokenValidator CreateValidator(IGoogleJsonWebSignatureValidator boundary) => new(
        Microsoft.Extensions.Options.Options.Create(new GooglePlayRtdnOptions { Enabled = true, ExpectedAudience = "https://example.test/rtdn", ExpectedServiceAccountEmail = "push@example.test", ExpectedPubSubSubscription = "projects/example/subscriptions/rtdn" }),
        boundary);

    private sealed class StubGoogleValidator(GooglePlayPubSubOidcClaims? claims) : IGoogleJsonWebSignatureValidator
    {
        public string? ExpectedAudience { get; private set; }
        public Task<GooglePlayPubSubOidcClaims?> ValidateAsync(string token, string expectedAudience, CancellationToken cancellationToken) { ExpectedAudience = expectedAudience; return Task.FromResult(claims); }
    }
}
