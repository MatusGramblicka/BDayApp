using System.Security.Cryptography;
using System.Text;
using Core.Services.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Repositories;

namespace Core.Services;

public class RefreshTokenStore(DbRepositoryContext db, TimeProvider timeProvider) : IRefreshTokenStore
{
    /// <summary>How long a login lasts. Refreshing does not extend it.</summary>
    public static readonly TimeSpan LoginLifetime = TimeSpan.FromDays(7);

    /// <summary>
    /// A replaced token is still accepted this long, so that two tabs of one browser (which share the stored token)
    /// can refresh at the same moment without logging each other out.
    /// </summary>
    public static readonly TimeSpan ReplacedTokenGracePeriod = TimeSpan.FromMinutes(1);

    public async Task<string> IssueAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = Now;

        // Housekeeping: forget this user's logins that have ended.
        var ended = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.ExpiresAt <= now)
            .ToListAsync(cancellationToken);
        db.RefreshTokens.RemoveRange(ended);

        var token = Add(userId, now, now + LoginLifetime);
        await db.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task<string?> RotateAsync(string userId, string presentedToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(presentedToken))
            return null;

        var now = Now;
        var hash = Hash(presentedToken);
        var current = await db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId, cancellationToken);

        if (current is null ||
            current.RevokedAt is not null ||
            current.ExpiresAt <= now ||
            (current.ReplacedAt is not null && now - current.ReplacedAt.Value > ReplacedTokenGracePeriod))
            return null;

        current.ReplacedAt ??= now;
        var token = Add(userId, now, current.ExpiresAt);
        await db.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task RevokeAsync(string presentedToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(presentedToken))
            return;

        var hash = Hash(presentedToken);
        var token = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null || token.RevokedAt is not null)
            return;

        token.RevokedAt = Now;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = Now;
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
            token.RevokedAt = now;

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Only this hash is stored. The tokens are 256 random bits, so a plain SHA-256 is enough: there is nothing to
    /// guess that salting or a slow hash would protect.
    /// </summary>
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private string Add(string userId, DateTime now, DateTime expiresAt)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(token),
            CreatedAt = now,
            ExpiresAt = expiresAt
        });

        return token;
    }

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;
}
