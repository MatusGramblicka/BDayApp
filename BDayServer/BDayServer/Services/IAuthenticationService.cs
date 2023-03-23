using Entities;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BDayServer.Services
{
    public interface IAuthenticationService
    {
        Task<string> GetToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
