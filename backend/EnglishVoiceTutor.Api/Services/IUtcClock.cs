namespace EnglishVoiceTutor.Api.Services;

public interface IUtcClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class UtcClock : IUtcClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
