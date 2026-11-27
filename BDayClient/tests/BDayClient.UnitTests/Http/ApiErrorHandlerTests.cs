using System.Net;
using System.Net.Http.Json;
using System.Text;
using BDayClient.Http;
using BDayClient.Pocos;
using Contracts.DataTransferObjects.Auth;
using RichardSzalay.MockHttp;

namespace BDayClient.UnitTests.Http;

/// <summary>The central error handling of the API HttpClient (replaces the per-page HttpInterceptorService).</summary>
public class ApiErrorHandlerTests
{
    private const string Api = TestApi.Api;

    private readonly TestApi _api = new();

    public ApiErrorHandlerTests()
    {
        _api.LocalStorage.StoreTokens(TestJwt.Create(expires: DateTimeOffset.UtcNow.AddMinutes(30)));
        _api.Navigation.NavigateTo("/persons");
        _api.Navigation.NavigatedTo.Clear();
    }

    private static async Task<bool> IsHandled(HttpResponseMessage response) =>
        (await ApiErrorReader.ReadAsync(response)).Handled;

    [Fact]
    public async Task Unauthorized_FromApiCall_LogsOutAndGoesToLoginWithReturnUrl()
    {
        _api.Http.When(HttpMethod.Get, Api + "persons").Respond(HttpStatusCode.Unauthorized);

        var response = await _api.Client.GetAsync("persons");

        _api.LocalStorage.AccessToken.Should().BeNull();
        _api.LocalStorage.RefreshToken.Should().BeNull();
        _api.Navigation.NavigatedTo.Should().Equal("/login?returnUrl=%2Fpersons");
        (await IsHandled(response)).Should().BeTrue();
    }

    // Review finding: a leaked interceptor turned a wrong-password login into a redirect to /unauthorized.
    [Fact]
    public async Task Unauthorized_FromLogin_IsLeftToTheLoginPage()
    {
        _api.LocalStorage.Items.Clear();
        _api.Http.When(HttpMethod.Post, Api + "account/login")
            .Respond(HttpStatusCode.Unauthorized, JsonContent.Create(new AuthResponseDto { ErrorMessage = "Invalid credentials" }));

        var result = await _api.AuthService.Login(new UserForAuthentication { Email = "a@test.com", Password = "wrong" });

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid credentials");
        _api.Navigation.NavigatedTo.Should().BeEmpty();
        _api.VerifyNoErrorToast();
    }

    [Fact]
    public async Task Forbidden_GoesToUnauthorizedPage()
    {
        _api.Http.When(HttpMethod.Get, Api + "users/users").Respond(HttpStatusCode.Forbidden);

        var response = await _api.Client.GetAsync("users/users");

        _api.Navigation.NavigatedTo.Should().Equal("/unauthorized");
        _api.LocalStorage.AccessToken.Should().NotBeNull("a 403 does not log the user out");
        (await IsHandled(response)).Should().BeTrue();
    }

    [Theory]
    [InlineData("persons")]
    [InlineData("upload")]
    public async Task TooManyRequests_ShowsToast(string route)
    {
        _api.Http.When(Api + route).Respond(HttpStatusCode.TooManyRequests);

        var response = await _api.Client.PostAsync(route, new StringContent(string.Empty));

        _api.VerifyErrorToast("Too many requests - please try again in a minute", Times.Once());
        _api.Navigation.NavigatedTo.Should().BeEmpty();
        (await IsHandled(response)).Should().BeTrue();
    }

    [Fact]
    public async Task TooManyRequests_OnLogin_ShowsToastOnce_AndLoginReportsItAsHandled()
    {
        _api.Http.When(HttpMethod.Post, Api + "account/login").Respond(HttpStatusCode.TooManyRequests);

        var exception = await Record.ExceptionAsync(() =>
            _api.AuthService.Login(new UserForAuthentication { Email = "a@test.com", Password = "x" }));

        exception.Should().BeOfType<ApiException>().Which.Handled.Should().BeTrue();
        _api.VerifyErrorToast("Too many requests - please try again in a minute", Times.Once());
    }

