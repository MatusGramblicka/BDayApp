using System.ComponentModel.DataAnnotations;
using BDayServer.Extensions;
using BDayServer.Security;
using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BDayServer.Controllers;

// Missing or invalid bodies are answered with 400 validation problem details by [ApiController]; unexpected errors
// are logged and answered with 500 problem details by the exception handler (Program.cs).
[Route("api/account")]
[ApiController]
[EnableRateLimiting(RateLimitPolicies.Account)]
public class AccountController(IAccountManager accountManager, ClientUriValidator clientUriValidator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userForRegistrationDto)
    {
        if (!clientUriValidator.IsAllowed(userForRegistrationDto.ClientUri, Request))
            return InvalidClientUri();

        try
        {
            await accountManager.RegisterUser(userForRegistrationDto);
        }
        catch (AccountManagerErrorsException ex)
        {
            return BadRequest(new ResponseDto { Errors = ex.Errors });
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserForAuthenticationDto userForAuthenticationDto)
    {
        try
        {
            return Ok(await accountManager.Login(userForAuthenticationDto));
        }
        catch (AccountManagerUnauthorizedLoginException ex)
        {
            return Unauthorized(new AuthResponseDto { ErrorMessage = ex.Message });
        }
    }

    [HttpPost("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        if (!clientUriValidator.IsAllowed(forgotPasswordDto.ClientUri, Request))
            return InvalidClientUri();

        // 204 whether or not the address belongs to an account, so that the form doesn't reveal who is registered.
        await accountManager.ForgotPassword(forgotPasswordDto);

        return NoContent();
    }

    [HttpPost("ResetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        try
        {
            await accountManager.ResetPassword(resetPasswordDto);
        }
        catch (AccountManagerErrorsException ex)
        {
            return BadRequest(new ResetPasswordResponseDto { Errors = ex.Errors });
        }

        return NoContent();
    }

    [HttpGet("EmailConfirmation")]
    public async Task<IActionResult> EmailConfirmation([FromQuery, Required] string email,
        [FromQuery, Required] string token)
    {
        try
        {
            await accountManager.EmailConfirmation(email, token);
        }
        catch (AccountManagerUnauthorizedLoginException ex)
        {
            return BadRequest(ex.Message);
        }

        return NoContent();
    }

    [HttpPost("TwoStepVerification")]
    public async Task<IActionResult> TwoStepVerification(
        [FromBody] TwoFactorVerificationDto twoFactorVerificationDto)
    {
        try
        {
            return Ok(await accountManager.TwoStepVerification(twoFactorVerificationDto));
        }
        catch (AccountManagerUnauthorizedLoginException ex)
        {
            return Unauthorized(new AuthResponseDto { ErrorMessage = ex.Message });
        }
    }

    private ActionResult InvalidClientUri()
    {
        ModelState.AddModelError(nameof(UserForRegistrationDto.ClientUri),
            "The link in the e-mail must point to this application.");

        return ValidationProblem(ModelState);
    }
}
