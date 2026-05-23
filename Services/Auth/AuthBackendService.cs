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

    private readonly AuthSessionStorageService sessionStorageService;
    private string backendBaseUrl = BackendConstants.DefaultBackendBaseUrl;

    public AuthBackendService(AuthSessionStorageService? sessionStorageService = null)
    {
        this.sessionStorageService = sessionStorageService ?? new AuthSessionStorageService();
    }

    public void SetBackendBaseUrl(string? value)
    {
        backendBaseUrl = BackendEndpointBuilder.NormalizeBaseUrl(value);
    }

    public Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return AuthenticateAsync(BackendConstants.AuthRegisterEndpoint, request, cancellationToken);
    }

    public Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return AuthenticateAsync(BackendConstants.AuthLoginEndpoint, request, cancellationToken);
    }

    public async Task<AuthUserDto?> GetMeAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
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
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AuthUserDto>(JsonOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public Task<StoredAuthSession?> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        return sessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        return sessionStorageService.ClearAsync(cancellationToken);
    }

    private async Task<AuthResponse?> AuthenticateAsync<TRequest>(string endpointPath, TRequest requestBody, CancellationToken cancellationToken)
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
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions, cancellationToken);
            if (payload is null)
            {
                return null;
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
                return null;
            }

            await sessionStorageService.SaveAsync(storedSession, cancellationToken);
            return payload;
        }
        catch
        {
            return null;
        }
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
