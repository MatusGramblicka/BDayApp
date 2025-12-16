using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Core.Services.Interfaces;
using EmailService.EmailServiceContracts;
using EmailService.Interfaces;
using Entities.Models;
using Microsoft.AspNetCore.WebUtilities;
using Identity = Microsoft.AspNetCore.Identity;

namespace Core.Managers;

public class AccountManager(
    Identity.UserManager<User> userManager,
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

        var message = new Message([user.Email ?? string.Empty], "Email confirmation token",
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

                var message = new Message([userForAuthenticationDto.Email],
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

    public async Task ForgotPassword(ForgotPasswordDto forgotPasswordDto)
    {
        var user = await userManager.FindByEmailAsync(forgotPasswordDto.Email);

        if (user is null)
            throw new AccountManagerUnauthorizedLoginException("Invalid Request");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var param = new Dictionary<string, string>
        {
            {"token", token},
            {"email", forgotPasswordDto.Email}
        };

        var callback = QueryHelpers.AddQueryString(forgotPasswordDto.ClientUri, param);

        var message = new Message([user.Email ?? string.Empty], "Reset password token", callback, null);

        await emailSender.SendEmailAsync(message);
    }

    public async Task<ResetPasswordResponseDto> ResetPassword(ResetPasswordDto resetPasswordDto)
    {
        var user = await userManager.FindByEmailAsync(resetPasswordDto.Email);

        if (user is null)
            throw new AccountManagerUnauthorizedLoginException("Invalid Request");

        var resetPassResult = await userManager.ResetPasswordAsync(user,
            resetPasswordDto.Token, resetPasswordDto.Password);

        if (!resetPassResult.Succeeded)
        {
            var errors = resetPassResult.Errors.Select(e => e.Description);
            throw new AccountManagerErrorsException(errors);
        }

        await userManager.SetLockoutEndDateAsync(user, null);

        return new ResetPasswordResponseDto {IsResetPasswordSuccessful = true};
    }

    public async Task EmailConfirmation(string email, string token)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
            throw new AccountManagerUnauthorizedLoginException("Invalid Request");

        var confirmResult = await userManager.ConfirmEmailAsync(user, token);
        if (!confirmResult.Succeeded)
            throw new AccountManagerUnauthorizedLoginException("Invalid Request");
    }

    public async Task<AuthResponseDto> TwoStepVerification(TwoFactorVerificationDto twoFactorVerificationDto)
    {
        var user = await userManager.FindByEmailAsync(twoFactorVerificationDto.Email);

        if (user is null)
            throw new AccountManagerUnauthorizedLoginException("Invalid Request");

        var validVerification = await userManager.VerifyTwoFactorTokenAsync(user,
            twoFactorVerificationDto.Provider, twoFactorVerificationDto.TwoFactorToken);

        if (!validVerification)
            throw new AccountManagerUnauthorizedLoginException("Invalid Token Verification");

        var token = await authenticationService.GetToken(user);
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

    private async Task<AuthResponseDto> GenerateOtpFor2StepVerification(User user)
    {
        var providers = await userManager.GetValidTwoFactorProvidersAsync(user);

        if (!providers.Contains("Email"))
            throw new AccountManagerUnauthorizedLoginException("Invalid 2-Step Verification Provider");

        var token = await userManager.GenerateTwoFactorTokenAsync(user, "Email");

        var message = new Message([user.Email ?? string.Empty], "Authentication token", token, null);

        await emailSender.SendEmailAsync(message);

        return new AuthResponseDto
        {
            Is2StepVerificationRequired = true,
            Provider = "Email"
        };
    }
}