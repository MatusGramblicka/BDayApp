using System.Security.Claims;
using BDayClient.Features;
using Microsoft.AspNetCore.Components.Authorization;

namespace BDayClient.AuthProvider;

/// <summary>
/// The authentication state comes from the stored access token. An expired token still counts as logged in: the
/// <see cref="Http.AuthTokenHandler"/> refreshes it before the next API call, and logs the user out when that fails.
/// </summary>
public class AuthStateProvider(TokenStorage tokenStorage) : AuthenticationStateProvider
{
    private const string AuthenticationType = "jwtAuthType";

    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStorage.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var state = CreateState(token);
        if (state is not null)
            return state;

        // A damaged token would otherwise break the authentication state of the whole app until storage is cleared.
        await tokenStorage.ClearAsync();
        return Anonymous;
    }

    public void NotifyUserAuthentication(string token) =>
        NotifyAuthenticationStateChanged(Task.FromResult(CreateState(token) ?? Anonymous));

    public void NotifyUserLogout() => NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));

    private static AuthenticationState? CreateState(string token) =>
        JwtParser.TryParseClaims(token, out var claims)
            ? new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType)))
            : null;
}
