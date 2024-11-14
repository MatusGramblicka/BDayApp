using Entities.DataTransferObjects.Auth;
using Entities.DataTransferObjects.User;

namespace Contracts.Managers;

public interface IAccountManager
{
    Task RegisterUser(UserForRegistrationDto userForRegistrationDto);

    Task<AuthResponseDto> Login(UserForAuthenticationDto userForAuthenticationDto)
}