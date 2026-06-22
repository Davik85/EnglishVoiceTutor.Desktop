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
            options.AddPolicy(RateLimitingConstants.LessonChatReplyPolicyName, context => CreateLessonChatPartition(context));
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
            var userId = context.HttpContext.User.FindFirstValue(AuthClaimTypes.UserId);
            logger.LogWarning(
                "Request throttled. Policy={PolicyName}; EndpointGroup={EndpointGroup}; StatusCode={StatusCode}; RetryAfterSeconds={RetryAfterSeconds}; UserId={UserId}; Path={Path}.",
                policyName,
                GetEndpointGroup(policyName),
                StatusCodes.Status429TooManyRequests,
                retryAfterSeconds,
                string.IsNullOrWhiteSpace(userId) ? null : userId,
                context.HttpContext.Request.Path.Value);
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
        ApiConstants.LessonChatReplyRoute => RateLimitingConstants.LessonChatReplyPolicyName,
        _ => "unknown"
    };

    private static string GetEndpointGroup(string policyName) => policyName switch
    {
        RateLimitingConstants.LessonChatReplyPolicyName => RateLimitingConstants.LessonChatEndpointGroup,
        RateLimitingConstants.AuthLoginPolicyName or RateLimitingConstants.AuthRegisterPolicyName or RateLimitingConstants.AuthPasswordResetRequestPolicyName or RateLimitingConstants.AuthPasswordResetConfirmPolicyName => RateLimitingConstants.AuthEndpointGroup,
        _ => RateLimitingConstants.UnknownEndpointGroup
    };

    private static string GetMessage(string policyName) => policyName switch
    {
        RateLimitingConstants.AuthLoginPolicyName => RateLimitingConstants.LoginMessage,
        RateLimitingConstants.AuthRegisterPolicyName => RateLimitingConstants.RegisterMessage,
        RateLimitingConstants.AuthPasswordResetRequestPolicyName or RateLimitingConstants.AuthPasswordResetConfirmPolicyName => RateLimitingConstants.PasswordResetMessage,
        RateLimitingConstants.LessonChatReplyPolicyName => RateLimitingConstants.LessonChatReplyMessage,
        _ => RateLimitingConstants.DefaultMessage
    };
}
