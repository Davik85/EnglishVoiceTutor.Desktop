using EnglishVoiceTutor.Api.Services.WebsiteCms;

namespace EnglishVoiceTutor.Api.Tests.Services.WebsiteCms;

public sealed class WebsiteCmsContentGuardTests
{
    [Theory]
    [InlineData("PADDLE_API_KEY=pdl_live_secret")]
    [InlineData("Webhook-Secret: whsec_value")]
    [InlineData("OpenAI API key sk-testsecretvalue")]
    [InlineData("Jwt Signing Key should never be here")]
    [InlineData("Host=db;Username=app;Password=secret")]
    [InlineData("raw_payload: { provider event }")]
    [InlineData("customer ctm_123456789")]
    [InlineData("transaction txn_123456789")]
    [InlineData("subscription sub_123456789")]
    public void FindBlockedSecretLikeMarkers_BlocksSecretLikeContent(string value)
    {
        Assert.NotEmpty(WebsiteCmsContentGuard.FindBlockedSecretLikeMarkers(value));
    }

    [Fact]
    public void FindBlockedSecretLikeMarkers_AllowsOrdinaryDraftPolicyText()
    {
        var result = WebsiteCmsContentGuard.FindBlockedSecretLikeMarkers(
            "Draft refund policy text for owner/legal review.",
            "Support hours and cancellation instructions without provider identifiers.");

        Assert.Empty(result);
    }
}
