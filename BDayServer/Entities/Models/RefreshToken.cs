namespace Entities.Models;

/// <summary>
/// One login (device or browser) of a user. Only a SHA-256 hash of the token is stored, so the database alone gives
/// nobody a usable token. Every refresh replaces the token with a new one (rotation); the login itself ends at
/// <see cref="ExpiresAt"/>, however often it is refreshed.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Upper-case hex SHA-256 of the token (64 characters).</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>End of the login; a refresh keeps it.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>When a refresh replaced this token. It is still accepted for a short grace period after that.</summary>
    public DateTime? ReplacedAt { get; set; }

    /// <summary>When the login was ended (logout or password reset); never accepted again.</summary>
    public DateTime? RevokedAt { get; set; }
}
