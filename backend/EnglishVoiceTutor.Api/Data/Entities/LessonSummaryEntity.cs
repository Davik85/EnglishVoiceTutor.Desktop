namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class LessonSummaryEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string? WhatWentWell { get; set; }
    public string? WhatToImprove { get; set; }
    public string? UsefulPhrases { get; set; }
    public string? MistakesToReview { get; set; }
    public string? NextSteps { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public LessonSessionEntity Session { get; set; } = null!;
}
