using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnglishVoiceTutor.Api.Services;

public interface IAccountAnonymizationPreflightService
{
    Task<AccountAnonymizationPreflightResult> CreateOrRefreshAsync(Guid actorAdminUserId, Guid reportId, bool refresh, CancellationToken cancellationToken);
    Task<AccountAnonymizationStatusResult> GetStatusAsync(Guid reportId, CancellationToken cancellationToken);
}

public sealed class AccountAnonymizationPreflightService(AppDbContext dbContext) : IAccountAnonymizationPreflightService
{
    public const string InitialPolicyVersion = "account_anonymization_policy_v1";
    public const string ProcedureVersion = "account_anonymization_procedure_v1";
    public const string BlockedState = "blocked";
    public const string PreflightState = "preflight";
    public const string BackupPolicyUnverified = "account_anonymization_backup_policy_unverified";
    public const string RetentionUnresolved = "account_anonymization_retention_unresolved";
    public const string ActiveAdminTarget = "account_anonymization_admin_target_blocked";
    public const string SelfTarget = "account_anonymization_self_target_blocked";
    public const string UnknownProvider = "account_anonymization_provider_unknown";
    public const string BillingLifecycleUnresolved = "account_anonymization_billing_lifecycle_unresolved";
    public const string AdminCmsDependencyUnclassified = "account_anonymization_admin_cms_dependency_unclassified";
    private static readonly TimeSpan PreflightLifetime = TimeSpan.FromMinutes(15);
    private static readonly string[] KnownProviderKeys = ["paddle", "google_play", "apple_app_store"];

    public async Task<AccountAnonymizationPreflightResult> CreateOrRefreshAsync(Guid actorAdminUserId, Guid reportId, bool refresh, CancellationToken cancellationToken)
        => await CreateOrRefreshAsync(actorAdminUserId, reportId, refresh, allowPolicySnapshotRaceRetry: true, cancellationToken);

    private async Task<AccountAnonymizationPreflightResult> CreateOrRefreshAsync(Guid actorAdminUserId, Guid reportId, bool refresh, bool allowPolicySnapshotRaceRetry, CancellationToken cancellationToken)
    {
        var report = await dbContext.UserFeedbackReports.AsNoTracking().SingleOrDefaultAsync(item => item.Id == reportId, cancellationToken);
        if (report is null) return AccountAnonymizationPreflightResult.NotFound();
        if (report.Category != UserFeedbackReportConstants.AccountDeletionCategory) return AccountAnonymizationPreflightResult.WrongCategory();
        if (report.Status is UserFeedbackReportConstants.ResolvedStatus or UserFeedbackReportConstants.RejectedStatus) return AccountAnonymizationPreflightResult.RequestStateBlocked();

        var now = DateTimeOffset.UtcNow;
        var operation = await dbContext.AccountAnonymizationOperations.SingleOrDefaultAsync(item => item.ReportId == reportId, cancellationToken);
        if (operation is not null && !refresh && operation.ExpiresAtUtc > now)
        {
            return AccountAnonymizationPreflightResult.Success(ToResponse(operation));
        }

        var snapshot = await GetOrCreatePolicySnapshotAsync(now, cancellationToken);
        var data = await BuildPreflightDataAsync(report, actorAdminUserId, snapshot, cancellationToken);
        var nextVersion = operation is null ? 1 : operation.PreflightVersion + 1;
        var fingerprint = CreateFingerprint(report, snapshot, data, nextVersion);
        if (operation is null)
        {
            operation = new AccountAnonymizationOperationEntity
            {
                Id = Guid.NewGuid(), ReportId = report.Id, TargetUserId = report.UserId, PolicySnapshotId = snapshot.Id,
                ActorAdminUserId = actorAdminUserId, CreatedAtUtc = now
            };
            dbContext.AccountAnonymizationOperations.Add(operation);
        }

        operation.PolicySnapshotId = snapshot.Id;
        operation.ActorAdminUserId = actorAdminUserId;
        operation.State = data.BlockingCodes.Count == 0 ? PreflightState : BlockedState;
        operation.PreflightVersion = nextVersion;
        operation.PreflightFingerprint = fingerprint;
        operation.ProcedureVersion = ProcedureVersion;
        operation.ExpiresAtUtc = now.Add(PreflightLifetime);
        operation.CategoryCountsJson = JsonSerializer.Serialize(data.CategoryCounts);
        operation.BlockingCodesJson = JsonSerializer.Serialize(data.BlockingCodes);
        operation.RetentionSummaryJson = JsonSerializer.Serialize(data.RetentionSummary);
        operation.ProviderStatesJson = JsonSerializer.Serialize(data.ProviderStates);
        operation.BackupReconciliationState = "unverified";
        operation.UpdatedAtUtc = now;
        operation.ConcurrencyRevision++;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return await ReloadDurableOperationOrUnavailableAsync(reportId, cancellationToken);
        }
        catch (DbUpdateException exception) when (IsOperationUniqueConstraint(exception))
        {
            return await ReloadDurableOperationOrUnavailableAsync(reportId, cancellationToken);
        }
        catch (DbUpdateException exception) when (allowPolicySnapshotRaceRetry && IsPolicySnapshotUniqueConstraint(exception))
        {
            dbContext.ChangeTracker.Clear();
            return await CreateOrRefreshAsync(actorAdminUserId, reportId, refresh, allowPolicySnapshotRaceRetry: false, cancellationToken);
        }
        catch (DbUpdateException)
        {
            return AccountAnonymizationPreflightResult.Unavailable();
        }

