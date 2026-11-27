using Microsoft.AspNetCore.Identity;

namespace Entities.Models;

public class User : IdentityUser
{
    // Never null: the column is NOT NULL, and a user who has not logged in yet has no refresh token.
    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshTokenExpiryTime { get; set; }

    public bool IsAdmin { get; set; }
}
