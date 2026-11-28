using BDayServer.Extensions;
using Contracts.DataTransferObjects.Auth;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BDayServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting(RateLimitPolicies.TokenRefresh)]
public class TokenController(ITokenManager tokenManager) : ControllerBase
{
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto tokenDto)
    {
        try
        {
            var (token, refreshToken) = await tokenManager.Refresh(tokenDto);

            return Ok(new AuthResponseDto
            {
                IsAuthSuccessful = true,
                Token = token,
                RefreshToken = refreshToken
            });
        }
        catch (TokenOperationException ex)
        {
            return BadRequest(new AuthResponseDto
            {
                IsAuthSuccessful = false,
                ErrorMessage = ex.Message
            });
        }
    }

    /// <summary>
    /// Logout: ends the login the refresh token belongs to; the user's other devices stay logged in. Always 204, so the
    /// answer doesn't tell whether a token was valid.
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenDto revokeTokenDto)
    {
        await tokenManager.Revoke(revokeTokenDto);

        return NoContent();
    }
}
