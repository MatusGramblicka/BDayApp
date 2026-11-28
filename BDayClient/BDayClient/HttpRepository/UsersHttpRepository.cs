using System.Net.Http.Json;
using BDayClient.Http;
using Contracts.DataTransferObjects.User;

namespace BDayClient.HttpRepository;

public class UsersHttpRepository(HttpClient client) : IUsersHttpRepository
{
    public async Task<List<UserLite>> GetUsers()
    {
        using var response = await client.GetAsync("users/users");
        await response.EnsureSuccessAsync();

        return await response.ReadJsonOrDefaultAsync<List<UserLite>>() ?? [];
    }

    public Task UpdateUser(UserLite user) => PostAsync("users/updateuser", user);

    public Task RemoveAdminRole(UserLite user) => PostAsync("users/removeadminrole", user);

    public Task DeleteUser(UserLite user) => PostAsync("users/deleteuser", user);

    public Task SetTwoFactorAuthorization(UserLite2StepsAuthDto user2StepsAuth) =>
        PostAsync("users/SetTwoFactorAuthorization", user2StepsAuth);

    private async Task PostAsync<T>(string requestUri, T body)
    {
        using var response = await client.PostAsJsonAsync(requestUri, body);
        await response.EnsureSuccessAsync();
    }
}
