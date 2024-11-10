using BDayServer.Services;
using Entities;
using Entities.DataTransferObjects.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SwaggerLoginController(UserManager<User> userManager, IAuthenticationService authenticationService)
    : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(SwaggerLoginDto swaggerLoginDto)
    {
        try
        {
            if (string.IsNullOrEmpty(swaggerLoginDto.Email) ||
                string.IsNullOrEmpty(swaggerLoginDto.Password))
                return BadRequest("Username and/or Password not specified");

            var managedUser = await userManager.FindByEmailAsync(swaggerLoginDto.Email);
            if (managedUser is null)
                return BadRequest("Bad credentials");

            var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, swaggerLoginDto.Password);
            if (!isPasswordValid)
                return BadRequest("Bad credentials");

            var jwtSecurityToken = await authenticationService.GetToken(managedUser);
            return Ok(jwtSecurityToken);
        }
        catch
        {
            return BadRequest("An error occurred in generating the token");
        }
    }
}