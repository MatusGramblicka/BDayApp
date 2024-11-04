using BDayServer.Services;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Entities.DataTransferObjects;
using Entities.DataTransferObjects.Auth;

namespace BDayServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SwaggerLoginController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IAuthenticationService _authenticationService;

    public SwaggerLoginController(UserManager<User> userManager, IAuthenticationService authenticationService)
    {
        _userManager = userManager;
        _authenticationService = authenticationService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(SwaggerLoginDto swaggerloginDto)
    {
        try
        {
            if (string.IsNullOrEmpty(swaggerloginDto.Email) ||
                string.IsNullOrEmpty(swaggerloginDto.Password))
                return BadRequest("Username and/or Password not specified");

            var managedUser = await _userManager.FindByEmailAsync(swaggerloginDto.Email);
            if (managedUser is null)
                return BadRequest("Bad credentials");

            var isPasswordValid = await _userManager.CheckPasswordAsync(managedUser, swaggerloginDto.Password);
            if (!isPasswordValid)
                return BadRequest("Bad credentials");

            var jwtSecurityToken = await _authenticationService.GetToken(managedUser);
            return Ok(jwtSecurityToken);
        }
        catch
        {
            return BadRequest("An error occurred in generating the token");
        }
    }
}