namespace Entities.Configuration;

public class JwtConfiguration
{
    public string SecurityKey { get; set; } = string.Empty;

    public string ValidIssuer { get; set; } = string.Empty;

    public string ValidAudience { get; set; } = string.Empty;

    public int ExpiryInMinutes { get; set; }
}