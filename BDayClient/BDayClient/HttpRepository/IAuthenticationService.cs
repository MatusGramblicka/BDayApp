using Entities.DTO;
using System.Net;
using System.Threading.Tasks;

namespace BDayClient.HttpRepository
{
    public interface IAuthenticationService
    {
        Task<ResponseDto> RegisterUser(UserForRegistrationDto userForRegistrationDto);
        Task<AuthResponseDto> Login(UserForAuthenticationDto userForAuthentication);
        Task Logout();
        Task<string> RefreshToken();
        Task<HttpStatusCode> ForgotPassword(ForgotPasswordDto forgotPasswordDto);
        Task<ResetPasswordResponseDto> ResetPassword(ResetPasswordDto resetPasswordDto);
        Task<HttpStatusCode> EmailConfirmation(string email, string token);
        Task<AuthResponseDto> LoginVerification(TwoFactorVerificationDto twoFactorDto);

    }
}