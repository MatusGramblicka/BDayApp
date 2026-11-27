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
}
