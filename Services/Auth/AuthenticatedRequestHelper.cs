using System.Net.Http;
using System.Net.Http.Headers;

namespace EnglishVoiceTutor.Desktop.Services.Auth;

public static class AuthenticatedRequestHelper
{
    public static void AddBearerTokenIfPresent(HttpRequestMessage request, string? accessToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
