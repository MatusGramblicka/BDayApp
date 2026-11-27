using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.Features;

/// <summary>Reads the claims of a JWT. The client never validates the signature - the API does.</summary>
public static class JwtParser
{
    /// <summary>The claims of the token's payload; array values (e.g. several roles) become one claim per element.</summary>
    /// <exception cref="FormatException">The token is not a JWT with a base64url-encoded JSON object payload.</exception>
    public static IReadOnlyList<Claim> ParseClaimsFromJwt(string jwt)
    {
        using var payload = ParsePayload(jwt);

        var claims = new List<Claim>();
        foreach (var property in payload.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
                claims.AddRange(property.Value.EnumerateArray().Select(value => new Claim(property.Name, AsString(value))));
            else
                claims.Add(new Claim(property.Name, AsString(property.Value)));
        }

        return claims;
    }

    public static bool TryParseClaims(string jwt, [NotNullWhen(true)] out IReadOnlyList<Claim>? claims)
    {
        try
        {
            claims = ParseClaimsFromJwt(jwt);
            return true;
        }
        catch (FormatException)
        {
            claims = null;
            return false;
        }
    }

    /// <summary>The token's expiry (the "exp" claim), or null when the token has none or cannot be read.</summary>
    public static DateTimeOffset? GetExpiry(string jwt)
    {
        try
        {
            using var payload = ParsePayload(jwt);
            if (!payload.RootElement.TryGetProperty("exp", out var exp))
                return null;

            long? seconds = exp.ValueKind switch
            {
                JsonValueKind.Number when exp.TryGetInt64(out var value) => value,
                JsonValueKind.String when long.TryParse(exp.GetString(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var value) => value,
                _ => null
            };

            return seconds is null ? null : DateTimeOffset.FromUnixTimeSeconds(seconds.Value);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static JsonDocument ParsePayload(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            throw new FormatException("The token is not a JWT.");

        try
        {
            // The payload is base64url (RFC 7515): '-' and '_' instead of '+' and '/', and no padding.
            var document = JsonDocument.Parse(WebEncoders.Base64UrlDecode(parts[1]));
            if (document.RootElement.ValueKind == JsonValueKind.Object)
                return document;

            document.Dispose();
            throw new FormatException("The token payload is not a JSON object.");
        }
        catch (JsonException ex)
        {
            throw new FormatException("The token payload is not JSON.", ex);
        }
    }

    private static string AsString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Null => string.Empty,
        _ => value.GetRawText()
    };
}
