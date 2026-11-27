using System.Net;
using System.Net.Http.Json;
using Contracts.DataTransferObjects.Auth;
using RichardSzalay.MockHttp;

namespace BDayClient.UnitTests.Http;

/// <summary>The token handling of the API HttpClient (replaces the per-page BeforeSend registration).</summary>
public class AuthTokenHandlerTests
{
    private const string Api = TestApi.Api;

    private readonly TestApi _api = new();

    [Fact]
    public async Task ApiCall_CarriesTheStoredToken_OnTheRequestItself()
    {
        var token = TestJwt.Create(expires: DateTimeOffset.UtcNow.AddMinutes(30));
        _api.LocalStorage.StoreTokens(token);
        _api.Http.Expect(HttpMethod.Get, Api + "persons")
            .WithHeaders("Authorization", $"Bearer {token}")
            .Respond(HttpStatusCode.OK);

        await _api.Client.GetAsync("persons");

        _api.Http.VerifyNoOutstandingExpectation();
        _api.Client.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    [Theory]
    [InlineData("account/login")]
    [InlineData("account/register")]
    [InlineData("token/refresh")]
    public async Task AuthEndpoints_GetNoToken(string route)
    {
        _api.LocalStorage.StoreTokens(TestJwt.Create(expires: DateTimeOffset.UtcNow.AddMinutes(30)));
        _api.Http.Expect(HttpMethod.Post, Api + route)
            .With(request => request.Headers.Authorization is null)
            .Respond(HttpStatusCode.OK);

        await _api.Client.PostAsync(route, new StringContent("{}"));

        _api.Http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task NotLoggedIn_SendsNoToken_AndDoesNotRefresh()
    {
        var refresh = _api.Http.When(HttpMethod.Post, Api + "token/refresh").Respond(HttpStatusCode.OK);
        _api.Http.Expect(HttpMethod.Get, Api + "persons")
            .With(request => request.Headers.Authorization is null)
            .Respond(HttpStatusCode.OK);

        await _api.Client.GetAsync("persons");

        _api.Http.VerifyNoOutstandingExpectation();
        _api.Http.GetMatchCount(refresh).Should().Be(0);
    }

    // Review finding: only Persons, Index and Users hooked up the refresh, so deep links into the other pages got 401s.
    [Fact]
    public async Task ExpiringToken_IsRefreshedBeforeTheCall()
    {
        var newToken = TestJwt.Create(expires: DateTimeOffset.UtcNow.AddMinutes(5));
        _api.LocalStorage.StoreTokens(TestJwt.Create(expires: DateTimeOffset.UtcNow.AddMinutes(1)), "refresh-1");
        _api.Http.When(HttpMethod.Post, Api + "token/refresh")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new AuthResponseDto
            {
                IsAuthSuccessful = true, Token = newToken, RefreshToken = "refresh-2"
            }));
        _api.Http.Expect(HttpMethod.Get, Api + "persons/1")
            .WithHeaders("Authorization", $"Bearer {newToken}")
            .Respond(HttpStatusCode.OK);

        await _api.Client.GetAsync("persons/1");

        _api.Http.VerifyNoOutstandingExpectation();
        _api.LocalStorage.AccessToken.Should().Be(newToken);
        _api.LocalStorage.RefreshToken.Should().Be("refresh-2");
    }

    // Review finding: two requests inside the 2-minute window both refreshed; the server rotates the refresh token,
    // so the second refresh was refused and the user was logged out.
    [Fact]
    public async Task ConcurrentRequests_ShareOneRefresh()
    {
        var newToken = TestJwt.Create(expires: DateTimeOffset.UtcNow.AddMinutes(5));
        _api.LocalStorage.StoreTokens(TestJwt.Create(expires: DateTimeOffset.UtcNow.AddMinutes(1)), "refresh-1");
        var releaseRefresh = new TaskCompletionSource();
        var refresh = _api.Http.When(HttpMethod.Post, Api + "token/refresh");
        refresh.Respond(async () =>
            {
                await releaseRefresh.Task;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new AuthResponseDto
                    {
                        IsAuthSuccessful = true, Token = newToken, RefreshToken = "refresh-2"
                    })
                };
            });
        var persons = _api.Http.When(HttpMethod.Get, Api + "persons")
            .WithHeaders("Authorization", $"Bearer {newToken}")
            .Respond(HttpStatusCode.OK);

        var requests = Enumerable.Range(1, 3).Select(i => _api.Client.GetAsync($"persons?page={i}")).ToList();
        await Task.Delay(100);
        releaseRefresh.SetResult();
        var responses = await Task.WhenAll(requests);

        _api.Http.GetMatchCount(refresh).Should().Be(1);
        _api.Http.GetMatchCount(persons).Should().Be(3);
        responses.Should().OnlyContain(r => r.IsSuccessStatusCode);
    }

    [Fact]
    public async Task RefusedRefresh_LogsOut_AndTheCallEndsOnTheLoginPage()
    {
        _api.LocalStorage.StoreTokens(TestJwt.Create(expires: DateTimeOffset.UtcNow.AddMinutes(1)), "used-refresh-token");
        _api.Http.When(HttpMethod.Post, Api + "token/refresh")
            .Respond(HttpStatusCode.BadRequest, JsonContent.Create(new AuthResponseDto
            {
                IsAuthSuccessful = false, ErrorMessage = "Invalid client request"
            }));
        _api.Http.When(HttpMethod.Get, Api + "persons")
            .With(request => request.Headers.Authorization is null)
            .Respond(HttpStatusCode.Unauthorized);

        await _api.Client.GetAsync("persons");

        _api.LocalStorage.AccessToken.Should().BeNull();
        _api.LocalStorage.RefreshToken.Should().BeNull();
        _api.Navigation.NavigatedTo.Should().ContainSingle().Which.Should().StartWith("/login");
    }

    [Fact]
    public async Task RequestToAnotherHost_GetsNoToken()
    {
        _api.LocalStorage.StoreTokens(TestJwt.Create(expires: DateTimeOffset.UtcNow.AddMinutes(30)));
        _api.Http.Expect(HttpMethod.Get, "https://elsewhere.test/api/persons")
            .With(request => request.Headers.Authorization is null)
            .Respond(HttpStatusCode.OK);

        await _api.Client.GetAsync("https://elsewhere.test/api/persons");

        _api.Http.VerifyNoOutstandingExpectation();
    }
}