        return AccountAnonymizationPreflightResult.Success(ToResponse(operation));
    }

    public async Task<AccountAnonymizationStatusResult> GetStatusAsync(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await dbContext.UserFeedbackReports.AsNoTracking().SingleOrDefaultAsync(item => item.Id == reportId, cancellationToken);
        if (report is null) return AccountAnonymizationStatusResult.NotFound();
        if (report.Category != UserFeedbackReportConstants.AccountDeletionCategory) return AccountAnonymizationStatusResult.WrongCategory();
        var operation = await dbContext.AccountAnonymizationOperations.AsNoTracking().SingleOrDefaultAsync(item => item.ReportId == reportId, cancellationToken);
        return operation is null ? AccountAnonymizationStatusResult.NoOperation() : AccountAnonymizationStatusResult.Success(ToResponse(operation));
    }

    private async Task<AccountAnonymizationPolicySnapshotEntity> GetOrCreatePolicySnapshotAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var decisionsJson = JsonSerializer.Serialize(InitialPolicyDecisions);
        var hash = Sha256(decisionsJson);
        var existing = await dbContext.AccountAnonymizationPolicySnapshots.SingleOrDefaultAsync(item => item.PolicyVersion == InitialPolicyVersion, cancellationToken);
        if (existing is not null) return existing;
        var snapshot = new AccountAnonymizationPolicySnapshotEntity { Id = Guid.NewGuid(), PolicyVersion = InitialPolicyVersion, VersionHash = hash, CategoryDecisionsJson = decisionsJson, CreatedAtUtc = now };
        dbContext.AccountAnonymizationPolicySnapshots.Add(snapshot);
        return snapshot;
    }

    private async Task<PreflightData> BuildPreflightDataAsync(UserFeedbackReportEntity report, Guid actorAdminUserId, AccountAnonymizationPolicySnapshotEntity snapshot, CancellationToken cancellationToken)
    {
        var userId = report.UserId;
        var counts = new SortedDictionary<string, int>(StringComparer.Ordinal)
        {
            ["profile"] = await dbContext.UserProfiles.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["settings"] = await dbContext.UserSettings.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["refresh_tokens"] = await dbContext.UserRefreshTokens.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["password_reset_tokens"] = await dbContext.PasswordResetTokens.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["devices"] = await dbContext.Devices.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["lesson_sessions"] = await dbContext.LessonSessions.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["usage_events"] = await dbContext.UsageEvents.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["daily_usage_counters"] = await dbContext.DailyUsageCounters.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["daily_free_lesson_usage"] = await dbContext.DailyFreeLessonUsages.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["trial_grants"] = await dbContext.TrialGrants.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["subscriptions"] = await dbContext.Subscriptions.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["payments"] = await dbContext.Payments.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["entitlements"] = await dbContext.Entitlements.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["feedback_reports"] = await dbContext.UserFeedbackReports.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["admin_actions"] = await dbContext.AdminActions.AsNoTracking().CountAsync(item => item.TargetUserId == userId, cancellationToken),
            ["admin_user_mappings"] = await dbContext.AdminUsers.AsNoTracking().CountAsync(item => item.UserId == userId, cancellationToken),
            ["cms_content_pack_authorship"] = await dbContext.ContentPacks.AsNoTracking().CountAsync(item => item.CreatedByUserId == userId || item.UpdatedByUserId == userId, cancellationToken),
            ["cms_prompt_template_authorship"] = await dbContext.PromptTemplates.AsNoTracking().CountAsync(item => item.UpdatedByUserId == userId, cancellationToken),
            ["cms_content_version_authorship"] = await dbContext.ContentVersions.AsNoTracking().CountAsync(item => item.PublishedByUserId == userId, cancellationToken),
            ["cms_audit_logs"] = await dbContext.ContentAuditLogs.AsNoTracking().CountAsync(item => item.ActorUserId == userId, cancellationToken),
            ["paddle_webhook_records"] = await dbContext.PaddleWebhookEvents.AsNoTracking().CountAsync(item => item.InternalUserId == userId, cancellationToken)
        };
        var sessionIds = dbContext.LessonSessions.AsNoTracking().Where(item => item.UserId == userId).Select(item => item.Id);
        counts["lesson_messages"] = await dbContext.LessonMessages.AsNoTracking().CountAsync(item => sessionIds.Contains(item.SessionId), cancellationToken);
        counts["feedback_results"] = await dbContext.FeedbackResults.AsNoTracking().CountAsync(item => sessionIds.Contains(item.SessionId), cancellationToken);
        counts["lesson_summaries"] = await dbContext.LessonSummaries.AsNoTracking().CountAsync(item => sessionIds.Contains(item.SessionId), cancellationToken);
        counts["feedback_report_replies"] = await dbContext.UserFeedbackReportReplies.AsNoTracking().CountAsync(item => dbContext.UserFeedbackReports.Any(reportItem => reportItem.Id == item.FeedbackReportId && reportItem.UserId == userId), cancellationToken);
        var targetAdminIds = dbContext.AdminUsers.AsNoTracking().Where(item => item.UserId == userId).Select(item => item.Id);
        counts["admin_user_roles"] = await dbContext.AdminUserRoles.AsNoTracking().CountAsync(item => targetAdminIds.Contains(item.AdminUserId), cancellationToken);
        counts["admin_role_assignment_events"] = await dbContext.AdminRoleAssignmentEvents.AsNoTracking().CountAsync(item => targetAdminIds.Contains(item.TargetAdminUserId) || (item.ActorAdminUserId.HasValue && targetAdminIds.Contains(item.ActorAdminUserId.Value)), cancellationToken);
        counts["admin_auth_audit"] = await dbContext.AdminAuthAuditEvents.AsNoTracking().CountAsync(item => item.ActorUserId == userId || (item.ActorAdminUserId.HasValue && targetAdminIds.Contains(item.ActorAdminUserId.Value)), cancellationToken);

        var blockers = new SortedSet<string>(StringComparer.Ordinal) { BackupPolicyUnverified, RetentionUnresolved };
        var targetAdmin = await dbContext.AdminUsers.AsNoTracking().AnyAsync(item => item.UserId == userId && item.Status == "active", cancellationToken);
        if (targetAdmin) blockers.Add(ActiveAdminTarget);
        if (counts["admin_user_mappings"] + counts["admin_user_roles"] + counts["admin_role_assignment_events"] + counts["admin_auth_audit"] + counts["cms_content_pack_authorship"] + counts["cms_prompt_template_authorship"] + counts["cms_content_version_authorship"] + counts["cms_audit_logs"] > 0) blockers.Add(AdminCmsDependencyUnclassified);
        var actorUserId = await dbContext.AdminUsers.AsNoTracking().Where(item => item.Id == actorAdminUserId).Select(item => item.UserId).SingleOrDefaultAsync(cancellationToken);
        if (actorUserId == userId) blockers.Add(SelfTarget);

        var providerRecords = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        await AddSubscriptionProvidersAsync(userId, providerRecords, blockers, cancellationToken);
        await AddPaymentProvidersAsync(userId, providerRecords, blockers, cancellationToken);
        if (await dbContext.BillingEvents.AsNoTracking().AnyAsync(cancellationToken)) blockers.Add("account_anonymization_billing_events_unlinked");
        if (counts["paddle_webhook_records"] > 0) AddProviderState(providerRecords, "paddle", "local_webhook_records_present");
        var providers = providerRecords.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => new AccountAnonymizationProviderStateResponse { ProviderKey = item.Key, RecordCount = item.Value.Count, StateCodes = item.Value.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray() }).ToArray();
        var retention = new AccountAnonymizationRetentionSummaryResponse { ImmediateDeleteOrAnonymizeCount = InitialPolicyDecisions.Values.Count(value => value == "immediate_delete_or_anonymize"), UnresolvedDecisionCount = InitialPolicyDecisions.Values.Count(value => value == "unresolved_legal_decision") };
        return new PreflightData(counts, blockers.ToArray(), retention, providers);
    }

    private async Task AddSubscriptionProvidersAsync(Guid userId, Dictionary<string, List<string>> providers, SortedSet<string> blockers, CancellationToken cancellationToken)
    {
        var subscriptions = await dbContext.Subscriptions.AsNoTracking().Where(item => item.UserId == userId).Select(item => new { item.Provider, item.Status, item.CancelAtPeriodEnd }).ToListAsync(cancellationToken);
        foreach (var item in subscriptions)
        {
            var key = NormalizeProvider(item.Provider, blockers);
            var state = NormalizeSubscriptionState(item.Status, item.CancelAtPeriodEnd);
            AddProviderState(providers, key, state);
            if (state == "active") blockers.Add("account_anonymization_active_renewal");
            if (IsUncertainLifecycle(state)) blockers.Add(BillingLifecycleUnresolved);
        }
    }

    private async Task AddPaymentProvidersAsync(Guid userId, Dictionary<string, List<string>> providers, SortedSet<string> blockers, CancellationToken cancellationToken)
    {
        var payments = await dbContext.Payments.AsNoTracking().Where(item => item.UserId == userId).Select(item => new { item.Provider, item.Status }).ToListAsync(cancellationToken);
        foreach (var item in payments)
        {
            var key = NormalizeProvider(item.Provider, blockers);
            var state = NormalizePaymentState(item.Status);
            AddProviderState(providers, key, state);
            if (IsUncertainLifecycle(state)) blockers.Add(BillingLifecycleUnresolved);
        }
    }

    private static AccountAnonymizationPreflightResponse ToResponse(AccountAnonymizationOperationEntity operation) => new()
    {
        OperationId = operation.Id, ReportId = operation.ReportId, State = operation.State, PreflightVersion = operation.PreflightVersion, PreflightFingerprint = operation.PreflightFingerprint,
        ExpiresAtUtc = operation.ExpiresAtUtc, CategoryCounts = Deserialize<Dictionary<string, int>>(operation.CategoryCountsJson), BlockingReasonCodes = Deserialize<List<string>>(operation.BlockingCodesJson),
        RetentionSummary = Deserialize<AccountAnonymizationRetentionSummaryResponse>(operation.RetentionSummaryJson), ProviderStates = Deserialize<List<AccountAnonymizationProviderStateResponse>>(operation.ProviderStatesJson),
        BackupReconciliationState = operation.BackupReconciliationState, CreatedAtUtc = operation.CreatedAtUtc, UpdatedAtUtc = operation.UpdatedAtUtc
    };

    private static T Deserialize<T>(string json) where T : new() => JsonSerializer.Deserialize<T>(json) ?? new T();
    private static string CreateFingerprint(UserFeedbackReportEntity report, AccountAnonymizationPolicySnapshotEntity snapshot, PreflightData data, int version) => Sha256(JsonSerializer.Serialize(new { report.Category, report.Status, snapshot.PolicyVersion, snapshot.VersionHash, data.CategoryCounts, data.BlockingCodes, data.ProviderStates, version }));
    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private async Task<AccountAnonymizationPreflightResult> ReloadDurableOperationOrUnavailableAsync(Guid reportId, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var existing = await dbContext.AccountAnonymizationOperations.AsNoTracking().SingleOrDefaultAsync(item => item.ReportId == reportId, cancellationToken);
        return existing is null ? AccountAnonymizationPreflightResult.Unavailable() : AccountAnonymizationPreflightResult.Success(ToResponse(existing));
    }

    private static bool IsOperationUniqueConstraint(DbUpdateException exception) => IsUniqueConstraint(exception, "IX_account_anonymization_operations_ReportId");
    private static bool IsPolicySnapshotUniqueConstraint(DbUpdateException exception) => IsUniqueConstraint(exception, "IX_account_anonymization_policy_snapshots_PolicyVersion") || IsUniqueConstraint(exception, "IX_account_anonymization_policy_snapshots_VersionHash");
    private static bool IsUniqueConstraint(DbUpdateException exception, string constraintName) => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: var name } && string.Equals(name, constraintName, StringComparison.Ordinal);
    private static string NormalizeProvider(string? value, SortedSet<string> blockers)
    {
        if (string.IsNullOrWhiteSpace(value)) { blockers.Add(UnknownProvider); return "unknown"; }
        var normalized = value.Trim().ToLowerInvariant();
        if (KnownProviderKeys.Contains(normalized, StringComparer.Ordinal)) return normalized;
        blockers.Add(UnknownProvider);
        return "unsupported";
    }
    private static string NormalizeSubscriptionState(string? value, bool cancelAtPeriodEnd)
    {
        if (cancelAtPeriodEnd) return "cancellation_pending";
        return NormalizeLifecycleState(value, "active", "canceled", "pending", "refunded", "chargeback", "dispute", "completed");
    }
    private static string NormalizePaymentState(string? value) => NormalizeLifecycleState(value, "pending", "refunded", "chargeback", "dispute", "completed");
    private static string NormalizeLifecycleState(string? value, params string[] allowed)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (normalized is "cancelled" or "canceled") normalized = "canceled";
        if (normalized is "paid" or "succeeded" or "success") normalized = "completed";
        return normalized is not null && allowed.Contains(normalized, StringComparer.Ordinal) ? normalized : "unknown";
    }
    private static bool IsUncertainLifecycle(string state) => state is "cancellation_pending" or "pending" or "refunded" or "chargeback" or "dispute" or "unknown";
    private static void AddProviderState(Dictionary<string, List<string>> providers, string provider, string state) { if (!providers.TryGetValue(provider, out var states)) providers[provider] = states = []; states.Add(state); }
    private static readonly IReadOnlyDictionary<string, string> InitialPolicyDecisions = new SortedDictionary<string, string>(StringComparer.Ordinal)
    {
        ["devices"] = "immediate_delete_or_anonymize", ["lesson_content"] = "immediate_delete_or_anonymize", ["profile_settings"] = "immediate_delete_or_anonymize", ["tokens"] = "immediate_delete_or_anonymize",
        ["admin_audit"] = "unresolved_legal_decision", ["backup_restore"] = "unresolved_legal_decision", ["billing_financial"] = "unresolved_legal_decision", ["external_provider"] = "unresolved_legal_decision", ["support_content"] = "unresolved_legal_decision"
    };
    private sealed record PreflightData(SortedDictionary<string, int> CategoryCounts, IReadOnlyList<string> BlockingCodes, AccountAnonymizationRetentionSummaryResponse RetentionSummary, IReadOnlyList<AccountAnonymizationProviderStateResponse> ProviderStates);
}

