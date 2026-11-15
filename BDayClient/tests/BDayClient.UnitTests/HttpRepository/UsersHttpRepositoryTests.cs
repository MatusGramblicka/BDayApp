using System.Net;
using System.Net.Http.Json;
using BDayClient.HttpRepository;
using Contracts.DataTransferObjects.User;
using RichardSzalay.MockHttp;

namespace BDayClient.UnitTests.HttpRepository;

public class UsersHttpRepositoryTests
{
    private const string Api = "https://api.test/api/";

    private readonly MockHttpMessageHandler _http = new();
    private readonly UsersHttpRepository _sut;

    public UsersHttpRepositoryTests()
    {
        var client = _http.ToHttpClient();
        client.BaseAddress = new Uri(Api);
        _sut = new UsersHttpRepository(client);
    }

    [Fact]
    public async Task GetUsers_DeserializesList()
    {
        _http.When(HttpMethod.Get, Api + "users/users").Respond(HttpStatusCode.OK, JsonContent.Create(new[]
        {
            new UserLite { Email = "a@test.com", IsAdmin = true },
            new UserLite { Email = "b@test.com", TwoFactorEnabled = true }
        }));

        var users = await _sut.GetUsers();

        users.Should().BeEquivalentTo(new[]
        {
            new UserLite { Email = "a@test.com", IsAdmin = true },
            new UserLite { Email = "b@test.com", TwoFactorEnabled = true }
        });
    }

    public static TheoryData<string, Func<UsersHttpRepository, UserLite, Task<HttpStatusCode>>> UserEndpoints => new()
    {
        { "users/updateuser", (r, u) => r.UpdateUser(u) },
        { "users/removeadminrole", (r, u) => r.RemoveAdminRole(u) },
        { "users/deleteuser", (r, u) => r.DeleteUser(u) }
    };

    [Theory]
    [MemberData(nameof(UserEndpoints))]
    public async Task UserActions_PostUserAndReturnStatusCode(
        string route, Func<UsersHttpRepository, UserLite, Task<HttpStatusCode>> action)
    {
        _http.Expect(HttpMethod.Post, Api + route)
            .WithJsonContent<UserLite>(u => u.Email == "a@test.com", TestJson.Web)
            .Respond(HttpStatusCode.Accepted);

        var status = await action(_sut, new UserLite { Email = "a@test.com" });

        status.Should().Be(HttpStatusCode.Accepted);
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SetTwoFactorAuthorization_PostsFlag()
    {
        _http.Expect(HttpMethod.Post, Api + "users/SetTwoFactorAuthorization")
            .WithJsonContent<UserLite2StepsAuthDto>(u => u.Email == "a@test.com" && u.TwoFactorEnabled, TestJson.Web)
            .Respond(HttpStatusCode.OK);

        var status = await _sut.SetTwoFactorAuthorization(new UserLite2StepsAuthDto
        {
            Email = "a@test.com", TwoFactorEnabled = true
        });

        status.Should().Be(HttpStatusCode.OK);
        _http.VerifyNoOutstandingExpectation();
    }
}
