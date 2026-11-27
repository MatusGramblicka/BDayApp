using Core.Services.Interfaces;
using Entities.Configuration;
using Entities.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity = Microsoft.AspNetCore.Identity;

namespace Core.Services;

public class AuthenticationService(IOptions<JwtConfiguration> jwtSettings, Identity.UserManager<User> userManager)
    : IAuthenticationService
{
    private readonly JwtConfiguration _jwtSettings = jwtSettings.Value;

    public async Task<string> GetToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Email ?? user.UserName ?? string.Empty)
        };

        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.ValidIssuer,
            audience: _jwtSettings.ValidAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
            signingCredentials: new SigningCredentials(CreateSigningKey(_jwtSettings), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var principal = new JwtSecurityTokenHandler().ValidateToken(token,
            CreateTokenValidationParameters(_jwtSettings, validateLifetime: false), out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }

    /// <summary>
    /// Validation of our access tokens; shared by the JWT bearer authentication (with the lifetime check) and the
    /// token refresh (which reads expired tokens, so without it).
    /// </summary>
    public static TokenValidationParameters CreateTokenValidationParameters(JwtConfiguration jwtSettings,
        bool validateLifetime) => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = validateLifetime,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.ValidIssuer,
        ValidAudience = jwtSettings.ValidAudience,
        IssuerSigningKey = CreateSigningKey(jwtSettings),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        // The default tolerance (5 minutes) would keep a token valid as long again as its lifetime.
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    private static SymmetricSecurityKey CreateSigningKey(JwtConfiguration jwtSettings) =>
        new(Encoding.UTF8.GetBytes(jwtSettings.SecurityKey));
}
