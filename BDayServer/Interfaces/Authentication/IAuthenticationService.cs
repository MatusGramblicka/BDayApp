using System.Security.Claims;
using Entities.Models;

namespace Interfaces.Authentication;

public interface IAuthenticationService
{
    Task<string> GetToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}