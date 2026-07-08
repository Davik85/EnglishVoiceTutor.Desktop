using System.Security.Claims;
using System.Threading.RateLimiting;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Options;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.RateLimiting;

public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddEnglishVoiceTutorRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRateLimitResponseAsync;
            options.AddPolicy(RateLimitingConstants.AuthLoginPolicyName, context => CreateFixedWindowPartition(
                GetIpPartitionKey(context),
                GetOptions(context).Auth.LoginPerIpLimit,
                GetOptions(context).Auth.LoginWindowMinutes));
            options.AddPolicy(RateLimitingConstants.AuthRegisterPolicyName, context => CreateFixedWindowPartition(
                GetIpPartitionKey(context),
                GetOptions(context).Auth.RegisterPerIpLimit,
                GetOptions(context).Auth.RegisterWindowMinutes));
            options.AddPolicy(RateLimitingConstants.AuthPasswordResetRequestPolicyName, context => CreateFixedWindowPartition(
                GetIpPartitionKey(context),
                GetOptions(context).Auth.PasswordResetPerIpLimit,
                GetOptions(context).Auth.PasswordResetWindowMinutes));
            options.AddPolicy(RateLimitingConstants.AuthPasswordResetConfirmPolicyName, context => CreateFixedWindowPartition(
                GetIpPartitionKey(context),
                GetOptions(context).Auth.PasswordResetConfirmPerIpLimit,
                GetOptions(context).Auth.PasswordResetConfirmWindowMinutes));
            options.AddPolicy(RateLimitingConstants.AuthSessionPolicyName, context => CreateUserOrIpPartition(
                context,
                GetOptions(context).Auth.SessionPerUserLimit,
                GetOptions(context).Auth.SessionWindowMinutes));
            options.AddPolicy(RateLimitingConstants.LessonChatReplyPolicyName, context => CreateLessonChatPartition(context));
            options.AddPolicy(RateLimitingConstants.LessonStartPolicyName, context => CreateUserOrIpPartition(
                context,
                GetOptions(context).Lessons.StartPerUserLimit,
                GetOptions(context).Lessons.LessonWindowMinutes));
            options.AddPolicy(RateLimitingConstants.LessonHintPolicyName, context => CreateUserOrIpPartition(
                context,
                GetOptions(context).Lessons.HintPerUserLimit,
                GetOptions(context).Lessons.LessonWindowMinutes));
            options.AddPolicy(RateLimitingConstants.LessonFeedbackPolicyName, context => CreateUserOrIpPartition(
                context,
                GetOptions(context).Lessons.FeedbackPerUserLimit,
                GetOptions(context).Lessons.LessonWindowMinutes));
            options.AddPolicy(RateLimitingConstants.LessonPersistedMessagePolicyName, context => CreateLessonSessionPartition(
                context,
                GetOptions(context).Lessons.PersistedMessagePerSessionLimit,
                GetOptions(context).Lessons.LessonWindowMinutes));
            options.AddPolicy(RateLimitingConstants.LessonStatusPolicyName, context => CreateUserOrIpPartition(
                context,
                GetOptions(context).Lessons.StatusPerUserLimit,
                GetOptions(context).Lessons.StatusWindowMinutes));
            options.AddPolicy(RateLimitingConstants.AudioTranscriptionPolicyName, context => CreateUserOrIpPartition(
                context,
                GetOptions(context).Audio.TranscriptionPerUserLimit,
                GetOptions(context).Audio.AudioWindowMinutes));
            options.AddPolicy(RateLimitingConstants.AudioSpeechPolicyName, context => CreateAudioTtsPartition(context));
            options.AddPolicy(RateLimitingConstants.AudioSpeechStreamPolicyName, context => CreateAudioTtsPartition(context));
            options.AddPolicy(RateLimitingConstants.TranslationPolicyName, context => CreateUserOrIpPartition(
                context,
                GetOptions(context).Translation.PerUserLimit,
                GetOptions(context).Translation.WindowMinutes));
            options.AddPolicy(RateLimitingConstants.RealtimeVoicePolicyName, context => CreateFixedWindowPartition(
                GetIpPartitionKey(context),
                GetOptions(context).Audio.RealtimeVoiceStartPerIpLimit,
                GetOptions(context).Audio.RealtimeVoiceWindowMinutes));
            options.AddPolicy(RateLimitingConstants.AdminReadPolicyName, context => CreateAdminPartition(
                context,
                GetOptions(context).Admin.ReadPerAdminLimit,
                GetOptions(context).Admin.WindowMinutes));
            options.AddPolicy(RateLimitingConstants.AdminWritePolicyName, context => CreateAdminPartition(
                context,
                GetOptions(context).Admin.WritePerAdminLimit,
                GetOptions(context).Admin.WindowMinutes));
            options.AddPolicy(RateLimitingConstants.AdminRoleManagementPolicyName, context => CreateAdminPartition(
                context,
                GetOptions(context).Admin.RoleManagementPerAdminLimit,
                GetOptions(context).Admin.WindowMinutes));
            options.AddPolicy(RateLimitingConstants.BillingCheckoutPolicyName, context => CreateUserOrIpPartition(
                context,
                GetOptions(context).Billing.CheckoutPerUserLimit,
                GetOptions(context).Billing.WindowMinutes));
            options.AddPolicy(RateLimitingConstants.BillingCancelRenewalPolicyName, context => CreateUserOrIpPartition(
                context,
                GetOptions(context).Billing.CancelPerUserLimit,
                GetOptions(context).Billing.WindowMinutes));
            options.AddPolicy(RateLimitingConstants.PaddleCheckoutLaunchPolicyName, context => CreateUserOrIpPartition(
                context,
                GetOptions(context).Billing.PaddleCheckoutLaunchPerIpLimit,
                GetOptions(context).Billing.WindowMinutes));
            options.AddPolicy(RateLimitingConstants.PaddleWebhookPolicyName, context => CreateFixedWindowPartition(
                GetIpPartitionKey(context),
                GetOptions(context).Billing.PaddleWebhookPerIpLimit,
                GetOptions(context).Billing.WebhookWindowMinutes));
        });

        return services;
    }

    private static RateLimitPartition<string> CreateLessonChatPartition(HttpContext context)
    {
        var options = GetOptions(context).Lessons;
        var userId = context.User.FindFirstValue(AuthClaimTypes.UserId) ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return CreateFixedWindowPartition($"user:{userId}", options.ChatReplyPerUserLimit, options.ChatReplyWindowMinutes);
        }

        // ASP.NET Core's built-in named partition policies choose a single partition key synchronously.
        // This first slice therefore uses authenticated user when available, otherwise IP fallback;
        // per-session chat throttling is documented for a future slice that can safely inspect the body.
        return CreateFixedWindowPartition(GetIpPartitionKey(context), options.ChatReplyPerIpFallbackLimit, options.ChatReplyWindowMinutes);
    }

    private static RateLimitPartition<string> CreateLessonSessionPartition(HttpContext context, int permitLimit, int windowMinutes)
    {
        var sessionId = context.Request.RouteValues.TryGetValue("sessionId", out var value) ? value?.ToString() : null;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            return CreateFixedWindowPartition($"lesson-session:{sessionId}", permitLimit, windowMinutes);
        }

        return CreateUserOrIpPartition(context, permitLimit, windowMinutes);
    }

    private static RateLimitPartition<string> CreateAudioTtsPartition(HttpContext context)
    {
        var options = GetOptions(context).Audio;
        return CreateUserOrIpPartition(context, options.TtsPerUserLimit, options.AudioWindowMinutes);
    }

    private static RateLimitPartition<string> CreateAdminPartition(HttpContext context, int permitLimit, int windowMinutes)
    {
        var adminUserId = context.User.FindFirstValue(AuthClaimTypes.AdminUserId);
        if (!string.IsNullOrWhiteSpace(adminUserId))
        {
            return CreateFixedWindowPartition($"admin:{adminUserId}", permitLimit, windowMinutes);
        }

        return CreateUserOrIpPartition(context, permitLimit, windowMinutes);
    }

    private static RateLimitPartition<string> CreateUserOrIpPartition(HttpContext context, int permitLimit, int windowMinutes)
    {
        var userId = context.User.FindFirstValue(AuthClaimTypes.UserId) ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return CreateFixedWindowPartition($"user:{userId}", permitLimit, windowMinutes);
        }

        return CreateFixedWindowPartition(GetIpPartitionKey(context), permitLimit, windowMinutes);
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(string key, int permitLimit, int windowMinutes)
    {
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, permitLimit),
            Window = TimeSpan.FromMinutes(Math.Max(1, windowMinutes)),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }

    private static RateLimitingOptions GetOptions(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

    private static string GetIpPartitionKey(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(address) ? "ip:unknown" : $"ip:{address}";
    }

    private static async ValueTask WriteRateLimitResponseAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var options = GetOptions(context.HttpContext);
        var retryAfterSeconds = GetRetryAfterSeconds(context, options);
        var policyName = GetPolicyName(context.HttpContext);
        var message = GetMessage(policyName);

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers[RateLimitingConstants.RetryAfterHeaderName] = retryAfterSeconds.ToString();

        if (options.LogThrottledRequests)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RateLimiting");
            var adminUserId = context.HttpContext.User.FindFirstValue(AuthClaimTypes.AdminUserId);
            var userId = context.HttpContext.User.FindFirstValue(AuthClaimTypes.UserId) ?? context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lessonSessionId = context.HttpContext.Request.RouteValues.TryGetValue("sessionId", out var routeSessionId) ? routeSessionId?.ToString() : null;
            logger.LogWarning(
                "Request throttled. Policy={PolicyName}; EndpointGroup={EndpointGroup}; StatusCode={StatusCode}; RetryAfterSeconds={RetryAfterSeconds}; AdminUserId={AdminUserId}; UserId={UserId}; LessonSessionId={LessonSessionId}; Path={Path}; Method={Method}; RemoteIp={RemoteIp}.",
                policyName,
                GetEndpointGroup(policyName),
                StatusCodes.Status429TooManyRequests,
                retryAfterSeconds,
                string.IsNullOrWhiteSpace(adminUserId) ? null : adminUserId,
                string.IsNullOrWhiteSpace(userId) ? null : userId,
                string.IsNullOrWhiteSpace(lessonSessionId) ? null : lessonSessionId,
                context.HttpContext.Request.Path.Value,
                context.HttpContext.Request.Method,
                context.HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = RateLimitingConstants.ErrorCode,
            message,
            retryAfterSeconds
        }, cancellationToken);
    }

    private static int GetRetryAfterSeconds(OnRejectedContext context, RateLimitingOptions options)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            return Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        return Math.Max(1, options.DefaultRetryAfterSeconds);
    }

    private static string GetPolicyName(HttpContext context) => context.Request.Path.Value switch
    {
        ApiConstants.AuthLoginRoute => RateLimitingConstants.AuthLoginPolicyName,
        ApiConstants.AuthRegisterRoute => RateLimitingConstants.AuthRegisterPolicyName,
        ApiConstants.AuthPasswordResetRequestRoute => RateLimitingConstants.AuthPasswordResetRequestPolicyName,
        ApiConstants.AuthPasswordResetConfirmRoute => RateLimitingConstants.AuthPasswordResetConfirmPolicyName,
        ApiConstants.AuthRefreshRoute or ApiConstants.AuthRevokeRoute or ApiConstants.AuthMeRoute or ApiConstants.AuthChangePasswordRoute => RateLimitingConstants.AuthSessionPolicyName,
        ApiConstants.LessonChatReplyRoute => RateLimitingConstants.LessonChatReplyPolicyName,
        ApiConstants.LessonChatHintRoute => RateLimitingConstants.LessonHintPolicyName,
        ApiConstants.LessonChatFeedbackRoute => RateLimitingConstants.LessonFeedbackPolicyName,
        ApiConstants.MeLessonSessionsRoute when HttpMethods.IsPost(context.Request.Method) => RateLimitingConstants.LessonStartPolicyName,
        _ when IsAuthenticatedLessonReplyRequest(context) => RateLimitingConstants.LessonChatReplyPolicyName,
        ApiConstants.MeSubscriptionStatusRoute or ApiConstants.MeLessonAccessRoute or ApiConstants.MeTrialClaimRoute => RateLimitingConstants.LessonStatusPolicyName,
        _ when IsAuthenticatedLessonMessageCreateRequest(context) => RateLimitingConstants.LessonPersistedMessagePolicyName,
        ApiConstants.AudioTranscriptionRoute => RateLimitingConstants.AudioTranscriptionPolicyName,
        ApiConstants.AudioSpeechRoute => RateLimitingConstants.AudioSpeechPolicyName,
        ApiConstants.AudioSpeechStreamRoute => RateLimitingConstants.AudioSpeechStreamPolicyName,
        ApiConstants.TranslationRoute => RateLimitingConstants.TranslationPolicyName,
        ApiConstants.RealtimeVoiceRoute => RateLimitingConstants.RealtimeVoicePolicyName,
        ApiConstants.MeBillingCheckoutSessionRoute => RateLimitingConstants.BillingCheckoutPolicyName,
        ApiConstants.MeBillingSubscriptionCancelRoute => RateLimitingConstants.BillingCancelRenewalPolicyName,
        ApiConstants.PaddleCheckoutLaunchRoute => RateLimitingConstants.PaddleCheckoutLaunchPolicyName,
        ApiConstants.PaddleBillingWebhookRoute => RateLimitingConstants.PaddleWebhookPolicyName,
        _ when IsAdminRoleManagementRequest(context) && !HttpMethods.IsGet(context.Request.Method) => RateLimitingConstants.AdminRoleManagementPolicyName,
        _ when IsAdminRequest(context) && HttpMethods.IsGet(context.Request.Method) => RateLimitingConstants.AdminReadPolicyName,
        _ when IsAdminRequest(context) => RateLimitingConstants.AdminWritePolicyName,
        _ => "unknown"
    };

    private static bool IsAuthenticatedLessonMessageCreateRequest(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method)
        && context.Request.Path.Value?.StartsWith("/api/me/lesson-sessions/", StringComparison.OrdinalIgnoreCase) == true
        && context.Request.Path.Value.EndsWith("/messages", StringComparison.OrdinalIgnoreCase);

    private static bool IsAuthenticatedLessonReplyRequest(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method)
        && context.Request.Path.Value?.StartsWith("/api/me/lesson-sessions/", StringComparison.OrdinalIgnoreCase) == true
        && context.Request.Path.Value.EndsWith("/reply", StringComparison.OrdinalIgnoreCase);

    private static bool IsAdminRequest(HttpContext context) =>
        context.Request.Path.Value?.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsAdminRoleManagementRequest(HttpContext context) =>
        context.Request.Path.Value?.StartsWith("/api/admin/role-assignments", StringComparison.OrdinalIgnoreCase) == true;

    private static string GetEndpointGroup(string policyName) => policyName switch
    {
        RateLimitingConstants.LessonChatReplyPolicyName => RateLimitingConstants.LessonChatEndpointGroup,
        RateLimitingConstants.LessonStartPolicyName => RateLimitingConstants.LessonStartEndpointGroup,
        RateLimitingConstants.LessonHintPolicyName => RateLimitingConstants.LessonHintEndpointGroup,
        RateLimitingConstants.LessonFeedbackPolicyName => RateLimitingConstants.LessonFeedbackEndpointGroup,
        RateLimitingConstants.LessonPersistedMessagePolicyName => RateLimitingConstants.LessonPersistedMessageEndpointGroup,
        RateLimitingConstants.LessonStatusPolicyName => RateLimitingConstants.LessonStatusEndpointGroup,
        RateLimitingConstants.AudioTranscriptionPolicyName or RateLimitingConstants.AudioSpeechPolicyName or RateLimitingConstants.AudioSpeechStreamPolicyName => RateLimitingConstants.AudioEndpointGroup,
        RateLimitingConstants.TranslationPolicyName => RateLimitingConstants.TranslationEndpointGroup,
        RateLimitingConstants.RealtimeVoicePolicyName => RateLimitingConstants.RealtimeVoiceEndpointGroup,
        RateLimitingConstants.AdminReadPolicyName => RateLimitingConstants.AdminReadEndpointGroup,
        RateLimitingConstants.AdminWritePolicyName => RateLimitingConstants.AdminWriteEndpointGroup,
        RateLimitingConstants.AdminRoleManagementPolicyName => RateLimitingConstants.AdminRoleManagementEndpointGroup,
        RateLimitingConstants.BillingCheckoutPolicyName => RateLimitingConstants.BillingCheckoutEndpointGroup,
        RateLimitingConstants.BillingCancelRenewalPolicyName => RateLimitingConstants.BillingCancelRenewalEndpointGroup,
        RateLimitingConstants.PaddleCheckoutLaunchPolicyName => RateLimitingConstants.PaddleCheckoutLaunchEndpointGroup,
        RateLimitingConstants.PaddleWebhookPolicyName => RateLimitingConstants.PaddleWebhookEndpointGroup,
        RateLimitingConstants.AuthLoginPolicyName or RateLimitingConstants.AuthRegisterPolicyName or RateLimitingConstants.AuthPasswordResetRequestPolicyName or RateLimitingConstants.AuthPasswordResetConfirmPolicyName or RateLimitingConstants.AuthSessionPolicyName => RateLimitingConstants.AuthEndpointGroup,
        _ => RateLimitingConstants.UnknownEndpointGroup
    };

    private static string GetMessage(string policyName) => policyName switch
    {
        RateLimitingConstants.AuthLoginPolicyName => RateLimitingConstants.LoginMessage,
        RateLimitingConstants.AuthRegisterPolicyName => RateLimitingConstants.RegisterMessage,
        RateLimitingConstants.AuthPasswordResetRequestPolicyName or RateLimitingConstants.AuthPasswordResetConfirmPolicyName => RateLimitingConstants.PasswordResetMessage,
        RateLimitingConstants.AuthSessionPolicyName => RateLimitingConstants.AuthSessionMessage,
        RateLimitingConstants.LessonChatReplyPolicyName => RateLimitingConstants.LessonChatReplyMessage,
        RateLimitingConstants.LessonStartPolicyName => RateLimitingConstants.LessonStartMessage,
        RateLimitingConstants.LessonHintPolicyName => RateLimitingConstants.LessonHintMessage,
        RateLimitingConstants.LessonFeedbackPolicyName => RateLimitingConstants.LessonFeedbackMessage,
        RateLimitingConstants.LessonPersistedMessagePolicyName => RateLimitingConstants.LessonPersistedMessageMessage,
        RateLimitingConstants.LessonStatusPolicyName => RateLimitingConstants.LessonStatusMessage,
        RateLimitingConstants.AudioTranscriptionPolicyName => RateLimitingConstants.AudioTranscriptionMessage,
        RateLimitingConstants.AudioSpeechPolicyName or RateLimitingConstants.AudioSpeechStreamPolicyName => RateLimitingConstants.AudioTtsMessage,
        RateLimitingConstants.TranslationPolicyName => RateLimitingConstants.TranslationMessage,
        RateLimitingConstants.RealtimeVoicePolicyName => RateLimitingConstants.RealtimeVoiceMessage,
        RateLimitingConstants.AdminReadPolicyName => RateLimitingConstants.AdminReadMessage,
        RateLimitingConstants.AdminWritePolicyName => RateLimitingConstants.AdminWriteMessage,
        RateLimitingConstants.AdminRoleManagementPolicyName => RateLimitingConstants.AdminRoleManagementMessage,
        RateLimitingConstants.BillingCheckoutPolicyName => RateLimitingConstants.BillingCheckoutMessage,
        RateLimitingConstants.BillingCancelRenewalPolicyName => RateLimitingConstants.BillingCancelRenewalMessage,
        RateLimitingConstants.PaddleCheckoutLaunchPolicyName => RateLimitingConstants.PaddleCheckoutLaunchMessage,
        RateLimitingConstants.PaddleWebhookPolicyName => RateLimitingConstants.PaddleWebhookMessage,
        _ => RateLimitingConstants.DefaultMessage
    };
}
