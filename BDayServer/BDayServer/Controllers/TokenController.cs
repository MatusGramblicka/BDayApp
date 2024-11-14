using BDayServer.ActionFilters;
using Contracts.Exceptions;
using Contracts.Managers;
using Entities.DataTransferObjects.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TokenController(ITokenManager tokenManager) : ControllerBase
{
    [HttpPost("refresh")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto tokenDto)
    {
        string token;
        string refreshToken;

        try
        {
            (token, refreshToken) = await tokenManager.Refresh(tokenDto);
        }
        catch (ArgumentNullException)
        {
            return BadRequest(new AuthResponseDto
            {
                IsAuthSuccessful = false,
                ErrorMessage = "Invalid client request"
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
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return Ok(new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            IsAuthSuccessful = true
        });
    }
}