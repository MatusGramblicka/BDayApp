using Microsoft.AspNetCore.Identity;

namespace Entities.Models;

public class User : IdentityUser
{
    // Legacy, no longer read or written: refresh tokens are now one per login, hashed, in RefreshTokens.
    // The columns stay until this version runs in production (the previous version still writes them during a
    // deployment); a later migration can drop them. NOT NULL in the database, hence the default.
    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshTokenExpiryTime { get; set; }

    public bool IsAdmin { get; set; }
}