    [Fact]
    public async Task ServerError_ShowsTheServersMessage()
    {
        _api.Http.When(HttpMethod.Get, Api + "persons").Respond(HttpStatusCode.InternalServerError,
            new StringContent("""{"title":"An error occurred while processing your request.","status":500}""",
                Encoding.UTF8, "application/problem+json"));

        var response = await _api.Client.GetAsync("persons");

        _api.VerifyErrorToast("An error occurred while processing your request.", Times.Once());
        _api.Navigation.NavigatedTo.Should().BeEmpty("there is no /error redirect for handled errors");
        (await IsHandled(response)).Should().BeTrue();
        // The body can still be read by the caller after the handler has read it.
        (await response.Content.ReadAsStringAsync()).Should().Contain("An error occurred");
    }

    // Review finding: a transport failure navigated to /error, a route that did not exist.
    [Fact]
    public async Task NetworkFailure_ShowsToast_AndThrowsHandledApiException()
    {
        _api.Http.When(HttpMethod.Get, Api + "persons").Throw(new HttpRequestException("TypeError: Failed to fetch"));

        var exception = await Record.ExceptionAsync(() => _api.Client.GetAsync("persons"));

        var apiException = exception.Should().BeOfType<ApiException>().Subject;
        apiException.Handled.Should().BeTrue();
        apiException.StatusCode.Should().BeNull();
        apiException.InnerException.Should().BeOfType<HttpRequestException>();
        _api.VerifyErrorToast(ApiErrorHandler.ServerUnreachableMessage, Times.Once());
        _api.Navigation.NavigatedTo.Should().BeEmpty();
    }

    // Review finding: a transport failure on token/refresh threw a NullReferenceException in the interceptor.
    [Fact]
    public async Task NetworkFailure_DuringTokenRefresh_IsAHandledApiException_AndKeepsTheSession()
    {
        var expiring = TestJwt.Create(expires: DateTimeOffset.UtcNow.AddMinutes(1));
        _api.LocalStorage.StoreTokens(expiring, "refresh-1");
        _api.Http.When(HttpMethod.Post, Api + "token/refresh").Throw(new HttpRequestException("TypeError: Failed to fetch"));

        var exception = await Record.ExceptionAsync(() => _api.Client.GetAsync("persons"));

        exception.Should().BeOfType<ApiException>().Which.Handled.Should().BeTrue();
        _api.LocalStorage.AccessToken.Should().Be(expiring, "no answer from the server is not a refused refresh");
        _api.VerifyErrorToast(ApiErrorHandler.ServerUnreachableMessage, Times.Once());
    }

    // Review finding: every non-success status was thrown out of HttpClient as an HttpResponseException (e.g. 422).
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task OtherClientErrors_AreLeftToTheCaller(HttpStatusCode status)
    {
        _api.Http.When(HttpMethod.Put, Api + "persons/1").Respond(status, "text/plain", "Person does not exist");

        var response = await _api.Client.PutAsync("persons/1", new StringContent("{}"));

        response.StatusCode.Should().Be(status);
        (await IsHandled(response)).Should().BeFalse();
        _api.VerifyNoErrorToast();
        _api.Navigation.NavigatedTo.Should().BeEmpty();

        var exception = await Record.ExceptionAsync(() => response.EnsureSuccessAsync());
        exception.Should().BeOfType<ApiException>().Which.Message.Should().Be("Person does not exist");
    }

    [Fact]
    public async Task Success_IsPassedThrough()
    {
        _api.Http.When(HttpMethod.Delete, Api + "persons/1").Respond(HttpStatusCode.NoContent);

        var response = await _api.Client.DeleteAsync("persons/1");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _api.VerifyNoErrorToast();
        _api.Navigation.NavigatedTo.Should().BeEmpty();
    }
}
