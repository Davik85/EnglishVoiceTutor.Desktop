using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class SpeechVoiceOptionsTests
{
    public static readonly string[] ExpectedIds =
        ["alloy", "ash", "ballad", "coral", "echo", "sage", "shimmer", "verse", "marin", "cedar"];

    public static IEnumerable<object[]> SupportedVoices => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object?[]> UnsupportedVoices =>
        new string?[] { null, "", " ", "nova", "onyx", "fable", "unknown", " NOVA ", " ONYX ", " FABLE " }
            .Select(voice => new object?[] { voice });

    [Fact]
    public void CatalogContainsExactlyTheTenCanonicalVoices()
    {
        Assert.Equal(ExpectedIds, SpeechVoiceOptions.All.Select(voice => voice.Id));
        Assert.DoesNotContain(SpeechVoiceOptions.All, voice => voice.Id is "nova" or "onyx" or "fable");
    }

    [Theory]
    [MemberData(nameof(SupportedVoices))]
    public void SupportedVoicesResolveCanonicallyWithoutChangingTutorPreference(string voice)
    {
        Assert.Equal(voice, SpeechVoiceOptions.GetById(voice).Id);
        Assert.Equal(voice, SpeechVoiceOptions.ResolveSupportedVoiceId($" {voice.ToUpperInvariant()} ", "david"));
        Assert.Equal(voice, SpeechVoiceOptions.GetById($" {voice.ToUpperInvariant()} ", "david").Id);
        Assert.True(SpeechVoiceOptions.TryGetSupportedId($" {voice.ToUpperInvariant()} ", out var canonical));
        Assert.Equal(voice, canonical);
    }

    [Theory]
    [MemberData(nameof(UnsupportedVoices))]
    public void UnsupportedVoicesUseOnlySupportedTutorFallbacks(string? voice)
    {
        Assert.False(SpeechVoiceOptions.TryGetSupportedId(voice, out var canonical));
        Assert.Equal(string.Empty, canonical);
        Assert.Equal("coral", SpeechVoiceOptions.GetById(voice).Id);
        Assert.Equal("cedar", SpeechVoiceOptions.GetById(voice, " DAVID ").Id);
        foreach (var tutor in new string?[] { null, "lana", "nelli", "elena", "unknown" })
        {
            Assert.Equal("coral", SpeechVoiceOptions.ResolveSupportedVoiceId(voice, tutor));
        }
    }

    [Theory]
    [InlineData("david", "cedar")]
    [InlineData(" DAVID ", "cedar")]
    [InlineData("lana", "coral")]
    [InlineData("nelli", "coral")]
    [InlineData(null, "coral")]
    public void TutorPreferredVoicesRemainSupported(string? tutor, string expected)
    {
        Assert.Equal(expected, SpeechVoiceOptions.GetPreferredVoiceIdForTutor(tutor));
    }
}
