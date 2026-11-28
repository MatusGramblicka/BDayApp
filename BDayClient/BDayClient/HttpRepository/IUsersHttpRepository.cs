using BDayClient.Http;
using Contracts.DataTransferObjects.User;

namespace BDayClient.HttpRepository;

/// <summary>User administration calls. Every method throws an <see cref="ApiException"/> when the call fails.</summary>
public interface IUsersHttpRepository
{
    Task<List<UserLite>> GetUsers();
    Task UpdateUser(UserLite user);
    Task RemoveAdminRole(UserLite user);
    Task DeleteUser(UserLite user);
    Task SetTwoFactorAuthorization(UserLite2StepsAuthDto user2StepsAuth);
}
