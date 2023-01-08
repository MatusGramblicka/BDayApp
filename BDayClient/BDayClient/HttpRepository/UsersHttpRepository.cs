using Entities.DTO;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BDayClient.HttpRepository
{
    public class UsersHttpRepository : IUsersHttpRepository
    {
        private readonly HttpClient _client;

        public UsersHttpRepository(HttpClient client)
        {
            _client = client;
        }

        public async Task<List<UserLite>> GetUsers()
        {
            var usersResult = await _client.GetFromJsonAsync<List<UserLite>>("users/users");

            return usersResult;
        }

        public async Task<HttpStatusCode> UpdateUser(UserLite user)
        {
            var result = await _client.PostAsJsonAsync("users/updateuser",
                user);

            return result.StatusCode;
        }

        public async Task<HttpStatusCode> RemoveAdminRole(UserLite user)
        {
            var result = await _client.PostAsJsonAsync("users/removeadminrole",
                user);

            return result.StatusCode;
        }

        public async Task<HttpStatusCode> DeleteUser(UserLite user)
        {
            var result = await _client.PostAsJsonAsync("users/deleteuser",
                user);

            return result.StatusCode;
        }

        public async Task<HttpStatusCode> SetTwoFactorAuthorization(UserLite2StepsAuthDto user2StepsAuth)
        {
            var result = await _client.PostAsJsonAsync("users/SetTwoFactorAuthorization",
                user2StepsAuth);

            return result.StatusCode;
        }
    }
}