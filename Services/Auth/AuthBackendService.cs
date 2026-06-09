using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models.Auth;

namespace EnglishVoiceTutor.Desktop.Services.Auth;

public sealed class AuthBackendService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const int MaxSafeAuthErrorLength = 180;
    private readonly AuthSessionStorageService sessionStorageService;
    private string backendBaseUrl = BackendConstants.DefaultBackendBaseUrl;

    public AuthBackendService(AuthSessionStorageService? sessionStorageService = null)
    {
        this.sessionStorageService = sessionStorageService ?? new AuthSessionStorageService();
    }

    public string EffectiveBackendBaseUrl => BackendEndpointBuilder.NormalizeBaseUrl(backendBaseUrl);

    public string LastErrorCategory { get; private set; } = "none";

    public HttpStatusCode? LastStatusCode { get; private set; }

    public void SetBackendBaseUrl(string? value)
    {
        backendBaseUrl = BackendEndpointBuilder.NormalizeBaseUrl(value);
    }

    public Task<AuthOperationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return AuthenticateAsync(BackendConstants.AuthRegisterEndpoint, request, cancellationToken);
    }

    public Task<AuthOperationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return AuthenticateAsync(BackendConstants.AuthLoginEndpoint, request, cancellationToken);
    }

    public async Task<PasswordOperationResult> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostPasswordOperationAsync(BackendConstants.AuthPasswordResetRequestEndpoint, request, accessToken: null, cancellationToken);
    }

    public async Task<PasswordOperationResult> ConfirmPasswordResetAsync(PasswordResetConfirmRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostPasswordOperationAsync(BackendConstants.AuthPasswordResetConfirmEndpoint, request, accessToken: null, cancellationToken);
    }

    public async Task<PasswordOperationResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var session = await sessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
        if (session is null)
        {
            return PasswordOperationResult.Unauthorized();
        }

        return await PostPasswordOperationAsync(BackendConstants.AuthChangePasswordEndpoint, request, session.AccessToken, cancellationToken);
    }

    public async Task<AuthMeResult> GetMeAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return AuthMeResult.InvalidSession();
        }

        using var httpClient = CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, BackendConstants.AuthMeEndpoint));
        AuthenticatedRequestHelper.AddBearerTokenIfPresent(request, accessToken);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await sessionStorageService.ClearAsync(cancellationToken);
                return AuthMeResult.InvalidSession();
            }

            if (!response.IsSuccessStatusCode)
            {
                return AuthMeResult.BackendUnavailable();
            }

            var user = await response.Content.ReadFromJsonAsync<AuthUserDto>(JsonOptions, cancellationToken);
            return user is null ? AuthMeResult.BackendUnavailable() : AuthMeResult.Success(user);
        }
        catch
        {
            return AuthMeResult.BackendUnavailable();
        }
    }

    public Task<StoredAuthSession?> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        return sessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
    }

    public Task<bool> HasStoredSessionAsync(CancellationToken cancellationToken = default)
    {
        return sessionStorageService.HasStoredSessionAsync(cancellationToken);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        return sessionStorageService.ClearAsync(cancellationToken);
    }


    private async Task<PasswordOperationResult> PostPasswordOperationAsync<TRequest>(string endpointPath, TRequest requestBody, string? accessToken, CancellationToken cancellationToken)
    {
        using var httpClient = CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpointPath))
        {
            Content = JsonContent.Create(requestBody, options: JsonOptions)
        };
        AuthenticatedRequestHelper.AddBearerTokenIfPresent(request, accessToken);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var message = await TryReadPasswordOperationMessageAsync(response, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return PasswordOperationResult.Success(message);
            }

            if ((int)response.StatusCode >= 500)
            {
                return PasswordOperationResult.BackendUnavailable();
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized && string.IsNullOrWhiteSpace(message))
            {
                return PasswordOperationResult.Unauthorized();
            }

            return PasswordOperationResult.Failed(string.IsNullOrWhiteSpace(message) ? response.ReasonPhrase ?? string.Empty : message);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return PasswordOperationResult.BackendUnavailable();
        }
    }

    private async Task<AuthOperationResult> AuthenticateAsync<TRequest>(string endpointPath, TRequest requestBody, CancellationToken cancellationToken)
    {
        using var httpClient = CreateHttpClient();

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                BackendEndpointBuilder.BuildEndpointUri(backendBaseUrl, endpointPath),
                requestBody,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var safeMessage = await TryReadAuthErrorMessageAsync(response, cancellationToken);
                var failure = AuthOperationResult.FromHttpFailure(response.StatusCode, safeMessage);
                RecordAuthFailure(failure);
                return failure;
            }

            var payload = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions, cancellationToken);
            if (payload is null)
            {
                var failure = AuthOperationResult.UnexpectedResponse();
                RecordAuthFailure(failure);
                return failure;
            }

            var storedSession = new StoredAuthSession
            {
                AccessToken = payload.AccessToken,
                TokenType = payload.TokenType,
                ExpiresAtUtc = payload.ExpiresAtUtc,
                User = payload.User
            };

            if (AuthSessionStorageService.IsExpired(storedSession))
            {
                await sessionStorageService.ClearAsync(cancellationToken);
                var failure = AuthOperationResult.UnexpectedResponse();
                RecordAuthFailure(failure);
                return failure;
            }

            await sessionStorageService.SaveAsync(storedSession, cancellationToken);
            LastErrorCategory = "none";
            LastStatusCode = null;
            return AuthOperationResult.Success(payload);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            var failure = AuthOperationResult.NetworkUnavailable();
            RecordAuthFailure(failure);
            return failure;
        }
        catch (JsonException)
        {
            var failure = AuthOperationResult.UnexpectedResponse();
            RecordAuthFailure(failure);
            return failure;
        }
    }

    private static async Task<string> TryReadPasswordOperationMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<PasswordResetResponse>(JsonOptions, cancellationToken);
            return payload?.Message ?? string.Empty;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return string.Empty;
        }
    }

    private static async Task<string> TryReadAuthErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<AuthErrorResponse>(JsonOptions, cancellationToken);
            return SanitizeSafeMessage(payload?.Error ?? payload?.Title ?? payload?.Detail ?? string.Empty);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return string.Empty;
        }
    }

    private void RecordAuthFailure(AuthOperationResult result)
    {
        LastErrorCategory = result.ErrorCategory;
        LastStatusCode = result.StatusCode;
    }

    private static string SanitizeSafeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var safeMessage = message.ReplaceLineEndings(" ").Trim();
        if (safeMessage.Length > MaxSafeAuthErrorLength)
        {
            safeMessage = string.Concat(safeMessage.AsSpan(0, MaxSafeAuthErrorLength), "...");
        }

        return safeMessage;
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(BackendConstants.AuthRequestTimeoutSeconds)
        };

        httpClient.DefaultRequestHeaders.Add(
            BackendConstants.NgrokSkipBrowserWarningHeaderName,
            BackendConstants.NgrokSkipBrowserWarningHeaderValue);
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
            BackendConstants.BackendUserAgentProductName,
            BackendConstants.BackendUserAgentVersion));

        return httpClient;
    }
}

