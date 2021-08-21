using Entities;
using Entities.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
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
        Task<List<UserLite>> GetUsers();
        Task<HttpStatusCode> UpdateUser(UserLite user);
        Task<HttpStatusCode> RemoveAdminRole(UserLite user);
        Task<HttpStatusCode> DeleteUser(UserLite user);
        Task<AuthResponseDto> LoginVerification(TwoFactorVerificationDto twoFactorDto);
        Task<HttpStatusCode> SetTwoFactorAuthorization(UserLite2StepsAuthDto user2StepsAuth);
    }
}
