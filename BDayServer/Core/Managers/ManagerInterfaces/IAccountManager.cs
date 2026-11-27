using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;

namespace Core.Managers.ManagerInterfaces;

public interface IAccountManager
{
    Task RegisterUser(UserForRegistrationDto userForRegistrationDto);

    Task<AuthResponseDto> Login(UserForAuthenticationDto userForAuthenticationDto);

    /// <summary>E-mails a reset link; does nothing (and says nothing) for unknown addresses.</summary>
    Task ForgotPassword(ForgotPasswordDto forgotPasswordDto);

    Task ResetPassword(ResetPasswordDto resetPasswordDto);

    Task EmailConfirmation(string email, string token);

    Task<AuthResponseDto> TwoStepVerification(TwoFactorVerificationDto twoFactorVerificationDto);
}
