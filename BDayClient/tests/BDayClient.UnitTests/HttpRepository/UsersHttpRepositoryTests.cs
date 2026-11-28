using System.Net;
using System.Net.Http.Json;
using BDayClient.Http;
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

    [Fact]
    public async Task GetUsers_NullBody_IsEmptyList()
    {
        _http.When(HttpMethod.Get, Api + "users/users").Respond("application/json", "null");

        (await _sut.GetUsers()).Should().BeEmpty();
    }

    public static TheoryData<string, Func<UsersHttpRepository, UserLite, Task>> UserEndpoints => new()
    {
        { "users/updateuser", (r, u) => r.UpdateUser(u) },
        { "users/removeadminrole", (r, u) => r.RemoveAdminRole(u) },
        { "users/deleteuser", (r, u) => r.DeleteUser(u) }
    };

    [Theory]
    [MemberData(nameof(UserEndpoints))]
    public async Task UserActions_PostTheUser_AnyStatus2xxIsSuccess(
        string route, Func<UsersHttpRepository, UserLite, Task> action)
    {
        _http.Expect(HttpMethod.Post, Api + route)
            .WithJsonContent<UserLite>(u => u.Email == "a@test.com", TestJson.Web)
            .Respond(HttpStatusCode.NoContent);

        await action(_sut, new UserLite { Email = "a@test.com" });

        _http.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [MemberData(nameof(UserEndpoints))]
    public async Task UserActions_RuleViolation_ThrowsApiExceptionWithServerMessage(
        string route, Func<UsersHttpRepository, UserLite, Task> action)
    {
        _http.When(HttpMethod.Post, Api + route)
            .Respond(HttpStatusCode.BadRequest, "text/plain", "You cannot change your own account.");

        var exception = await Record.ExceptionAsync(() => action(_sut, new UserLite { Email = "me@test.com" }));

        exception.Should().BeOfType<ApiException>().Which.Message.Should().Be("You cannot change your own account.");
    }

    [Fact]
    public async Task SetTwoFactorAuthorization_PostsFlag()
    {
        _http.Expect(HttpMethod.Post, Api + "users/SetTwoFactorAuthorization")
            .WithJsonContent<UserLite2StepsAuthDto>(u => u.Email == "a@test.com" && u.TwoFactorEnabled, TestJson.Web)
            .Respond(HttpStatusCode.OK);

        await _sut.SetTwoFactorAuthorization(new UserLite2StepsAuthDto
        {
            Email = "a@test.com", TwoFactorEnabled = true
        });

        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SetTwoFactorAuthorization_UnknownUser_ThrowsApiException()
    {
        _http.When(HttpMethod.Post, Api + "users/SetTwoFactorAuthorization").Respond(HttpStatusCode.NotFound);

        var exception = await Record.ExceptionAsync(() =>
            _sut.SetTwoFactorAuthorization(new UserLite2StepsAuthDto { Email = "gone@test.com" }));

        exception.Should().BeOfType<ApiException>().Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
