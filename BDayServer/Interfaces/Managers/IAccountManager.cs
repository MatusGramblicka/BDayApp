using Entities.DataTransferObjects.Auth;
using Entities.DataTransferObjects.User;

namespace Interfaces.Managers;

public interface IAccountManager
{
    Task RegisterUser(UserForRegistrationDto userForRegistrationDto);

    Task<AuthResponseDto> Login(UserForAuthenticationDto userForAuthenticationDto);

    Task ForgotPassword(ForgotPasswordDto forgotPasswordDto);

    Task<ResetPasswordResponseDto> ResetPassword(ResetPasswordDto resetPasswordDto);

    Task EmailConfirmation(string email, string token);

    Task<AuthResponseDto> TwoStepVerification(TwoFactorVerificationDto twoFactorVerificationDto);
}