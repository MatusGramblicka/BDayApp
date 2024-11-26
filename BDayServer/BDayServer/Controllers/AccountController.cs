using BDayServer.ActionFilters;
using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Interfaces.Managers;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/account")]
[ApiController]
public class AccountController(IAccountManager accountManager) : ControllerBase
{
    [HttpPost("register")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userForRegistrationDto)
    {
        if (userForRegistrationDto is null)
            return BadRequest("Object is null");

        try
        {
            await accountManager.RegisterUser(userForRegistrationDto);
        }
        catch (AccountManagerErrorsException ex)
        {
            return BadRequest(new ResponseDto
            {
                Errors = ex.Errors
            });
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return StatusCode(201);
    }

    [HttpPost("login")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> Login([FromBody] UserForAuthenticationDto userForAuthenticationDto)
    {
        if (userForAuthenticationDto is null)
            return BadRequest("Object is null");

        AuthResponseDto authResponseDto;

        try
        {
            authResponseDto = await accountManager.Login(userForAuthenticationDto);
        }
        catch (AccountManagerUnauthorizedLoginException ex)
        {
            return Unauthorized(new AuthResponseDto
            {
                ErrorMessage = ex.Message
            });
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return Ok(authResponseDto);
    }

    [HttpPost("ForgotPassword")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        if (forgotPasswordDto is null)
            return BadRequest("Object is null");

        try
        {
            await accountManager.ForgotPassword(forgotPasswordDto);
        }
        catch (AccountManagerUnauthorizedLoginException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return NoContent();
    }

    [HttpPost("ResetPassword")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto resetPasswordDto)
    {
        if (resetPasswordDto is null)
            return BadRequest("Object is null");

        ResetPasswordResponseDto resetPasswordResponseDto;

        try
        {
            resetPasswordResponseDto = await accountManager.ResetPassword(resetPasswordDto);
        }
        catch (AccountManagerUnauthorizedLoginException ex)
        {
            return BadRequest(new AuthResponseDto
            {
                ErrorMessage = ex.Message
            });
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return Ok(resetPasswordResponseDto);
    }

    [HttpGet("EmailConfirmation")]
    public async Task<IActionResult> EmailConfirmation([FromQuery] string email,
        [FromQuery] string token)
    {
        try
        {
            await accountManager.EmailConfirmation(email, token);
        }
        catch (AccountManagerUnauthorizedLoginException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return NoContent();
    }

    [HttpPost("TwoStepVerification")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> TwoStepVerification(
        [FromBody] TwoFactorVerificationDto twoFactorVerificationDto)
    {
        if (twoFactorVerificationDto is null)
            return BadRequest("Object is null");

        AuthResponseDto authResponseDto;

        try
        {
            authResponseDto = await accountManager.TwoStepVerification(twoFactorVerificationDto);
        }
        catch (AccountManagerUnauthorizedLoginException ex)
        {
            return BadRequest(new AuthResponseDto
            {
                ErrorMessage = ex.Message
            });
        }
        catch (Exception e)
        {
            return BadRequest("Unspecified problem");
        }

        return Ok(authResponseDto);
    }
}