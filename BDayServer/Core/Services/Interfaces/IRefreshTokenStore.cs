namespace Core.Services.Interfaces;

/// <summary>Refresh tokens, one per login (device or browser), stored as hashes.</summary>
public interface IRefreshTokenStore
{
    /// <summary>Starts a new login for the user and returns its refresh token.</summary>
    Task<string> IssueAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a valid refresh token of the user with a new one and returns it; null when the token is unknown,
    /// belongs to another user, has expired, was revoked, or was replaced longer ago than the grace period.
    /// </summary>
    Task<string?> RotateAsync(string userId, string presentedToken, CancellationToken cancellationToken = default);

    /// <summary>Ends the login the token belongs to (logout). Unknown tokens are ignored.</summary>
    Task RevokeAsync(string presentedToken, CancellationToken cancellationToken = default);

    /// <summary>Ends all logins of the user (e.g. after a password reset).</summary>
    Task RevokeAllAsync(string userId, CancellationToken cancellationToken = default);
}
