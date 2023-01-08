using Entities.DTO;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace BDayClient.HttpRepository
{
    public interface IUsersHttpRepository
    {
        Task<List<UserLite>> GetUsers();
        Task<HttpStatusCode> UpdateUser(UserLite user);
        Task<HttpStatusCode> RemoveAdminRole(UserLite user);
        Task<HttpStatusCode> DeleteUser(UserLite user);
        Task<HttpStatusCode> SetTwoFactorAuthorization(UserLite2StepsAuthDto user2StepsAuth);
    }
}