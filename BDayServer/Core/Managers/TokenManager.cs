using System.Security.Cryptography;
using System.Text;
using Contracts.DataTransferObjects.Auth;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Core.Services.Interfaces;
using Entities.Models;
using Microsoft.IdentityModel.Tokens;
using Identity = Microsoft.AspNetCore.Identity;

namespace Core.Managers;

public class TokenManager(Identity.UserManager<User> userManager, IAuthenticationService authenticationService)
    : ITokenManager
{
    public const string InvalidRequestMessage = "Invalid client request";

    /// <summary>
    /// Issues a new access token and rotates the refresh token. Every invalid request (bad or foreign access token,
    /// wrong, revoked or expired refresh token, locked-out user) fails with the same <see cref="TokenOperationException"/>.
    /// </summary>
    public async Task<(string, string)> Refresh(RefreshTokenDto tokenDto)
    {
        ArgumentNullException.ThrowIfNull(tokenDto);

        var userName = ReadUserName(tokenDto.Token);
        var user = userName is null ? null : await userManager.FindByEmailAsync(userName);

        if (user is null ||
            string.IsNullOrEmpty(user.RefreshToken) ||
            !FixedTimeEquals(user.RefreshToken, tokenDto.RefreshToken) ||
            user.RefreshTokenExpiryTime <= DateTime.UtcNow ||
            await userManager.IsLockedOutAsync(user))
            throw new TokenOperationException(InvalidRequestMessage);

        var token = await authenticationService.GetToken(user);
        user.RefreshToken = authenticationService.GenerateRefreshToken();

        // Fails e.g. when a concurrent refresh changed the user first; the new tokens must not be handed out then.
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new TokenOperationException(InvalidRequestMessage);

        return (token, user.RefreshToken);
    }

    private string? ReadUserName(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        try
        {
            return authenticationService.GetPrincipalFromExpiredToken(accessToken).Identity?.Name;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            // Malformed, wrongly signed or foreign token.
            return null;
        }
    }

    private static bool FixedTimeEquals(string expected, string? actual) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(actual ?? string.Empty));
}
