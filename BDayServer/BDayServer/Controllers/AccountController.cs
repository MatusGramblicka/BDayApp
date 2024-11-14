using BDayServer.ActionFilters;
using Contracts.Exceptions;
using Contracts.Managers;
using Core.Services;
using EmailService.Contracts;
using EmailService.Contracts.Models;
using Entities;
using Entities.DataTransferObjects.Auth;
using Entities.DataTransferObjects.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayServer.Controllers;

[Route("api/account")]
[ApiController]
public class AccountController(
    UserManager<User> userManager,
    IAuthenticationService authenticationService,
    IEmailSender emailSender, IAccountManager AccountManager)
    : ControllerBase
{
    [HttpPost("register")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userForRegistrationDto)
    {
        try
        {
            await AccountManager.RegisterUser(userForRegistrationDto);
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
        AuthResponseDto authResponseDto;

        try
        {
            authResponseDto= await AccountManager.Login(userForAuthenticationDto);
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
        var user = await userManager.FindByEmailAsync(forgotPasswordDto.Email);
        if (user is null)
            return BadRequest("Invalid Request");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var param = new Dictionary<string, string>
        {
            {"token", token},
            {"email", forgotPasswordDto.Email}
        };

        var callback = QueryHelpers.AddQueryString(forgotPasswordDto.ClientUri, param);

        var message = new Message(new[] {user.Email}, "Reset password token",
            callback, null);

        await emailSender.SendEmailAsync(message);

        return Ok();
    }

    [HttpPost("ResetPassword")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto resetPasswordDto)
    {
        var errorResponse = new ResetPasswordResponseDto
        {
            Errors = new[] {"Reset Password Failed"}
        };

        if (!ModelState.IsValid)
            return BadRequest(errorResponse);

        var user = await userManager.FindByEmailAsync(resetPasswordDto.Email);
        if (user is null)
            return BadRequest(errorResponse);

        var resetPassResult = await userManager.ResetPasswordAsync(user,
            resetPasswordDto.Token, resetPasswordDto.Password);

        if (!resetPassResult.Succeeded)
        {
            var errors = resetPassResult.Errors.Select(e => e.Description);
            return BadRequest(new ResetPasswordResponseDto {Errors = errors});
        }

        await userManager.SetLockoutEndDateAsync(user, null);

        return Ok(new ResetPasswordResponseDto {IsResetPasswordSuccessful = true});
    }

    [HttpGet("EmailConfirmation")]
    public async Task<IActionResult> EmailConfirmation([FromQuery] string email,
        [FromQuery] string token)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return BadRequest();

        var confirmResult = await userManager.ConfirmEmailAsync(user, token);
        if (!confirmResult.Succeeded)
            return BadRequest();

        return Ok();
    }

    [HttpPost("TwoStepVerification")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> TwoStepVerification(
        [FromBody] TwoFactorVerificationDto twoFactorVerificationDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new AuthResponseDto
            {
                ErrorMessage = "Invalid Request"
            });

        var user = await userManager.FindByEmailAsync(twoFactorVerificationDto.Email);
        if (user is null)
            return BadRequest(new AuthResponseDto
            {
                ErrorMessage = "Invalid Request"
            });

        var validVerification = await userManager.VerifyTwoFactorTokenAsync(user,
            twoFactorVerificationDto.Provider, twoFactorVerificationDto.TwoFactorToken);
        if (!validVerification)
            return BadRequest(new AuthResponseDto
            {
                ErrorMessage = "Invalid Token Verification"
            });

        var token = await authenticationService.GetToken(user);
        user.RefreshToken = authenticationService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
        await userManager.UpdateAsync(user);
        await userManager.ResetAccessFailedCountAsync(user);

        return Ok(new AuthResponseDto
        {
            IsAuthSuccessful = true,
            Token = token,
            RefreshToken = user.RefreshToken
        });
    }

    private async Task<IActionResult> GenerateOtpFor2StepVerification(User user)
    {
        var providers = await userManager.GetValidTwoFactorProvidersAsync(user);
        if (!providers.Contains("Email"))
        {
            return Unauthorized(new AuthResponseDto
            {
                ErrorMessage = "Invalid 2-Step Verification Provider"
            });
        }

        var token = await userManager.GenerateTwoFactorTokenAsync(user, "Email");

        var message = new Message(new[] {user.Email}, "Authentication token",
            token, null);

        await emailSender.SendEmailAsync(message);

        return Ok(new AuthResponseDto
        {
            Is2StepVerificationRequired = true,
            Provider = "Email"
        });
    }
}