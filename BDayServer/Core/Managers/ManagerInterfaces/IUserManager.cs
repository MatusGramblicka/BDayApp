using Contracts.DataTransferObjects.User;

namespace Core.Managers.ManagerInterfaces;

public interface IUserManager
{
    Task<List<UserLite>> GetUsersAsync();

    /// <summary>Gives the user the administrator role.</summary>
    Task UpdateUser(UserLite userForUpdate);

    Task RemoveAdminRole(UserLite userForUpdate);

    Task DeleteUser(UserLite userForDeletion);

    Task SetTwoFactorAuthorization(UserLite2StepsAuthDto userForUpdate);
}
