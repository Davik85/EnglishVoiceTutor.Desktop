using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class PaddleBillingProviderCheckoutAdapter : IBillingProviderCheckoutAdapter
{
    private const string TransactionsPath = "/transactions";
    private const string PaddleRequestIdHeaderName = "Paddle-Request-Id";

    private readonly HttpClient httpClient;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly PaddleBillingOptions options;
    private readonly ILogger<PaddleBillingProviderCheckoutAdapter> logger;

    public PaddleBillingProviderCheckoutAdapter(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IOptions<PaddleBillingOptions> options,
        ILogger<PaddleBillingProviderCheckoutAdapter> logger)
    {
        this.httpClient = httpClient;
        this.httpContextAccessor = httpContextAccessor;
        this.options = options.Value;
        this.logger = logger;
    }

    public string ProviderId => SubscriptionConstants.BillingProviders.Paddle;

    public async Task<BillingProviderCheckoutResult> CreateCheckoutSessionAsync(
        BillingProviderCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var environment = GetNormalizedEnvironment();

        if (!options.CheckoutAdapterEnabled)
        {
            logger.LogInformation("Paddle checkout adapter resolved but disabled. PlanId={PlanId}; Environment={Environment}.", request.PlanId, environment);
            return CreateNotConfiguredResult(request.PlanId, SubscriptionConstants.Billing.PaddleCheckoutAdapterDisabledMessage);
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            logger.LogInformation("Paddle checkout adapter resolved but API key is not configured. PlanId={PlanId}; Environment={Environment}.", request.PlanId, environment);
            return CreateNotConfiguredResult(request.PlanId, SubscriptionConstants.Billing.PaddleCheckoutNotConfiguredMessage);
        }

        if (string.Equals(request.PlanId, SubscriptionConstants.Plans.PremiumPlanId, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(options.PremiumPriceId))
        {
            logger.LogInformation("Paddle checkout adapter resolved but Premium price id is not configured. PlanId={PlanId}; Environment={Environment}.", request.PlanId, environment);
            return CreateNotConfiguredResult(request.PlanId, SubscriptionConstants.Billing.PaddleCheckoutNotConfiguredMessage);
        }

        if (string.IsNullOrWhiteSpace(options.ClientSideToken))
        {
            logger.LogInformation("Paddle checkout adapter resolved but client-side token is not configured. PlanId={PlanId}; Environment={Environment}.", request.PlanId, environment);
            return CreateNotConfiguredResult(request.PlanId, SubscriptionConstants.Billing.PaddleCheckoutClientSideTokenMissingMessage);
        }

        var baseUrl = GetBaseUrl(environment);
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            logger.LogWarning("Paddle checkout adapter base URL is not configured correctly. PlanId={PlanId}; Environment={Environment}.", request.PlanId, environment);
            return CreateNotConfiguredResult(request.PlanId, SubscriptionConstants.Billing.PaddleCheckoutNotConfiguredMessage);
        }

        var transactionUri = new Uri(baseUri, TransactionsPath);
        var payload = new
        {
            collection_mode = "automatic",
            items = new[]
            {
                new
                {
                    price_id = options.PremiumPriceId.Trim(),
                    quantity = 1
                }
            },
            custom_data = new
            {
                evt_user_id = request.UserId.ToString(),
                evt_plan_id = SubscriptionConstants.Plans.PremiumPlanId,
                evt_checkout_source = SubscriptionConstants.Billing.PaddleCheckoutSourceDesktopBackend
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, transactionUri)
        {
            Content = JsonContent.Create(payload)
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey.Trim());
        httpRequest.Headers.TryAddWithoutValidation(
            SubscriptionConstants.Billing.PaddleApiVersionHeaderName,
            GetApiVersion());

        try
        {
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var paddleRequestId = GetHeaderValue(response, PaddleRequestIdHeaderName);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                logger.LogWarning(
                    "Paddle checkout transaction creation failed. StatusCode={StatusCode}; PaddleRequestId={PaddleRequestId}; PlanId={PlanId}; Environment={Environment}; UserId={UserId}.",
                    (int)response.StatusCode,
                    paddleRequestId,
                    request.PlanId,
                    environment,
                    request.UserId);
                return CreateFailedResult(request.PlanId);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsedResponse = ParsePaddleCheckoutResponse(responseBody);
            var checkoutUrlPresent = !string.IsNullOrWhiteSpace(parsedResponse.CheckoutUrl);
            var checkoutUrl = BuildBackendHostedCheckoutLaunchUrl(parsedResponse.TransactionId);

            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                logger.LogWarning(
                    "Paddle checkout transaction was created but backend checkout launch URL could not be built. PaddleRequestId={PaddleRequestId}; PaddleTransactionId={PaddleTransactionId}; CheckoutUrlPresent={CheckoutUrlPresent}; ClientSideTokenConfigured={ClientSideTokenConfigured}; PlanId={PlanId}; Environment={Environment}; UserId={UserId}.",
                    paddleRequestId,
                    parsedResponse.TransactionId,
                    checkoutUrlPresent,
                    !string.IsNullOrWhiteSpace(options.ClientSideToken),
                    request.PlanId,
                    environment,
                    request.UserId);
                return CreateUrlUnavailableResult(request.PlanId);
            }

            logger.LogInformation(
                "Paddle checkout transaction created with backend-hosted checkout launch URL. PaddleRequestId={PaddleRequestId}; PaddleTransactionId={PaddleTransactionId}; CheckoutUrlPresent={CheckoutUrlPresent}; ClientSideTokenConfigured={ClientSideTokenConfigured}; PlanId={PlanId}; Environment={Environment}; UserId={UserId}.",
                paddleRequestId,
                parsedResponse.TransactionId,
                checkoutUrlPresent,
                !string.IsNullOrWhiteSpace(options.ClientSideToken),
                request.PlanId,
                environment,
                request.UserId);

            return new BillingProviderCheckoutResult
            {
                Created = true,
                CheckoutEnabled = true,
                Provider = SubscriptionConstants.BillingProviders.Paddle,
                PlanId = request.PlanId,
                CheckoutUrl = checkoutUrl,
                ErrorCode = string.Empty,
                Message = GetCheckoutCreatedMessage(),
                CheckedAtUtc = DateTimeOffset.UtcNow
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Paddle checkout transaction creation timed out. PlanId={PlanId}; Environment={Environment}; UserId={UserId}.", request.PlanId, environment, request.UserId);
            return CreateFailedResult(request.PlanId);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Paddle checkout transaction creation HTTP request failed. PlanId={PlanId}; Environment={Environment}; UserId={UserId}.", request.PlanId, environment, request.UserId);
            return CreateFailedResult(request.PlanId);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Paddle checkout transaction response could not be parsed. PlanId={PlanId}; Environment={Environment}; UserId={UserId}.", request.PlanId, environment, request.UserId);
            return CreateUrlUnavailableResult(request.PlanId);
        }
    }

    private BillingProviderCheckoutResult CreateNotConfiguredResult(string planId, string message)
    {
        return new BillingProviderCheckoutResult
        {
            Created = false,
            CheckoutEnabled = false,
            Provider = SubscriptionConstants.BillingProviders.Paddle,
            PlanId = planId,
            CheckoutUrl = string.Empty,
            ErrorCode = SubscriptionConstants.Billing.PaddleCheckoutNotConfiguredCode,
            Message = message,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private BillingProviderCheckoutResult CreateFailedResult(string planId)
    {
        return new BillingProviderCheckoutResult
        {
            Created = false,
            CheckoutEnabled = false,
            Provider = SubscriptionConstants.BillingProviders.Paddle,
            PlanId = planId,
            CheckoutUrl = string.Empty,
            ErrorCode = SubscriptionConstants.Billing.PaddleCheckoutFailedCode,
            Message = SubscriptionConstants.Billing.PaddleCheckoutFailedMessage,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private BillingProviderCheckoutResult CreateUrlUnavailableResult(string planId)
    {
        return new BillingProviderCheckoutResult
        {
            Created = false,
            CheckoutEnabled = false,
            Provider = SubscriptionConstants.BillingProviders.Paddle,
            PlanId = planId,
            CheckoutUrl = string.Empty,
            ErrorCode = SubscriptionConstants.Billing.PaddleCheckoutUrlUnavailableCode,
            Message = SubscriptionConstants.Billing.PaddleCheckoutUrlUnavailableMessage,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private string BuildBackendHostedCheckoutLaunchUrl(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(options.ClientSideToken))
        {
            return string.Empty;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return string.Empty;
        }

        var request = httpContext.Request;
        var route = string.Concat(
            request.Scheme,
            "://",
            request.Host.ToUriComponent(),
            request.PathBase.ToUriComponent(),
            ApiConstants.PaddleCheckoutLaunchRoute);

        return AppendQueryParameter(
            new Uri(route),
            SubscriptionConstants.Billing.PaddleCheckoutLaunchTransactionIdParameterName,
            transactionId.Trim());
    }

    private static string AppendQueryParameter(Uri uri, string name, string value)
    {
        var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
        return string.Concat(uri.GetLeftPart(UriPartial.Path), uri.Query, separator, Uri.EscapeDataString(name), "=", Uri.EscapeDataString(value));
    }

    private string GetNormalizedEnvironment()
    {
        var environment = string.IsNullOrWhiteSpace(options.Environment)
            ? SubscriptionConstants.Billing.DefaultPaddleEnvironment
            : options.Environment.Trim().ToLowerInvariant();

        return string.Equals(environment, SubscriptionConstants.Billing.LivePaddleEnvironment, StringComparison.OrdinalIgnoreCase)
            ? SubscriptionConstants.Billing.LivePaddleEnvironment
            : SubscriptionConstants.Billing.DefaultPaddleEnvironment;
    }

    private string GetBaseUrl(string environment)
    {
        return string.Equals(environment, SubscriptionConstants.Billing.LivePaddleEnvironment, StringComparison.OrdinalIgnoreCase)
            ? options.LiveBaseUrl
            : options.SandboxBaseUrl;
    }

    private string GetApiVersion()
    {
        return string.IsNullOrWhiteSpace(options.ApiVersion)
            ? SubscriptionConstants.Billing.PaddleApiVersion
            : options.ApiVersion.Trim();
    }

    private string GetCheckoutCreatedMessage()
    {
        return string.IsNullOrWhiteSpace(options.CheckoutCreatedMessage)
            ? SubscriptionConstants.Billing.PaddleCheckoutCreatedMessage
            : options.CheckoutCreatedMessage.Trim();
    }

    private static string GetHeaderValue(HttpResponseMessage response, string headerName)
    {
        if (response.Headers.TryGetValues(headerName, out var values))
        {
            return values.FirstOrDefault() ?? string.Empty;
        }

        return string.Empty;
    }

    private static PaddleCheckoutResponse ParsePaddleCheckoutResponse(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        if (!root.TryGetProperty("data", out var data))
        {
            return new PaddleCheckoutResponse(string.Empty, string.Empty);
        }

        var transactionId = data.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String
            ? idElement.GetString() ?? string.Empty
            : string.Empty;

        var checkoutUrl = string.Empty;
        if (data.TryGetProperty("checkout", out var checkout)
            && checkout.TryGetProperty("url", out var urlElement)
            && urlElement.ValueKind == JsonValueKind.String)
        {
            checkoutUrl = urlElement.GetString() ?? string.Empty;
        }

        return new PaddleCheckoutResponse(transactionId, checkoutUrl);
    }

    private sealed record PaddleCheckoutResponse(string TransactionId, string CheckoutUrl);
}