public enum AuthOperationResultStatus
{
    Success = 0,
    Failed = 1,
    BackendUnavailable = 2,
    UnexpectedResponse = 3
}

public sealed class AuthOperationResult
{
    private AuthOperationResult(AuthOperationResultStatus status, AuthResponse? response, string message, string errorCategory, HttpStatusCode? statusCode)
    {
        Status = status;
        Response = response;
        Message = message;
        ErrorCategory = errorCategory;
        StatusCode = statusCode;
    }

    public AuthOperationResultStatus Status { get; }
    public AuthResponse? Response { get; }
    public string Message { get; }
    public string ErrorCategory { get; }
    public HttpStatusCode? StatusCode { get; }

    public static AuthOperationResult Success(AuthResponse response) => new(AuthOperationResultStatus.Success, response, string.Empty, "none", null);
    public static AuthOperationResult Failed(string message) => new(AuthOperationResultStatus.Failed, null, message, "validation", null);
    public static AuthOperationResult BackendUnavailable() => NetworkUnavailable();
    public static AuthOperationResult NetworkUnavailable() => new(AuthOperationResultStatus.BackendUnavailable, null, string.Empty, "network", null);
    public static AuthOperationResult UnexpectedResponse() => new(AuthOperationResultStatus.UnexpectedResponse, null, string.Empty, "unexpected_response", null);

    public static AuthOperationResult FromHttpFailure(HttpStatusCode statusCode, string message)
    {
        var category = statusCode switch
        {
            HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity => "validation",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "authorization",
            _ when (int)statusCode >= 500 => "server_error",
            _ => "http_error"
        };

        var status = (int)statusCode >= 500
            ? AuthOperationResultStatus.BackendUnavailable
            : AuthOperationResultStatus.Failed;
        return new AuthOperationResult(status, null, message, category, statusCode);
    }
}

public sealed class AuthErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public enum AuthMeResultStatus
{
    Success = 0,
    InvalidSession = 1,
    BackendUnavailable = 2
}

public sealed class AuthMeResult
{
    private AuthMeResult(AuthMeResultStatus status, AuthUserDto? user)
    {
        Status = status;
        User = user;
    }

    public AuthMeResultStatus Status { get; }
    public AuthUserDto? User { get; }

    public static AuthMeResult Success(AuthUserDto user) => new(AuthMeResultStatus.Success, user);
    public static AuthMeResult InvalidSession() => new(AuthMeResultStatus.InvalidSession, null);
    public static AuthMeResult BackendUnavailable() => new(AuthMeResultStatus.BackendUnavailable, null);
}

public enum PasswordOperationResultStatus
{
    Success = 0,
    Failed = 1,
    Unauthorized = 2,
    BackendUnavailable = 3
}

public sealed class PasswordOperationResult
{
    private PasswordOperationResult(PasswordOperationResultStatus status, string message)
    {
        Status = status;
        Message = message;
    }

    public PasswordOperationResultStatus Status { get; }
    public string Message { get; }
    public bool IsSuccess => Status == PasswordOperationResultStatus.Success;

    public static PasswordOperationResult Success(string message) => new(PasswordOperationResultStatus.Success, message);
    public static PasswordOperationResult Failed(string message) => new(PasswordOperationResultStatus.Failed, message);
    public static PasswordOperationResult Unauthorized() => new(PasswordOperationResultStatus.Unauthorized, string.Empty);
    public static PasswordOperationResult BackendUnavailable() => new(PasswordOperationResultStatus.BackendUnavailable, string.Empty);
}
