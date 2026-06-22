using System.Text.Encodings.Web;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Options;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Endpoints;

public static class PaddleCheckoutLaunchEndpoints
{
    public static void MapPaddleCheckoutLaunchEndpoints(this WebApplication app)
    {
        var checkoutLaunchEndpoint = app.MapGet(ApiConstants.PaddleCheckoutLaunchRoute, HandlePaddleCheckoutLaunch);

        if (IsRateLimitingEnabled(app))
        {
            checkoutLaunchEndpoint.RequireRateLimiting(RateLimitingConstants.PaddleCheckoutLaunchPolicyName);
        }
    }

    private static bool IsRateLimitingEnabled(WebApplication app) =>
        app.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()?.Enabled == true;

    private static IResult HandlePaddleCheckoutLaunch(
        HttpRequest request,
        IOptions<PaddleBillingOptions> options)
    {
        var paddleOptions = options.Value;
        var transactionId = request.Query[SubscriptionConstants.Billing.PaddleCheckoutLaunchTransactionIdParameterName].ToString();
        var environment = GetNormalizedEnvironment(paddleOptions.Environment);
        var configurationMissing = string.IsNullOrWhiteSpace(paddleOptions.ClientSideToken);
        var transactionMissing = string.IsNullOrWhiteSpace(transactionId);
        var html = BuildCheckoutLaunchHtml(
            transactionId,
            paddleOptions.ClientSideToken,
            environment,
            configurationMissing,
            transactionMissing);

        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static string BuildCheckoutLaunchHtml(
        string transactionId,
        string clientSideToken,
        string environment,
        bool configurationMissing,
        bool transactionMissing)
    {
        var title = configurationMissing || transactionMissing ? "Upgrade unavailable" : "Opening checkout";
        var message = configurationMissing
            ? "Upgrade is unavailable because checkout is not configured."
            : transactionMissing
                ? "Upgrade is unavailable because the checkout transaction is missing."
                : "Opening secure Paddle checkout…";

        var encodedTitle = HtmlEncoder.Default.Encode(title);
        var encodedMessage = HtmlEncoder.Default.Encode(message);
        var tokenJson = JsonSerializer.Serialize(clientSideToken.Trim());
        var environmentJson = JsonSerializer.Serialize(environment);
        var transactionIdJson = JsonSerializer.Serialize(transactionId.Trim());
        var canOpenCheckoutJson = JsonSerializer.Serialize(!configurationMissing && !transactionMissing);

        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{{encodedTitle}}</title>
  <style>
    body {
      margin: 0;
      min-height: 100vh;
      display: grid;
      place-items: center;
      background: #0f172a;
      color: #f8fafc;
      font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    }

    main {
      width: min(34rem, calc(100vw - 2rem));
      padding: 2rem;
      border-radius: 1rem;
      background: #111827;
      box-shadow: 0 1.5rem 4rem rgb(0 0 0 / 35%);
      text-align: center;
    }

    h1 {
      margin: 0 0 0.75rem;
      font-size: clamp(1.5rem, 4vw, 2.25rem);
    }

    p {
      margin: 0;
      color: #cbd5e1;
      line-height: 1.5;
    }
  </style>
</head>
<body>
  <main>
    <h1>{{encodedTitle}}</h1>
    <p id="checkout-status">{{encodedMessage}}</p>
  </main>
  <script src="https://cdn.paddle.com/paddle/v2/paddle.js"></script>
  <script>
    (() => {
      const canOpenCheckout = {{canOpenCheckoutJson}};
      const clientSideToken = {{tokenJson}};
      const environment = {{environmentJson}};
      const transactionId = {{transactionIdJson}};
      const statusElement = document.getElementById('checkout-status');

      const showMessage = (message) => {
        if (statusElement) {
          statusElement.textContent = message;
        }
      };

      if (!canOpenCheckout) {
        return;
      }

      if (!window.Paddle) {
        showMessage('Upgrade is unavailable because Paddle checkout could not be loaded.');
        return;
      }

      if (environment === 'sandbox' && window.Paddle.Environment) {
        window.Paddle.Environment.set('sandbox');
      }

      window.Paddle.Initialize({ token: clientSideToken });
      window.Paddle.Checkout.open({ transactionId });
    })();
  </script>
</body>
</html>
""";
    }

    private static string GetNormalizedEnvironment(string environment)
    {
        var normalizedEnvironment = string.IsNullOrWhiteSpace(environment)
            ? SubscriptionConstants.Billing.DefaultPaddleEnvironment
            : environment.Trim().ToLowerInvariant();

        return string.Equals(normalizedEnvironment, SubscriptionConstants.Billing.LivePaddleEnvironment, StringComparison.OrdinalIgnoreCase)
            ? SubscriptionConstants.Billing.LivePaddleEnvironment
            : SubscriptionConstants.Billing.DefaultPaddleEnvironment;
    }
}
