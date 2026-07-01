using System.Security.Claims;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminAuthAuditService(AppDbContext dbContext) : IAdminAuthAuditService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task RecordAsync(AdminAuthAuditEventEntity auditEvent, CancellationToken cancellationToken)
    {
        auditEvent.Id = auditEvent.Id == Guid.Empty ? Guid.NewGuid() : auditEvent.Id;
        auditEvent.OccurredAtUtc = auditEvent.OccurredAtUtc == default ? DateTimeOffset.UtcNow : auditEvent.OccurredAtUtc;
        _dbContext.AdminAuthAuditEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordLogoutAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = ClaimsUserAccessor.TryGetUserId(principal);
        var email = ClaimsUserAccessor.TryGetUserEmail(principal);
        Guid? adminUserId = null;
        if (userId.HasValue)
        {
            adminUserId = await _dbContext.AdminUsers.AsNoTracking()
                .Where(adminUser => adminUser.UserId == userId.Value)
                .Select(adminUser => (Guid?)adminUser.Id)
                .SingleOrDefaultAsync(cancellationToken);
        }

        await RecordAsync(new AdminAuthAuditEventEntity
        {
            EventType = "admin_logout",
            Result = "succeeded",
            ActorUserId = userId,
            ActorAdminUserId = adminUserId,
            ActorEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim()
        }, cancellationToken);
    }
}
