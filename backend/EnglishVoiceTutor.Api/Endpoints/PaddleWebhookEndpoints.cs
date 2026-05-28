using System.Text;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class PaddleWebhookEndpoints
{
    private const string PaddleSignatureHeaderName = "Paddle-Signature";

    public static void MapPaddleWebhookEndpoints(this WebApplication app)
    {
        app.MapPost(ApiConstants.PaddleBillingWebhookRoute, ReceivePaddleWebhookAsync)
            .AllowAnonymous();
    }

    private static async Task<IResult> ReceivePaddleWebhookAsync(
        HttpContext httpContext,
        IOptions<PaddleWebhookOptions> options,
        IPaddleWebhookSignatureVerifier signatureVerifier,
        IPaddleWebhookIngestionService ingestionService,
        IPaddleWebhookEventNormalizer webhookEventNormalizer,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var webhookOptions = options.Value;
        if (!webhookOptions.Enabled)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(webhookOptions.SecretKey))
        {
            return Results.Json(
                new { message = "Paddle webhook is not configured." },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var signatureHeader = httpContext.Request.Headers.TryGetValue(PaddleSignatureHeaderName, out var signatureValues)
            ? signatureValues.ToString()
            : null;
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return PaddleWebhookUnauthorized(
                "paddle_webhook_signature_missing",
                "Paddle webhook signature is required.");
        }

        using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: false);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var nowUtc = DateTimeOffset.UtcNow;
        var tolerance = TimeSpan.FromSeconds(Math.Max(0, webhookOptions.TimestampToleranceSeconds));
        var verificationResult = signatureVerifier.Verify(
            rawBody,
            signatureHeader,
            webhookOptions.SecretKey,
            nowUtc,
            tolerance);

        if (!verificationResult.IsValid)
        {
            var logger = loggerFactory.CreateLogger("PaddleWebhookEndpoint");
            logger.LogWarning(
                "Paddle webhook signature verification failed. ErrorCode={ErrorCode}; Timestamp={Timestamp:o}.",
                verificationResult.ErrorCode,
                verificationResult.Timestamp);

            return PaddleWebhookUnauthorized(
                MapSignatureVerificationErrorCode(verificationResult.ErrorCode),
                MapSignatureVerificationMessage(verificationResult.ErrorCode));
        }

        var ingestionResult = await ingestionService.IngestAsync(rawBody, signatureHeader, nowUtc, cancellationToken);
        if (!ingestionResult.IsSuccess)
        {
            return Results.BadRequest(new
            {
                errorCode = ingestionResult.ErrorCode,
                message = ingestionResult.Message
            });
        }

        var logger = loggerFactory.CreateLogger("PaddleWebhookEndpoint");
        PaddleWebhookEventNormalizationResult normalizationResult;
        try
        {
            normalizationResult = ingestionResult.EventId is null
                ? new PaddleWebhookEventNormalizationResult(0, 0, 0, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
                : await webhookEventNormalizer.NormalizeReceivedEventAsync(ingestionResult.EventId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var completedAtUtc = DateTimeOffset.UtcNow;
            normalizationResult = new PaddleWebhookEventNormalizationResult(0, 0, 0, 1, completedAtUtc, completedAtUtc);
            logger.LogError(exception, "Paddle webhook normalization failed after raw event ingestion. EventId={PaddleEventId}.", ingestionResult.EventId);
        }

        logger.LogInformation(
            "Paddle webhook normalization completed after ingestion. CheckedCount={CheckedCount}; NormalizedCount={NormalizedCount}; AlreadyNormalizedCount={AlreadyNormalizedCount}; FailedCount={FailedCount}.",
            normalizationResult.CheckedCount,
            normalizationResult.NormalizedCount,
            normalizationResult.AlreadyNormalizedCount,
            normalizationResult.FailedCount);

        return Results.Ok(new
        {
            accepted = true,
            duplicate = ingestionResult.IsDuplicate,
            eventId = ingestionResult.EventId,
            normalized = normalizationResult.NormalizedCount > 0 || normalizationResult.AlreadyNormalizedCount > 0,
            billingEventCreated = normalizationResult.NormalizedCount > 0,
            existingBillingEvent = ingestionResult.IsDuplicate || normalizationResult.AlreadyNormalizedCount > 0,
            message = ingestionResult.Message
        });
    }

    private static IResult PaddleWebhookUnauthorized(string errorCode, string message)
    {
        return Results.Json(
            new
            {
                errorCode,
                message
            },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    private static string MapSignatureVerificationErrorCode(string verificationErrorCode)
    {
        return verificationErrorCode switch
        {
            "missing_signature" => "paddle_webhook_signature_missing",
            _ => "paddle_webhook_signature_invalid"
        };
    }

    private static string MapSignatureVerificationMessage(string verificationErrorCode)
    {
        return verificationErrorCode switch
        {
            "missing_signature" => "Paddle webhook signature is required.",
            _ => "Paddle webhook signature is invalid."
        };
    }
}
