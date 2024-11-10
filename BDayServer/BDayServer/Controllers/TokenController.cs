using BDayServer.Services;
using Entities;
using Entities.DataTransferObjects.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TokenController(UserManager<User> userManager, IAuthenticationService authenticationService)
    : ControllerBase
{
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenDto tokenDto)
    {
        if (tokenDto is null)
            return BadRequest(new AuthResponseDto
            {
                IsAuthSuccessful = false,
                ErrorMessage = "Invalid client request"
            });

        var principal = authenticationService
            .GetPrincipalFromExpiredToken(tokenDto.Token);
        var username = principal.Identity?.Name;

        var user = await userManager.FindByEmailAsync(username);
        if (user is null || user.RefreshToken != tokenDto.RefreshToken ||
            user.RefreshTokenExpiryTime <= DateTime.Now)
            return BadRequest(new AuthResponseDto
            {
                IsAuthSuccessful = false,
                ErrorMessage = "Invalid client request"
            });

        var token = await authenticationService.GetToken(user);
        user.RefreshToken = authenticationService.GenerateRefreshToken();

        await userManager.UpdateAsync(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            RefreshToken = user.RefreshToken,
            IsAuthSuccessful = true
        });
    }
}