using Contracts.DataTransferObjects.Auth;
using Contracts.Exceptions;
using Entities.Models;
using Interfaces;
using Interfaces.Authentication;
using Interfaces.Managers;
using Identity = Microsoft.AspNetCore.Identity;

namespace Core.Managers;

public class TokenManager(Identity.UserManager<User> userManager, IAuthenticationService authenticationService)
    : ITokenManager
{
    public async Task<(string, string)> Refresh(RefreshTokenDto tokenDto)
    {
        ArgumentNullException.ThrowIfNull(tokenDto, nameof(tokenDto));

        var principal = authenticationService
            .GetPrincipalFromExpiredToken(tokenDto.Token);
        var username = principal.Identity?.Name;

        ArgumentNullException.ThrowIfNull(username, nameof(username));

        var user = await userManager.FindByEmailAsync(username);
        if (user is null || user.RefreshToken != tokenDto.RefreshToken ||
            user.RefreshTokenExpiryTime <= DateTime.Now)
            throw new TokenOperationException("Invalid client request");

        var token = await authenticationService.GetToken(user);
        user.RefreshToken = authenticationService.GenerateRefreshToken();

        await userManager.UpdateAsync(user);

        return (token, user.RefreshToken);
    }
}