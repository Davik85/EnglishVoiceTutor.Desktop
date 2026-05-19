namespace EnglishVoiceTutor.Api.Contracts.Common;

public sealed class ErrorResponse
{
    public required string Status { get; init; }

    public required string Message { get; init; }

    public required DateTimeOffset CheckedAtUtc { get; init; }
}
