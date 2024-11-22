using BDayClient.Pocos;
using Contracts.DataTransferObjects.Auth;
using System.Net;

namespace BDayClient.HttpRepository;

public interface IAuthenticationService
{
    Task<ResponseDto> RegisterUser(UserForRegistration userForRegistration);
    Task<AuthResponseDto> Login(UserForAuthentication userForAuthentication);
    Task Logout();
    Task<string> RefreshToken();
    Task<HttpStatusCode> ForgotPassword(ForgotPassword forgotPasswordDto);
    Task<ResetPasswordResponseDto> ResetPassword(ResetPassword resetPassword);
    Task<HttpStatusCode> EmailConfirmation(string email, string token);
    Task<AuthResponseDto> LoginVerification(TwoFactorVerificationDto twoFactorDto);
}