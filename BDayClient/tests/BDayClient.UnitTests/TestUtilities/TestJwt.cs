using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BDayClient.UnitTests.TestUtilities;

/// <summary>Builds unsigned JWTs - the client only decodes the payload, it never validates the signature.</summary>
internal static class TestJwt
{
    public static string Create(string? name = "john@test.com", object? roles = null, DateTimeOffset? expires = null)
    {
        var payload = new Dictionary<string, object>();

        if (name is not null)
            payload[ClaimTypes.Name] = name;
        if (roles is not null)
            payload[ClaimTypes.Role] = roles;

        payload["exp"] = (expires ?? DateTimeOffset.UtcNow.AddMinutes(30)).ToUnixTimeSeconds();
        payload["iss"] = "https://issuer.test";

        return $"{Encode(new { alg = "HS256", typ = "JWT" })}.{Encode(payload)}.signature";
    }

    private static string Encode(object value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
