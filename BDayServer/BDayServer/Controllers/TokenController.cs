using BDayServer.Services;
using Entities;
using Entities.DataTransferObjects;
using Entities.DataTransferObjects.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TokenController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IAuthenticationService _authenticationService;

    public TokenController(UserManager<User> userManager,
        IAuthenticationService authenticationService)
    {
        _userManager = userManager;
        _authenticationService = authenticationService;
    }

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

        var principal = _authenticationService
            .GetPrincipalFromExpiredToken(tokenDto.Token);
        var username = principal.Identity?.Name;

        var user = await _userManager.FindByEmailAsync(username);
        if (user is null || user.RefreshToken != tokenDto.RefreshToken ||
            user.RefreshTokenExpiryTime <= DateTime.Now)
            return BadRequest(new AuthResponseDto
            {
                IsAuthSuccessful = false,
                ErrorMessage = "Invalid client request"
            });

        var token = await _authenticationService.GetToken(user);
        user.RefreshToken = _authenticationService.GenerateRefreshToken();

        await _userManager.UpdateAsync(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            RefreshToken = user.RefreshToken,
            IsAuthSuccessful = true
        });
    }
}