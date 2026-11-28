using System.Globalization;
using Contracts.Authorization;
using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Core.Services;
using Core.Services.Interfaces;
using EmailService.EmailServiceContracts;
using EmailService.Interfaces;
using Entities.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Identity = Microsoft.AspNetCore.Identity;

namespace Core.Managers;

public class AccountManager(
    Identity.UserManager<User> userManager,
    Identity.RoleManager<Identity.IdentityRole> roleManager,
    IAuthenticationService authenticationService,
    IRefreshTokenStore refreshTokens,
    IEmailSender emailSender,
    ILogger<AccountManager> logger) : IAccountManager
{
    public const string InvalidAuthenticationMessage = "Invalid Authentication";
    public const string LockedOutMessage = "The account is locked out";
    public const string EmailNotConfirmedMessage = "Email is not confirmed";
    public const string InvalidVerificationMessage = "Invalid Token Verification";
    public const string VerificationExpiredMessage = "The verification code has expired. Please log in again.";
    public const string InvalidRequestMessage = "Invalid Request";
    public const string InvalidTokenMessage = "Invalid token.";
    public const string RegistrationFailedMessage = "The registration could not be completed. Please try again later.";

    // Stored (in AspNetUserTokens) when a user passed the password check and was e-mailed a 2-step code.
    // Codes are only accepted while it is valid, so nobody can guess codes without knowing the password.
    public const string TwoFactorLoginProvider = "BDayApp";
    public const string TwoFactorLoginPendingToken = "TwoFactorLoginPending";
    private static readonly TimeSpan TwoFactorLoginWindow = TimeSpan.FromMinutes(10);

    private static readonly string EmailProvider = Identity.TokenOptions.DefaultEmailProvider;

    public async Task RegisterUser(UserForRegistrationDto userForRegistrationDto)
    {
        ArgumentNullException.ThrowIfNull(userForRegistrationDto);

        var user = new User
        {
            UserName = userForRegistrationDto.Email,
            Email = userForRegistrationDto.Email
        };

        var result = await userManager.CreateAsync(user, userForRegistrationDto.Password);
        if (!result.Succeeded)
            throw new AccountManagerErrorsException(result.Errors.Select(e => e.Description));

        try
        {
            await roleManager.EnsureRoleExistsAsync(AppRoles.Viewer);
            (await userManager.AddToRoleAsync(user, AppRoles.Viewer)).ThrowIfFailed("Adding the Viewer role");
            (await userManager.SetTwoFactorEnabledAsync(user, true)).ThrowIfFailed("Enabling 2-step verification");

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var callback = QueryHelpers.AddQueryString(userForRegistrationDto.ClientUri,
                new Dictionary<string, string?>
                {
                    ["token"] = token,
                    ["email"] = userForRegistrationDto.Email
                });

            await emailSender.SendEmailAsync(new Message([userForRegistrationDto.Email], "Email confirmation token",
                callback, null));
        }
        catch (Exception ex)
        {
            // Without its role or the confirmation e-mail the account is unusable, and its address would stay taken.
            // Remove it again, so that the user can simply register once more.
            logger.LogError(ex, "Registration failed after the account was created; the account is removed again.");
            await TryDeleteUserAsync(user);

            throw new AccountManagerErrorsException([RegistrationFailedMessage]);
        }
    }

    public async Task<AuthResponseDto> Login(UserForAuthenticationDto userForAuthenticationDto)
    {
        ArgumentNullException.ThrowIfNull(userForAuthenticationDto);

        var user = await userManager.FindByNameAsync(userForAuthenticationDto.Email);

        // An unknown user gets the same answer as a wrong password: the form must not reveal who has an account.
        if (user is null)
            throw new AccountManagerUnauthorizedLoginException(InvalidAuthenticationMessage);

        if (await userManager.IsLockedOutAsync(user))
            throw new AccountManagerUnauthorizedLoginException(LockedOutMessage);

        if (!await userManager.CheckPasswordAsync(user, userForAuthenticationDto.Password))
        {
            await userManager.AccessFailedAsync(user);

            if (await userManager.IsLockedOutAsync(user))
            {
                await SendLockedOutEmailAsync(user);
                throw new AccountManagerUnauthorizedLoginException(LockedOutMessage);
            }

            throw new AccountManagerUnauthorizedLoginException(InvalidAuthenticationMessage);
        }

        // Only checked after the password, so that it doesn't reveal unconfirmed accounts to anybody else.
        if (!await userManager.IsEmailConfirmedAsync(user))
            throw new AccountManagerUnauthorizedLoginException(EmailNotConfirmedMessage);

        if (await userManager.GetTwoFactorEnabledAsync(user))
            return await StartTwoStepVerificationAsync(user);

        return await IssueTokensAsync(user);
    }

    public async Task ForgotPassword(ForgotPasswordDto forgotPasswordDto)
    {
        ArgumentNullException.ThrowIfNull(forgotPasswordDto);

        var user = await userManager.FindByEmailAsync(forgotPasswordDto.Email);

        // Unknown addresses are ignored silently: the caller answers the same way in both cases.
        if (user is null)
            return;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var email = user.Email ?? forgotPasswordDto.Email;
        var callback = QueryHelpers.AddQueryString(forgotPasswordDto.ClientUri, new Dictionary<string, string?>
        {
            ["token"] = token,
            ["email"] = email
        });

        await emailSender.SendEmailAsync(new Message([email], "Reset password token", callback, null));
    }

    public async Task ResetPassword(ResetPasswordDto resetPasswordDto)
    {
        ArgumentNullException.ThrowIfNull(resetPasswordDto);

        var user = await userManager.FindByEmailAsync(resetPasswordDto.Email);

        // Same answer as for a wrong token, so the form doesn't reveal who has an account.
        if (user is null)
            throw new AccountManagerErrorsException([InvalidTokenMessage]);

        var resetPassResult = await userManager.ResetPasswordAsync(user, resetPasswordDto.Token,
            resetPasswordDto.Password);

        if (!resetPassResult.Succeeded)
            throw new AccountManagerErrorsException(resetPassResult.Errors.Select(e => e.Description));

        // The reset proves control of the mailbox: unlock the account, and end all its logins (their access tokens
        // expire within minutes) - whoever knew the old password is signed out everywhere.
        LogIfFailed(await userManager.SetLockoutEndDateAsync(user, null), "Unlocking the account", user);
        LogIfFailed(await userManager.ResetAccessFailedCountAsync(user), "Resetting the failed login count", user);

        await refreshTokens.RevokeAllAsync(user.Id);
    }

    public async Task EmailConfirmation(string email, string token)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
            throw new AccountManagerUnauthorizedLoginException(InvalidRequestMessage);

        var confirmResult = await userManager.ConfirmEmailAsync(user, token);
        if (!confirmResult.Succeeded)
            throw new AccountManagerUnauthorizedLoginException(InvalidRequestMessage);
    }

    public async Task<AuthResponseDto> TwoStepVerification(TwoFactorVerificationDto twoFactorVerificationDto)
    {
        ArgumentNullException.ThrowIfNull(twoFactorVerificationDto);

        var user = await userManager.FindByEmailAsync(twoFactorVerificationDto.Email);

        if (user is null)
            throw new AccountManagerUnauthorizedLoginException(InvalidVerificationMessage);

        if (await userManager.IsLockedOutAsync(user))
            throw new AccountManagerUnauthorizedLoginException(LockedOutMessage);

        if (!await IsTwoStepLoginPendingAsync(user) || !await userManager.GetTwoFactorEnabledAsync(user))
            throw new AccountManagerUnauthorizedLoginException(VerificationExpiredMessage);

        // Codes are only ever sent by e-mail, so the provider is fixed (the client's Provider value is ignored).
        if (!await userManager.VerifyTwoFactorTokenAsync(user, EmailProvider, twoFactorVerificationDto.TwoFactorToken))
        {
            // Wrong codes count like wrong passwords, so codes can't be tried out until one fits.
            await userManager.AccessFailedAsync(user);

            if (await userManager.IsLockedOutAsync(user))
            {
                await userManager.RemoveAuthenticationTokenAsync(user, TwoFactorLoginProvider, TwoFactorLoginPendingToken);
                throw new AccountManagerUnauthorizedLoginException(LockedOutMessage);
            }

            throw new AccountManagerUnauthorizedLoginException(InvalidVerificationMessage);
        }

        await userManager.RemoveAuthenticationTokenAsync(user, TwoFactorLoginProvider, TwoFactorLoginPendingToken);

        return await IssueTokensAsync(user);
    }

    private async Task<AuthResponseDto> StartTwoStepVerificationAsync(User user)
    {
        var providers = await userManager.GetValidTwoFactorProvidersAsync(user);

        if (!providers.Contains(EmailProvider))
            throw new AccountManagerUnauthorizedLoginException("Invalid 2-Step Verification Provider");

        var expiresAt = DateTimeOffset.UtcNow.Add(TwoFactorLoginWindow).ToString("O", CultureInfo.InvariantCulture);
        (await userManager.SetAuthenticationTokenAsync(user, TwoFactorLoginProvider, TwoFactorLoginPendingToken, expiresAt))
            .ThrowIfFailed("Starting the 2-step verification");

        var code = await userManager.GenerateTwoFactorTokenAsync(user, EmailProvider);

        await emailSender.SendEmailAsync(new Message([user.Email ?? string.Empty], "Authentication token", code, null));

        return new AuthResponseDto
        {
            Is2StepVerificationRequired = true,
            Provider = EmailProvider
        };
    }

    private async Task<bool> IsTwoStepLoginPendingAsync(User user)
    {
        var expiresAt = await userManager.GetAuthenticationTokenAsync(user, TwoFactorLoginProvider,
            TwoFactorLoginPendingToken);

        return DateTimeOffset.TryParse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                   out var expiry) &&
               expiry > DateTimeOffset.UtcNow;
    }

    private async Task<AuthResponseDto> IssueTokensAsync(User user)
    {
        var token = await authenticationService.GetToken(user);

        // A login of its own: logging in on one device no longer ends the logins on the others.
        var refreshToken = await refreshTokens.IssueAsync(user.Id);

        await userManager.ResetAccessFailedCountAsync(user);

        return new AuthResponseDto
        {
            IsAuthSuccessful = true,
            Token = token,
            RefreshToken = refreshToken
        };
    }

    private async Task SendLockedOutEmailAsync(User user)
    {
        const string content = "Your account is locked out. " +
                               "If you want to reset the password, you can use the " +
                               "Forgot Password link on the Login page";
        try
        {
            await emailSender.SendEmailAsync(new Message([user.Email ?? string.Empty],
                "Locked out account information", content, null));
        }
        catch (Exception ex)
        {
            // The user still gets the "locked out" answer on the login page.
            logger.LogError(ex, "The locked-out notice for user {UserId} could not be sent.", user.Id);
        }
    }

    private async Task TryDeleteUserAsync(User user)
    {
        try
        {
            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
                logger.LogError("The incompletely registered user {UserId} could not be removed: {Errors}",
                    user.Id, result.Describe());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The incompletely registered user {UserId} could not be removed.", user.Id);
        }
    }

    private void LogIfFailed(Identity.IdentityResult result, string operation, User user)
    {
        if (!result.Succeeded)
            logger.LogWarning("{Operation} for user {UserId} failed after a password reset: {Errors}",
                operation, user.Id, result.Describe());
    }
}
