namespace EnglishVoiceTutor.Desktop.Models;

public sealed record ChatMessage(int Id, string Sender, string Text, bool IsFromBot);
