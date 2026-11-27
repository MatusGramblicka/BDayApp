using BDayClient.AuthProvider;
using BDayClient.Features;

namespace BDayClient.HttpRepository;

/// <summary>
/// Hands out the access token for API calls and refreshes it shortly before it expires. A singleton (see Program.cs):
/// the server rotates the refresh token on every refresh, so all requests must share one refresh at a time.
/// </summary>
public class RefreshTokenService(TokenStorage tokenStorage, IAuthenticationService authService)
{
    public static readonly TimeSpan RefreshBeforeExpiry = TimeSpan.FromMinutes(2);

    private readonly object _gate = new();
    private Task<string>? _refresh;
    private TokenExpiry? _lastToken;

    /// <summary>
    /// The access token to send: the stored one, or a new one when it expires within <see cref="RefreshBeforeExpiry"/>.
    /// Empty when the user is not logged in (or the refresh failed, which logs the user out).
    /// </summary>
    public async Task<string> GetAccessTokenAsync()
    {
        var token = await tokenStorage.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
            return string.Empty;

        var expiry = GetExpiry(token);
        if (expiry is null || expiry.Value - DateTimeOffset.UtcNow > RefreshBeforeExpiry)
            return token;

        return await RefreshOnceAsync();
    }

    // Requests arriving while a refresh is running wait for that refresh instead of starting a second one, which the
    // server would reject because the first one has already used up the refresh token.
    private Task<string> RefreshOnceAsync()
    {
        lock (_gate)
        {
            if (_refresh is { IsCompleted: false } running)
                return running;

            _refresh = authService.RefreshToken();
            return _refresh;
        }
    }

    private DateTimeOffset? GetExpiry(string token)
    {
        var last = _lastToken;
        if (last is not null && last.Token == token)
            return last.Expiry;

        var expiry = JwtParser.GetExpiry(token);
        _lastToken = new TokenExpiry(token, expiry);
        return expiry;
    }

    private sealed record TokenExpiry(string Token, DateTimeOffset? Expiry);
}
