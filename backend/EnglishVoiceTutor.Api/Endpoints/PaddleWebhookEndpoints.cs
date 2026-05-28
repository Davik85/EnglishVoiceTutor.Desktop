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
            return Results.Unauthorized();
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

            return Results.Unauthorized();
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

        return Results.Ok(new
        {
            accepted = true,
            duplicate = ingestionResult.IsDuplicate,
            eventId = ingestionResult.EventId,
            message = ingestionResult.Message
        });
    }
}
