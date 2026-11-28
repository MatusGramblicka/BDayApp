using Contracts.DataTransferObjects.Auth;

namespace Core.Managers.ManagerInterfaces;

public interface ITokenManager
{
    Task<(string, string)> Refresh(RefreshTokenDto tokenDto);

    /// <summary>Ends the login the refresh token belongs to (logout).</summary>
    Task Revoke(RevokeTokenDto revokeTokenDto);
}