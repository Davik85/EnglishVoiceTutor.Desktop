namespace EnglishVoiceTutor.Api.Services;

internal static class SafeFailureLogger
{
    internal const string EmailDeliveryFailedCode = "email_delivery_failed";
    internal const string FeedbackReportPersistenceFailedCode = "feedback_report_persistence_failed";
    internal const string PasswordResetDeliveryFailedCode = "password_reset_delivery_failed";

    internal static void LogEmailDeliveryFailed(ILogger logger) =>
        logger.LogWarning("Email delivery failed. ErrorCode=email_delivery_failed.");

    internal static void LogFeedbackReportPersistenceFailed(ILogger logger) =>
        logger.LogWarning("Feedback report persistence failed. ErrorCode=feedback_report_persistence_failed.");

    internal static void LogPasswordResetDeliveryFailed(ILogger logger) =>
        logger.LogError("Password reset email delivery failed. ErrorCode=password_reset_delivery_failed.");
}
