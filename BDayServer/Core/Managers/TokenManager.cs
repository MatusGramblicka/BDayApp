using Contracts.DataTransferObjects.Auth;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Core.Services.Interfaces;
using Entities.Models;
using Microsoft.IdentityModel.Tokens;
using Identity = Microsoft.AspNetCore.Identity;

namespace Core.Managers;

public class TokenManager(
    Identity.UserManager<User> userManager,
    IAuthenticationService authenticationService,
    IRefreshTokenStore refreshTokens) : ITokenManager
{
    public const string InvalidRequestMessage = "Invalid client request";

    /// <summary>
    /// Issues a new access token and replaces the refresh token of this login. Every invalid request (bad or foreign
    /// access token, unknown, revoked or expired refresh token, locked-out user) fails with the same
    /// <see cref="TokenOperationException"/>.
    /// </summary>
    public async Task<(string, string)> Refresh(RefreshTokenDto tokenDto)
    {
        ArgumentNullException.ThrowIfNull(tokenDto);

        var userName = ReadUserName(tokenDto.Token);
        var user = userName is null ? null : await userManager.FindByEmailAsync(userName);

        if (user is null || await userManager.IsLockedOutAsync(user))
            throw new TokenOperationException(InvalidRequestMessage);

        var refreshToken = await refreshTokens.RotateAsync(user.Id, tokenDto.RefreshToken)
                           ?? throw new TokenOperationException(InvalidRequestMessage);

        var token = await authenticationService.GetToken(user);

        return (token, refreshToken);
    }

    public Task Revoke(RevokeTokenDto revokeTokenDto)
    {
        ArgumentNullException.ThrowIfNull(revokeTokenDto);

        return refreshTokens.RevokeAsync(revokeTokenDto.RefreshToken);
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
}
