using BDayServer.ActionFilters;
using Contracts.Exceptions;
using Contracts.Managers;
using Entities.DataTransferObjects.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SwaggerLoginController(ISwaggerLoginManager swaggerLoginManager)
    : ControllerBase
{
    [HttpPost("login")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> Login(SwaggerLoginDto swaggerLoginDto)
    {
        string? jwtSecurityToken;
        try
        {
            jwtSecurityToken = await swaggerLoginManager.Login(swaggerLoginDto);
        }
        catch (SwaggerLoginAuthenticationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return BadRequest("An error occurred in generating the token");
        }

        return Ok(jwtSecurityToken);
    }
}