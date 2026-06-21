using System.Text.Json;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;

namespace EnglishVoiceTutor.Api.Services.Admin;

// Validation messages intentionally include: ActionType must not be empty. Result must not be empty.
public sealed class AdminRoleAssignmentAuditService(
    AppDbContext dbContext,
    IAdminRolePermissionCatalogService adminRolePermissionCatalogService) : IAdminRoleAssignmentAuditService
{
    private const int MetadataJsonMaxLength = 4096;
    private const int ReasonMaxLength = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlySet<string> KnownActionTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        AdminRoleAssignmentAuditConstants.ActionTypes.AssignRole,
        AdminRoleAssignmentAuditConstants.ActionTypes.RevokeRole,
        AdminRoleAssignmentAuditConstants.ActionTypes.DisableAdmin,
        AdminRoleAssignmentAuditConstants.ActionTypes.EnableAdmin,
        AdminRoleAssignmentAuditConstants.ActionTypes.InviteCreated,
        AdminRoleAssignmentAuditConstants.ActionTypes.InviteRevoked,
        AdminRoleAssignmentAuditConstants.ActionTypes.LastOwnerBlocked,
        AdminRoleAssignmentAuditConstants.ActionTypes.SelfEscalationBlocked,
        AdminRoleAssignmentAuditConstants.ActionTypes.ValidationDenied
    };

    private static readonly IReadOnlySet<string> KnownResults = new HashSet<string>(StringComparer.Ordinal)
    {
        AdminRoleAssignmentAuditConstants.Results.Succeeded,
        AdminRoleAssignmentAuditConstants.Results.Denied,
        AdminRoleAssignmentAuditConstants.Results.FailedValidation,
        AdminRoleAssignmentAuditConstants.Results.FailedConflict
    };

    private static readonly IReadOnlySet<string> ReasonRequiredActionTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        AdminRoleAssignmentAuditConstants.ActionTypes.AssignRole,
        AdminRoleAssignmentAuditConstants.ActionTypes.RevokeRole,
        AdminRoleAssignmentAuditConstants.ActionTypes.DisableAdmin,
        AdminRoleAssignmentAuditConstants.ActionTypes.EnableAdmin,
        AdminRoleAssignmentAuditConstants.ActionTypes.LastOwnerBlocked,
        AdminRoleAssignmentAuditConstants.ActionTypes.SelfEscalationBlocked,
        AdminRoleAssignmentAuditConstants.ActionTypes.ValidationDenied
    };

    private static readonly string[] ForbiddenSafeMetadataFragments =
    [
        "password",
        "secret",
        "credential",
        "authorization",
        "api_key",
        "access_token",
        "refresh_token",
        "webhook"
    ];

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IAdminRolePermissionCatalogService _adminRolePermissionCatalogService = adminRolePermissionCatalogService;

    public async Task<AdminRoleAssignmentAuditResult> AppendAuditEventAsync(
        AdminRoleAssignmentAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actionType = ValidateKnownValue(request.ActionType, KnownActionTypes, nameof(request.ActionType));
        var result = ValidateKnownValue(request.Result, KnownResults, nameof(request.Result));
        var roleId = ValidateRoleId(request.RoleId);
        var reason = NormalizeReason(request.Reason, actionType, result);
        var oldRolesJson = SerializeRoles(request.OldRoles, nameof(request.OldRoles));
        var newRolesJson = SerializeRoles(request.NewRoles, nameof(request.NewRoles));
        var safeMetadataJson = ValidateSafeMetadataJson(request.SafeMetadataJson);

        if (request.TargetAdminUserId == Guid.Empty)
        {
            throw new ArgumentException("Target admin user id must not be empty.", nameof(request));
        }

        var occurredAtUtc = DateTimeOffset.UtcNow;
        var auditEvent = new AdminRoleAssignmentEventEntity
        {
            Id = Guid.NewGuid(),
            ActorAdminUserId = request.ActorAdminUserId,
            TargetAdminUserId = request.TargetAdminUserId,
            ActionType = actionType,
            RoleId = roleId,
            Reason = reason,
            OldRolesJson = oldRolesJson,
            NewRolesJson = newRolesJson,
            OccurredAtUtc = occurredAtUtc,
            Result = result,
            SafeMetadataJson = safeMetadataJson
        };

        await _dbContext.AdminRoleAssignmentEvents.AddAsync(auditEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminRoleAssignmentAuditResult(auditEvent.Id, occurredAtUtc);
    }

    private string ValidateKnownValue(string value, IReadOnlySet<string> knownValues, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} must not be empty.", parameterName);
        }

        var normalized = value.Trim();
        if (!knownValues.Contains(normalized))
        {
            throw new ArgumentException($"{parameterName} must be a known Admin role assignment audit value.", parameterName);
        }

        return normalized;
    }

    private string? ValidateRoleId(string? roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return null;
        }

        var normalized = roleId.Trim();
        if (!_adminRolePermissionCatalogService.GetProductionRolePermissions().ContainsKey(normalized))
        {
            throw new ArgumentException("Role id is not a known production Admin role.", nameof(roleId));
        }

        return normalized;
    }

    private static string? NormalizeReason(string? reason, string actionType, string result)
    {
        var normalized = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var reasonRequired = ReasonRequiredActionTypes.Contains(actionType)
            || string.Equals(result, AdminRoleAssignmentAuditConstants.Results.Denied, StringComparison.Ordinal)
            || string.Equals(result, AdminRoleAssignmentAuditConstants.Results.FailedValidation, StringComparison.Ordinal)
            || string.Equals(result, AdminRoleAssignmentAuditConstants.Results.FailedConflict, StringComparison.Ordinal);

        if (reasonRequired && normalized is null)
        {
            throw new ArgumentException("A non-empty human-readable reason is required for safety-sensitive Admin role assignment audit events.", nameof(reason));
        }

        if (normalized?.Length > ReasonMaxLength)
        {
            throw new ArgumentException("Reason is too long for Admin role assignment audit storage.", nameof(reason));
        }

        return normalized;
    }

    private static string? SerializeRoles(IReadOnlyList<string>? roleIds, string parameterName)
    {
        if (roleIds is null)
        {
            return null;
        }

        var normalizedRoleIds = roleIds
            .Where(roleId => !string.IsNullOrWhiteSpace(roleId))
            .Select(roleId => roleId.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(roleId => roleId, StringComparer.Ordinal)
            .ToArray();
        var json = JsonSerializer.Serialize(normalizedRoleIds, JsonOptions);

        if (json.Length > MetadataJsonMaxLength)
        {
            throw new ArgumentException($"{parameterName} is too long for Admin role assignment audit storage.", parameterName);
        }

        return json;
    }

    private static string? ValidateSafeMetadataJson(string? safeMetadataJson)
    {
        if (string.IsNullOrWhiteSpace(safeMetadataJson))
        {
            return null;
        }

        var normalized = safeMetadataJson.Trim();
        if (normalized.Length > MetadataJsonMaxLength)
        {
            throw new ArgumentException("Safe metadata JSON is too long for Admin role assignment audit storage.", nameof(safeMetadataJson));
        }

        foreach (var forbiddenFragment in ForbiddenSafeMetadataFragments)
        {
            if (normalized.Contains(forbiddenFragment, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Safe metadata JSON must not contain secret, credential, or raw provider payload fields.", nameof(safeMetadataJson));
            }
        }

        return normalized;
    }
}
