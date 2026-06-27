using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities.WebsiteCms;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public sealed class WebsiteCmsAdminMutationService(AppDbContext dbContext) : IWebsiteCmsAdminMutationService
{
    private const string InitialReviewStatus = "not_started";
    private const string InitializationChangeReason = "Initialize Website CMS section metadata";
    private static readonly HashSet<string> AllowedReviewStatuses = new(StringComparer.Ordinal)
    {
        "not_started",
        "draft",
        "owner_review_needed",
        "legal_review_needed",
        "owner_approved",
        "legal_approved"
    };

    private readonly AppDbContext _dbContext = dbContext;

    public async Task<AdminWebsiteCmsSectionInitializationResponse> InitializeMissingSectionsAsync(CancellationToken cancellationToken)
    {
        var expectedSections = WebsiteCmsExpectedSections.All;
        var expectedKeys = expectedSections.Select(section => section.SectionKey).ToArray();
        var existingKeys = await _dbContext.WebsiteCmsSections
            .Where(section => expectedKeys.Contains(section.SectionKey))
            .Select(section => section.SectionKey)
            .ToListAsync(cancellationToken);
        var existing = new HashSet<string>(existingKeys, StringComparer.Ordinal);
        var now = DateTimeOffset.UtcNow;
        var results = new List<AdminWebsiteCmsSectionInitializationResult>(expectedSections.Count);

        foreach (var expected in expectedSections)
        {
            if (existing.Contains(expected.SectionKey))
            {
                results.Add(new AdminWebsiteCmsSectionInitializationResult { SectionKey = expected.SectionKey, State = "existing", Created = false });
                continue;
            }

            _dbContext.WebsiteCmsSections.Add(new WebsiteCmsSectionEntity
            {
                Id = Guid.NewGuid(),
                SectionKey = expected.SectionKey,
                DraftBody = string.Empty,
                PublishedBody = null,
                ReviewStatus = InitialReviewStatus,
                EffectiveDate = null,
                InternalNotes = null,
                ChangeReason = InitializationChangeReason,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                PublishedAtUtc = null
            });
            existing.Add(expected.SectionKey);
            results.Add(new AdminWebsiteCmsSectionInitializationResult { SectionKey = expected.SectionKey, State = "created", Created = true });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminWebsiteCmsSectionInitializationResponse
        {
            CreatedCount = results.Count(result => result.Created),
            ExistingCount = results.Count(result => !result.Created),
            TotalExpectedCount = expectedSections.Count,
            Sections = results,
            CheckedAtUtc = now
        };
    }

    public async Task<AdminWebsiteCmsSectionDetailResponse?> SaveDraftAsync(string sectionKey, AdminWebsiteCmsSectionDraftSaveRequest request, CancellationToken cancellationToken)
    {
        var expected = WebsiteCmsExpectedSections.All.SingleOrDefault(section => string.Equals(section.SectionKey, sectionKey, StringComparison.Ordinal));
        if (expected is null)
        {
            return null;
        }

        var changeReason = request.ChangeReason?.Trim();
        if (string.IsNullOrWhiteSpace(changeReason))
        {
            throw new InvalidOperationException("Change reason is required.");
        }

        var reviewStatus = string.IsNullOrWhiteSpace(request.ReviewStatus) ? "draft" : request.ReviewStatus.Trim();
        if (!AllowedReviewStatuses.Contains(reviewStatus))
        {
            throw new InvalidOperationException($"Review status must be one of: {string.Join(", ", AllowedReviewStatuses)}.");
        }

        WebsiteCmsContentGuard.ThrowIfBlocked(request.DraftBody, request.InternalNotes, changeReason);

        var row = await _dbContext.WebsiteCmsSections.SingleOrDefaultAsync(section => section.SectionKey == sectionKey, cancellationToken);
        if (row is null)
        {
            return null;
        }

        row.DraftBody = request.DraftBody ?? string.Empty;
        row.ReviewStatus = reviewStatus;
        row.EffectiveDate = request.EffectiveDate;
        row.InternalNotes = string.IsNullOrWhiteSpace(request.InternalNotes) ? null : request.InternalNotes.Trim();
        row.ChangeReason = changeReason;
        row.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminWebsiteCmsSectionDetailResponse
        {
            SectionKey = expected.SectionKey,
            DisplayName = expected.DisplayName,
            Description = expected.Description,
            ReviewStatus = row.ReviewStatus,
            EffectiveDate = row.EffectiveDate,
            DraftBody = row.DraftBody,
            PublishedBodyExists = !string.IsNullOrWhiteSpace(row.PublishedBody),
            PublishedAtUtc = row.PublishedAtUtc,
            InternalNotes = row.InternalNotes,
            ChangeReason = row.ChangeReason,
            CreatedAtUtc = row.CreatedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }
    public async Task<AdminWebsiteCmsSectionDetailResponse?> UpdateReviewStatusAsync(string sectionKey, AdminWebsiteCmsSectionReviewStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var expected = WebsiteCmsExpectedSections.All.SingleOrDefault(section => string.Equals(section.SectionKey, sectionKey, StringComparison.Ordinal));
        if (expected is null)
        {
            return null;
        }

        var changeReason = request.ChangeReason?.Trim();
        if (string.IsNullOrWhiteSpace(changeReason))
        {
            throw new InvalidOperationException("Change reason is required.");
        }

        var reviewStatus = request.ReviewStatus?.Trim() ?? string.Empty;
        if (!AllowedReviewStatuses.Contains(reviewStatus))
        {
            throw new InvalidOperationException($"Review status must be one of: {string.Join(", ", AllowedReviewStatuses)}. Owner/legal approved are internal review markers only; they do not publish content and are not final legal advice by themselves.");
        }

        WebsiteCmsContentGuard.ThrowIfBlocked(changeReason);

        var row = await _dbContext.WebsiteCmsSections.SingleOrDefaultAsync(section => section.SectionKey == sectionKey, cancellationToken);
        if (row is null)
        {
            return null;
        }

        row.ReviewStatus = reviewStatus;
        row.ChangeReason = changeReason;
        row.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminWebsiteCmsSectionDetailResponse
        {
            SectionKey = expected.SectionKey,
            DisplayName = expected.DisplayName,
            Description = expected.Description,
            ReviewStatus = row.ReviewStatus,
            EffectiveDate = row.EffectiveDate,
            DraftBody = row.DraftBody,
            PublishedBodyExists = !string.IsNullOrWhiteSpace(row.PublishedBody),
            PublishedAtUtc = row.PublishedAtUtc,
            InternalNotes = row.InternalNotes,
            ChangeReason = row.ChangeReason,
            CreatedAtUtc = row.CreatedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public async Task<AdminWebsiteCmsSectionPublishResponse?> PublishSectionAsync(string sectionKey, AdminWebsiteCmsSectionPublishRequest request, CancellationToken cancellationToken)
    {
        var expected = WebsiteCmsExpectedSections.All.SingleOrDefault(section => string.Equals(section.SectionKey, sectionKey, StringComparison.Ordinal));
        if (expected is null)
        {
            return null;
        }

        var changeReason = request.ChangeReason?.Trim();
        if (string.IsNullOrWhiteSpace(changeReason))
        {
            throw new InvalidOperationException("Change reason is required.");
        }

        var row = await _dbContext.WebsiteCmsSections.SingleOrDefaultAsync(section => section.SectionKey == sectionKey, cancellationToken);
        if (row is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(row.DraftBody))
        {
            throw new InvalidOperationException("DraftBody is required before Website CMS publish.");
        }

        if (!string.Equals(row.ReviewStatus, "legal_approved", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Website CMS publish requires legal_approved review status. owner_approved/legal_approved are not automatic publish and public rendering remains unchanged.");
        }

        WebsiteCmsContentGuard.ThrowIfBlocked(row.DraftBody, changeReason);

        var now = DateTimeOffset.UtcNow;
        row.PublishedBody = row.DraftBody;
        row.PublishedAtUtc = now;
        row.ChangeReason = changeReason;
        row.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminWebsiteCmsSectionPublishResponse
        {
            SectionKey = expected.SectionKey,
            ReviewStatus = row.ReviewStatus,
            PublishedBodyExists = !string.IsNullOrWhiteSpace(row.PublishedBody),
            PublishedAtUtc = row.PublishedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc,
            CheckedAtUtc = now,
            PublishedCheckedAtUtc = now,
            Message = "Admin-only Website CMS publish stored DraftBody in PublishedBody only. This does not update public website rendering, does not modify site/public, and does not enable live Paddle."
        };
    }

    public async Task<AdminWebsiteCmsSectionUnpublishResponse?> UnpublishSectionAsync(string sectionKey, AdminWebsiteCmsSectionUnpublishRequest request, CancellationToken cancellationToken)
    {
        var expected = WebsiteCmsExpectedSections.All.SingleOrDefault(section => string.Equals(section.SectionKey, sectionKey, StringComparison.Ordinal));
        if (expected is null)
        {
            return null;
        }

        var changeReason = request.ChangeReason?.Trim();
        if (string.IsNullOrWhiteSpace(changeReason))
        {
            throw new InvalidOperationException("Change reason is required.");
        }

        WebsiteCmsContentGuard.ThrowIfBlocked(changeReason);

        var row = await _dbContext.WebsiteCmsSections.SingleOrDefaultAsync(section => section.SectionKey == sectionKey, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        row.PublishedBody = null;
        row.PublishedAtUtc = null;
        row.ReviewStatus = string.IsNullOrWhiteSpace(row.DraftBody) ? "not_started" : "draft";
        row.ChangeReason = changeReason;
        row.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminWebsiteCmsSectionUnpublishResponse
        {
            SectionKey = expected.SectionKey,
            ReviewStatus = row.ReviewStatus,
            DraftBodyExists = !string.IsNullOrWhiteSpace(row.DraftBody),
            PublishedBodyExists = !string.IsNullOrWhiteSpace(row.PublishedBody),
            PublishedAtUtc = row.PublishedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc,
            CheckedAtUtc = now,
            UnpublishedCheckedAtUtc = now,
            Message = "Unpublished from internal Website CMS PublishedBody only. Public website rendering was not changed, site/public was not modified, and live Paddle was not enabled."
        };
    }

}
