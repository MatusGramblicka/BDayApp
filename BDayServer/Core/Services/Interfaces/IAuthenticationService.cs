using System.Security.Claims;
using Entities.Models;

namespace Core.Services.Interfaces;

public interface IAuthenticationService
{
    Task<string> GetToken(User user);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}