public sealed class AccountAnonymizationPreflightResult
{
    public AccountAnonymizationPreflightResponse? Response { get; private init; }
    public bool IsNotFound { get; private init; }
    public bool IsWrongCategory { get; private init; }
    public bool IsRequestStateBlocked { get; private init; }
    public bool IsUnavailable { get; private init; }
    public static AccountAnonymizationPreflightResult Success(AccountAnonymizationPreflightResponse response) => new() { Response = response };
    public static AccountAnonymizationPreflightResult NotFound() => new() { IsNotFound = true };
    public static AccountAnonymizationPreflightResult WrongCategory() => new() { IsWrongCategory = true };
    public static AccountAnonymizationPreflightResult RequestStateBlocked() => new() { IsRequestStateBlocked = true };
    public static AccountAnonymizationPreflightResult Unavailable() => new() { IsUnavailable = true };
}

public sealed class AccountAnonymizationStatusResult
{
    public AccountAnonymizationPreflightResponse? Response { get; private init; }
    public bool IsNotFound { get; private init; }
    public bool IsWrongCategory { get; private init; }
    public bool IsNoOperation { get; private init; }
    public static AccountAnonymizationStatusResult Success(AccountAnonymizationPreflightResponse response) => new() { Response = response };
    public static AccountAnonymizationStatusResult NotFound() => new() { IsNotFound = true };
    public static AccountAnonymizationStatusResult WrongCategory() => new() { IsWrongCategory = true };
    public static AccountAnonymizationStatusResult NoOperation() => new() { IsNoOperation = true };
}
