using Entities;
using System.Security.Claims;

namespace BDayServer.Services;

public interface IAuthenticationService
{
    Task<string> GetToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}