using Contracts.DataTransferObjects.User;
using System.Net;
using System.Net.Http.Json;

namespace BDayClient.HttpRepository;

public class UsersHttpRepository(HttpClient client) : IUsersHttpRepository
{
    public async Task<List<UserLite>> GetUsers()
    {
        var usersResult = await client.GetFromJsonAsync<List<UserLite>>("users/users");

        return usersResult;
    }

    public async Task<HttpStatusCode> UpdateUser(UserLite user)
    {
        var result = await client.PostAsJsonAsync("users/updateuser",
            user);

        return result.StatusCode;
    }

    public async Task<HttpStatusCode> RemoveAdminRole(UserLite user)
    {
        var result = await client.PostAsJsonAsync("users/removeadminrole",
            user);

        return result.StatusCode;
    }

    public async Task<HttpStatusCode> DeleteUser(UserLite user)
    {
        var result = await client.PostAsJsonAsync("users/deleteuser",
            user);

        return result.StatusCode;
    }

    public async Task<HttpStatusCode> SetTwoFactorAuthorization(UserLite2StepsAuthDto user2StepsAuth)
    {
        var result = await client.PostAsJsonAsync("users/SetTwoFactorAuthorization",
            user2StepsAuth);

        return result.StatusCode;
    }
}