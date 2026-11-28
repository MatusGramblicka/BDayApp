using System.Net.Http.Headers;
using BDayClient.HttpRepository;

namespace BDayClient.Http;

/// <summary>
/// Puts the access token on every API request except the anonymous account/ and token/ endpoints, refreshing the token
/// first when it is about to expire (see <see cref="RefreshTokenService"/>). Registered once on the API
/// <see cref="HttpClient"/> (Program.cs). The header is set on the request, never on HttpClient.DefaultRequestHeaders.
/// </summary>
public sealed class AuthTokenHandler(ApiUrls apiUrls, RefreshTokenService refreshTokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null &&
            apiUrls.IsApiRequest(request.RequestUri) &&
            !apiUrls.IsAuthEndpoint(request.RequestUri))
        {
            var accessToken = await refreshTokenService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(accessToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
