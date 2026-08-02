using System.Text;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class GooglePlayRtdnEndpoints
{
    private const int MaximumRequestBodyBytes = 64 * 1024;

    public static void MapGooglePlayRtdnEndpoints(this WebApplication app)
    {
        var options = app.Configuration.GetSection(GooglePlayRtdnOptions.SectionName).Get<GooglePlayRtdnOptions>() ?? new GooglePlayRtdnOptions();
        if (!options.Enabled) return;
        options.ValidateForEnabledMode();
        var billingOptions = app.Configuration.GetSection(GooglePlayBillingOptions.SectionName).Get<GooglePlayBillingOptions>() ?? new GooglePlayBillingOptions();
        if (string.IsNullOrWhiteSpace(billingOptions.PackageName)) throw new InvalidOperationException("Google Play RTDN requires a configured Google Play package name.");
        app.MapPost(ApiConstants.GooglePlayRtdnPushRoute, ReceiveAsync);
    }

    private static async Task<IResult> ReceiveAsync(HttpRequest request, IGooglePlayRtdnPushReceiptService receiptService, CancellationToken cancellationToken)
    {
        if (request.ContentLength is > MaximumRequestBodyBytes) return Results.BadRequest(new { error = "invalid_notification" });
        var body = await ReadBoundedBodyAsync(request.Body, cancellationToken);
        if (body is null) return Results.BadRequest(new { error = "invalid_notification" });

        var result = await receiptService.ReceiveAsync(request.Headers.Authorization.ToArray(), body, cancellationToken);
        return result.Status switch
        {
            GooglePlayRtdnPushReceiptStatus.NoContent => Results.NoContent(),
            GooglePlayRtdnPushReceiptStatus.Unauthorized => Results.Unauthorized(),
            GooglePlayRtdnPushReceiptStatus.BadRequest => Results.BadRequest(new { error = "invalid_notification" }),
            GooglePlayRtdnPushReceiptStatus.TemporarilyUnavailable => Results.StatusCode(StatusCodes.Status503ServiceUnavailable),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private static async Task<string?> ReadBoundedBodyAsync(Stream body, CancellationToken cancellationToken)
    {
        var buffer = new byte[MaximumRequestBodyBytes + 1];
        var read = 0;
        while (read < buffer.Length)
        {
            var count = await body.ReadAsync(buffer.AsMemory(read, buffer.Length - read), cancellationToken);
            if (count == 0) break;
            read += count;
        }
        return read > MaximumRequestBodyBytes ? null : Encoding.UTF8.GetString(buffer, 0, read);
    }
}
