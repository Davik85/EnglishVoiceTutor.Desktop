using System.Security.Claims;
using EnglishVoiceTutor.Api.Data.Entities;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminAuthAuditService
{
    Task RecordAsync(AdminAuthAuditEventEntity auditEvent, CancellationToken cancellationToken);
    Task RecordLogoutAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
