using Contracts.Exceptions;
using Contracts.Managers;
using Core.Services;
using EmailService.Contracts;
using EmailService.Contracts.Models;
using Entities;
using Entities.DataTransferObjects.Auth;
using Entities.DataTransferObjects.User;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Sockets;
using System.Security.Principal;
using Identity = Microsoft.AspNetCore.Identity;

namespace Core.Managers;

public class AccountManager(Identity.UserManager<User> userManager,
    IAuthenticationService authenticationService,
    IEmailSender emailSender) : IAccountManager
{
    public async Task RegisterUser(UserForRegistrationDto userForRegistrationDto)
    {
        ArgumentNullException.ThrowIfNull(userForRegistrationDto, nameof(userForRegistrationDto));
        
        var user = new User
        {
            UserName = userForRegistrationDto.Email,
            Email = userForRegistrationDto.Email
        };

        var result = await userManager.CreateAsync(user, userForRegistrationDto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            throw new AccountManagerErrorsException(errors);
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var param = new Dictionary<string, string>
        {
            {"token", token},
            {"email", userForRegistrationDto.Email}
        };

        var callback = QueryHelpers.AddQueryString(userForRegistrationDto.ClientUri, param);

        var message = new Message(new[] { user.Email }, "Email confirmation token",
            callback, null);

        await emailSender.SendEmailAsync(message);

        await userManager.AddToRoleAsync(user, "Viewer");
    }

    public async Task<AuthResponseDto> Login(UserForAuthenticationDto userForAuthenticationDto)
    {
        var user = await userManager.FindByNameAsync(userForAuthenticationDto.Email);

        if (user is null)
            throw new AccountManagerUnauthorizedLoginException("Invalid Request");


        if (!await userManager.IsEmailConfirmedAsync(user))
            throw new AccountManagerUnauthorizedLoginException("Email is not confirmed");
       
        if (await userManager.IsLockedOutAsync(user))
            throw new AccountManagerUnauthorizedLoginException("The account is locked out");

        if (!await userManager.CheckPasswordAsync(user, userForAuthenticationDto.Password))
        {
            await userManager.AccessFailedAsync(user);

            if (await userManager.IsLockedOutAsync(user))
            {
                const string content = $"Your account is locked out. " +
                                       $"If you want to reset the password, you can use the " +
                                       $"Forgot Password link on the Login page";

                var message = new Message(new[] {userForAuthenticationDto.Email},
                    "Locked out account information", content, null);

                await emailSender.SendEmailAsync(message);

                throw new AccountManagerUnauthorizedLoginException("The account is locked out");
            }

            throw new AccountManagerUnauthorizedLoginException("Invalid Authentication");
        }

        if (await userManager.GetTwoFactorEnabledAsync(user))
            return await GenerateOtpFor2StepVerification(user);

        var token = await authenticationService.GetToken(user);
        //await _userManager.AddToRoleAsync(user, "Administrator");
        user.RefreshToken = authenticationService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);

        await userManager.UpdateAsync(user);
        await userManager.ResetAccessFailedCountAsync(user);

        return new AuthResponseDto
        {
            IsAuthSuccessful = true,
            Token = token,
            RefreshToken = user.RefreshToken
        };
    }

    public async Task<> ForgotPassword(
         ForgotPasswordDto forgotPasswordDto)
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

        var message = new Message(new[] { user.Email }, "Reset password token",
            callback, null);

        await emailSender.SendEmailAsync(message);

        return Ok();
    }

    public async Task<> ResetPassword(
         ResetPasswordDto resetPasswordDto)
    {
        var errorResponse = new ResetPasswordResponseDto
        {
            Errors = new[] { "Reset Password Failed" }
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
            return BadRequest(new ResetPasswordResponseDto { Errors = errors });
        }

        await userManager.SetLockoutEndDateAsync(user, null);

        return Ok(new ResetPasswordResponseDto { IsResetPasswordSuccessful = true });
    }

    public async Task<> EmailConfirmation([FromQuery] string email,
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

 public async Task<> TwoStepVerification(
         TwoFactorVerificationDto twoFactorVerificationDto)
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

    private async Task<> GenerateOtpFor2StepVerification(User user)
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

        var message = new Message(new[] { user.Email }, "Authentication token",
            token, null);

        await emailSender.SendEmailAsync(message);

        return Ok(new AuthResponseDto
        {
            Is2StepVerificationRequired = true,
            Provider = "Email"
        });
    }
}