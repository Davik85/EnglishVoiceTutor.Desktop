namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminFeedbackReportListResponse
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyList<AdminFeedbackReportListItem> Items { get; init; } = [];
}

public sealed class AdminFeedbackReportListItem
{
    public Guid ReportId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string MessagePreview { get; init; } = string.Empty;
    public bool HasReportedAiText { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public string ClientPlatform { get; init; } = string.Empty;
    public string ClientVersion { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public string? UserDisplayName { get; init; }
}

public sealed class AdminFeedbackReportDetailsResponse
{
    public Guid ReportId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ReportedAiText { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? ReviewedAtUtc { get; init; }
    public string ClientPlatform { get; init; } = string.Empty;
    public string ClientVersion { get; init; } = string.Empty;
    public AdminFeedbackReportUser User { get; init; } = new();
    public IReadOnlyList<AdminFeedbackReportReplyHistoryItem> Replies { get; init; } = [];
}

public sealed class AdminFeedbackReportReplyHistoryItem
{
    public Guid ReplyId { get; init; }
    public string ReplyText { get; init; } = string.Empty;
    public string RecipientEmail { get; init; } = string.Empty;
    public string DeliveryStatus { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? SentAtUtc { get; init; }
    public string? FailureCode { get; init; }
}

public sealed class AdminFeedbackReportUser
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
}

public sealed class AdminFeedbackReportStatusChangeRequest
{
    public string? Status { get; init; }
}

public sealed class AdminFeedbackReportStatusChangeResponse
{
    public Guid ReportId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset? ReviewedAtUtc { get; init; }
}

public sealed class AdminFeedbackReportReplyRequest
{
    public string? ReplyText { get; init; }
}

public sealed class AdminFeedbackReportReplyResponse
{
    public Guid ReplyId { get; init; }
    public Guid FeedbackReportId { get; init; }
    public string DeliveryStatus { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? SentAtUtc { get; init; }
    public string ReportStatus { get; init; } = string.Empty;
    public DateTimeOffset? ReviewedAtUtc { get; init; }
    public string? FailureCode { get; init; }
}

public sealed class AdminFeedbackReportListQuery
{
    public string? Status { get; init; }
    public string? Category { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
