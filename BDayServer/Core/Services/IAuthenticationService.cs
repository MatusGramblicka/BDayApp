using System.Security.Claims;
using Entities;

namespace Core.Services;

public interface IAuthenticationService
{
    Task<string> GetToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}