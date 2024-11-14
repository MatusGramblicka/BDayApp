using Entities.DataTransferObjects.User;

namespace Contracts.Managers;

public interface IUserManager
{
    IQueryable<UserLite> GetUsers();

    Task UpdateUser(UserLite userForUpdate);

    Task RemoveAdminRole(UserLite userForUpdate);

    Task DeleteUser(UserLite userForDeletion);

    Task SetTwoFactorAuthorization(UserLite2StepsAuthDto userForUpdate);
}