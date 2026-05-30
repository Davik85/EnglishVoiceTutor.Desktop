using Npgsql;

namespace EnglishVoiceTutor.Api.Services.Billing;

internal static class BillingSerializableTransactionRetryPolicy
{
    private const int MaxAttempts = 3;
    private const int InitialRetryDelayMilliseconds = 50;
    private const string SerializationFailureSqlState = "40001";

    public static async Task<T> ExecuteAsync<T>(
        Func<int, CancellationToken, Task<T>> operation,
        Action clearChangeTracker,
        ILogger logger,
        string operationName,
        Guid billingEventId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation(attempt, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException
                && IsSerializationFailure(exception)
                && attempt < MaxAttempts)
            {
                clearChangeTracker();

                var delay = TimeSpan.FromMilliseconds(InitialRetryDelayMilliseconds * attempt);
                logger.LogWarning(
                    exception,
                    "{OperationName} hit PostgreSQL serialization failure and will be retried. BillingEventId={BillingEventId}; Attempt={Attempt}; MaxAttempts={MaxAttempts}; RetryDelayMilliseconds={RetryDelayMilliseconds}.",
                    operationName,
                    billingEventId,
                    attempt,
                    MaxAttempts,
                    delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException
                && postgresException.SqlState == SerializationFailureSqlState)
            {
                return true;
            }
        }

        return false;
    }
}
