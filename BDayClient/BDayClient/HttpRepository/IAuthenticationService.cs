using BDayClient.Http;
using BDayClient.Pocos;
using Contracts.DataTransferObjects.Auth;

namespace BDayClient.HttpRepository;

/// <summary>
/// Account and token calls. Failures the user has to act on (wrong password, invalid input, ...) are returned in the
/// result. Failures <see cref="ApiErrorHandler"/> has already shown (429, 5xx, no connection) are thrown as an
/// <see cref="ApiException"/> with <see cref="ApiException.Handled"/> set.
/// </summary>
public interface IAuthenticationService
{
    Task<ResponseDto> RegisterUser(UserForRegistration userForRegistration);
    Task<AuthResponseDto> Login(UserForAuthentication userForAuthentication);
    Task Logout();

    /// <summary>Exchanges the stored tokens for new ones. Logs the user out and returns "" when the server refuses.</summary>
    Task<string> RefreshToken();

    Task<ApiResult> ForgotPassword(ForgotPassword forgotPassword);
    Task<ResetPasswordResponseDto> ResetPassword(ResetPassword resetPassword);
    Task<ApiResult> EmailConfirmation(string email, string token);
    Task<AuthResponseDto> LoginVerification(TwoFactorVerificationDto twoFactorDto);
}
