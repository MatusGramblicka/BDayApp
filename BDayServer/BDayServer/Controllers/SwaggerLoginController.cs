using BDayServer.ActionFilters;
using Contracts.DataTransferObjects.Auth;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
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
        if (swaggerLoginDto is null)
            return BadRequest("Object is null");

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