using Contracts.DataTransferObjects.Auth;

namespace Interfaces.Managers;

public interface ITokenManager
{
    Task<(string, string)> Refresh(RefreshTokenDto tokenDto);
}