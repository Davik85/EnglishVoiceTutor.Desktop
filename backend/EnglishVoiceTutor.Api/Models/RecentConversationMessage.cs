namespace EnglishVoiceTutor.Api.Models;

public sealed class RecentConversationMessage
{
    public string Sender { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;
}
