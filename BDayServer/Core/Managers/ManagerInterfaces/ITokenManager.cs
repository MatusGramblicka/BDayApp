using Contracts.DataTransferObjects.Auth;

namespace Core.Managers.ManagerInterfaces;

public interface ITokenManager
{
    Task<(string, string)> Refresh(RefreshTokenDto tokenDto);
}