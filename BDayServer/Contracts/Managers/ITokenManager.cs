using Entities.DataTransferObjects.Auth;

namespace Contracts.Managers;

public interface ITokenManager
{
    Task<(string, string)> Refresh(RefreshTokenDto tokenDto);
}