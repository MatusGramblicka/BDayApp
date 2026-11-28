using System.Net;
using System.Net.Http.Json;
using System.Text;
using BDayClient.AuthProvider;
using BDayClient.Http;
using BDayClient.Pocos;
using Contracts.DataTransferObjects.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using RichardSzalay.MockHttp;
using AuthenticationService = BDayClient.HttpRepository.AuthenticationService;

namespace BDayClient.UnitTests.HttpRepository;

public class AuthenticationServiceTests
{
    private const string Api = "https://api.test/api/";

    private readonly MockHttpMessageHandler _http = new();
    private readonly LocalStorageMock _localStorage = new();
    private readonly HttpClient _client;
    private readonly AuthStateProvider _authStateProvider;
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        _client = _http.ToHttpClient();
        _client.BaseAddress = new Uri(Api);
        var tokenStorage = new TokenStorage(_localStorage.Object);
        _authStateProvider = new AuthStateProvider(tokenStorage);
        _sut = new AuthenticationService(new StubHttpClientFactory(_client), tokenStorage, _authStateProvider,
            new TestNavigationManager());
    }

    private static HttpContent Json(object value) => JsonContent.Create(value);

    private static HttpContent ProblemJson(string json) =>
        new StringContent(json, Encoding.UTF8, "application/problem+json");

    private const string ValidationProblem = """
        {"title":"One or more validation errors occurred.","status":400,
         "errors":{"Email":["The Email field is not a valid e-mail address."]}}
        """;

    private void VerifyNothingStored() =>
        _localStorage.StorageMock.Verify(
            s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task Login_Success_StoresTokensAndNotifies()
    {
        var token = TestJwt.Create("john@test.com");
        _http.Expect(HttpMethod.Post, Api + "account/login")
            .WithJsonContent<Contracts.DataTransferObjects.User.UserForAuthenticationDto>(
                d => d.Email == "john@test.com" && d.Password == "pwd", TestJson.Web)
            .Respond(HttpStatusCode.OK, Json(new AuthResponseDto
            {
                IsAuthSuccessful = true, Token = token, RefreshToken = "refresh"
            }));
        Task<AuthenticationState>? notified = null;
        _authStateProvider.AuthenticationStateChanged += t => notified = t;

        var result = await _sut.Login(new UserForAuthentication { Email = "john@test.com", Password = "pwd" });

        result.IsAuthSuccessful.Should().BeTrue();
        _localStorage.AccessToken.Should().Be(token);
        _localStorage.RefreshToken.Should().Be("refresh");
        (await notified!).User.Identity!.Name.Should().Be("john@test.com");
        _client.DefaultRequestHeaders.Authorization.Should().BeNull("the token goes on each request (AuthTokenHandler)");
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Login_Unauthorized_ReturnsServerMessageWithoutStoringAnything()
    {
        _http.When(HttpMethod.Post, Api + "account/login")
            .Respond(HttpStatusCode.Unauthorized, Json(new AuthResponseDto { ErrorMessage = "Invalid Authentication" }));

        var result = await _sut.Login(new UserForAuthentication { Email = "a", Password = "b" });

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid Authentication");
        VerifyNothingStored();
    }

    [Fact]
    public async Task Login_PlainTextError_ReturnsFailedResultInsteadOfThrowing()
    {
        _http.When(HttpMethod.Post, Api + "account/login")
            .Respond(HttpStatusCode.BadRequest, "text/plain", "Unspecified problem");

        var result = await _sut.Login(new UserForAuthentication { Email = "a", Password = "b" });

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unspecified problem");
        VerifyNothingStored();
    }

    [Fact]
    public async Task Login_HtmlErrorPage_ReturnsFailedResultWithStatusCode()
    {
        // IIS answers with an HTML page, e.g. 503 while the site restarts after a deploy.
        _http.When(HttpMethod.Post, Api + "account/login")
            .Respond(HttpStatusCode.ServiceUnavailable, "text/html", "<html><body>Service Unavailable</body></html>");

        var result = await _sut.Login(new UserForAuthentication { Email = "a", Password = "b" });

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("503");
    }

    [Fact]
    public async Task Login_ValidationProblemDetails_ReturnsTheValidationMessage()
    {
        _http.When(HttpMethod.Post, Api + "account/login").Respond(HttpStatusCode.BadRequest, ProblemJson(ValidationProblem));

        var result = await _sut.Login(new UserForAuthentication { Email = "not-an-email", Password = "b" });

        result.ErrorMessage.Should().Be("The Email field is not a valid e-mail address.");
    }

    [Fact]
    public async Task Login_TwoStepRequired_ReturnsResponseWithoutStoringTokens()
    {
        _http.When(HttpMethod.Post, Api + "account/login")
            .Respond(HttpStatusCode.OK, Json(new AuthResponseDto { Is2StepVerificationRequired = true, Provider = "Email" }));

        var result = await _sut.Login(new UserForAuthentication { Email = "a", Password = "b" });

        result.Is2StepVerificationRequired.Should().BeTrue();
        result.Provider.Should().Be("Email");
        VerifyNothingStored();
    }

    [Fact]
    public async Task Logout_ClearsTokensAndNotifies()
    {
        _localStorage.StoreTokens("old", "old-refresh");
        Task<AuthenticationState>? notified = null;
        _authStateProvider.AuthenticationStateChanged += t => notified = t;

        await _sut.Logout();

        _localStorage.AccessToken.Should().BeNull();
        _localStorage.RefreshToken.Should().BeNull();
        (await notified!).User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_EndsThisLoginOnTheServer()
    {
        _localStorage.StoreTokens("old", "old-refresh");
        _http.Expect(HttpMethod.Post, Api + "token/revoke")
            .WithJsonContent<RevokeTokenDto>(d => d.RefreshToken == "old-refresh", TestJson.Web)
            .Respond(HttpStatusCode.NoContent);

        await _sut.Logout();

        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Logout_ServerUnreachable_StillLogsOutLocally()
    {
        _localStorage.StoreTokens("old", "old-refresh");
        _http.When(HttpMethod.Post, Api + "token/revoke").Throw(new HttpRequestException("offline"));

        await _sut.Logout();

        _localStorage.AccessToken.Should().BeNull();
        _localStorage.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task RefreshToken_SendsStoredTokens_StoresNewOnesAndNotifies()
    {
        var newToken = TestJwt.Create("john@test.com");
        _localStorage.StoreTokens("old-jwt", "old-refresh");
        _http.Expect(HttpMethod.Post, Api + "token/refresh")
            .WithJsonContent<RefreshTokenDto>(d => d.Token == "old-jwt" && d.RefreshToken == "old-refresh", TestJson.Web)
            .Respond(HttpStatusCode.OK, Json(new AuthResponseDto
            {
                IsAuthSuccessful = true, Token = newToken, RefreshToken = "new-refresh"
            }));
        Task<AuthenticationState>? notified = null;
        _authStateProvider.AuthenticationStateChanged += t => notified = t;

        var token = await _sut.RefreshToken();

        token.Should().Be(newToken);
        _localStorage.AccessToken.Should().Be(newToken);
        _localStorage.RefreshToken.Should().Be("new-refresh");
        (await notified!).User.Identity!.IsAuthenticated.Should().BeTrue();
        _http.VerifyNoOutstandingExpectation();
    }

    public static TheoryData<HttpStatusCode, HttpContent> RefusedRefreshResponses => new()
    {
        // The server refuses: an expired or already rotated refresh token.
        { HttpStatusCode.BadRequest, Json(new AuthResponseDto { IsAuthSuccessful = false, ErrorMessage = "Invalid client request" }) },
        { HttpStatusCode.BadRequest, new StringContent("Unspecified problem") },
        { HttpStatusCode.InternalServerError, ProblemJson("""{"title":"An error occurred.","status":500}""") },
        // 2xx, but not a usable answer.
        { HttpStatusCode.OK, Json(new AuthResponseDto { IsAuthSuccessful = false, Token = "x" }) },
        { HttpStatusCode.OK, Json(new AuthResponseDto { IsAuthSuccessful = true, Token = "" }) },
        { HttpStatusCode.OK, new StringContent("not json") }
    };

    // Review finding: a refused refresh stored "" for both tokens and left the user "logged in" with every call failing.
    [Theory]
    [MemberData(nameof(RefusedRefreshResponses))]
    public async Task RefreshToken_Refused_LogsOutAndReturnsEmpty(HttpStatusCode status, HttpContent content)
    {
        _localStorage.StoreTokens("old-jwt", "old-refresh");
        _http.When(HttpMethod.Post, Api + "token/refresh").Respond(status, content);
        Task<AuthenticationState>? notified = null;
        _authStateProvider.AuthenticationStateChanged += t => notified = t;

        var token = await _sut.RefreshToken();

        token.Should().BeEmpty();
        _localStorage.AccessToken.Should().BeNull();
        _localStorage.RefreshToken.Should().BeNull();
        (await notified!).User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshToken_NothingStored_LogsOutWithoutCallingTheServer()
    {
        var refresh = _http.When(HttpMethod.Post, Api + "token/refresh").Respond(HttpStatusCode.OK);

        var token = await _sut.RefreshToken();

        token.Should().BeEmpty();
        _http.GetMatchCount(refresh).Should().Be(0);
    }

    [Fact]
    public async Task RegisterUser_Created_IsSuccess_AndSendsConfirmationUriBasedOnAppBase()
    {
        _http.Expect(HttpMethod.Post, Api + "account/register")
            .WithJsonContent<Contracts.DataTransferObjects.User.UserForRegistrationDto>(
                d => d.Email == "new@test.com" && d.ClientUri == TestNavigationManager.Base + "emailconfirmation", TestJson.Web)
            .Respond(HttpStatusCode.Created);

        var result = await _sut.RegisterUser(new UserForRegistration
        {
            Email = "new@test.com", Password = "pwd", ConfirmPassword = "pwd"
        });

        result.IsSuccessfulRegistration.Should().BeTrue();
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RegisterUser_BadRequest_ReturnsServerErrors()
    {
        _http.When(HttpMethod.Post, Api + "account/register")
            .Respond(HttpStatusCode.BadRequest, Json(new ResponseDto { Errors = ["Email taken"] }));

        var result = await _sut.RegisterUser(new UserForRegistration { Email = "x@test.com" });

        result.IsSuccessfulRegistration.Should().BeFalse();
        result.Errors.Should().Equal("Email taken");
    }

    // Review finding: ProblemDetails ("errors" is an object) and plain-text 400s threw a JsonException; nothing was shown.
    [Fact]
    public async Task RegisterUser_ValidationProblemDetails_ReturnsTheMessages()
    {
        _http.When(HttpMethod.Post, Api + "account/register").Respond(HttpStatusCode.BadRequest, ProblemJson(ValidationProblem));

        var result = await _sut.RegisterUser(new UserForRegistration { Email = "x" });

        result.IsSuccessfulRegistration.Should().BeFalse();
        result.Errors.Should().Equal("The Email field is not a valid e-mail address.");
    }

    [Fact]
    public async Task RegisterUser_PlainTextError_ReturnsItAsError()
    {
        _http.When(HttpMethod.Post, Api + "account/register")
            .Respond(HttpStatusCode.BadRequest, "text/plain", "The confirmation email could not be sent.");

        var result = await _sut.RegisterUser(new UserForRegistration { Email = "x@test.com" });

        result.Errors.Should().Equal("The confirmation email could not be sent.");
    }

    // Review finding: the server answers 204, the client only accepted 200, so every success showed "failed".
    [Theory]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.OK)]
    public async Task ForgotPassword_Any2xx_IsSuccess_AndSendsResetUri(HttpStatusCode status)
    {
        _http.Expect(HttpMethod.Post, Api + "account/forgotpassword")
            .WithJsonContent<ForgotPasswordDto>(
                d => d.Email == "x@test.com" && d.ClientUri == TestNavigationManager.Base + "resetpassword", TestJson.Web)
            .Respond(status);

        var result = await _sut.ForgotPassword(new ForgotPassword { Email = "x@test.com" });

        result.Succeeded.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ForgotPassword_ValidationProblem_ReturnsTheMessages()
    {
        _http.When(HttpMethod.Post, Api + "account/forgotpassword").Respond(HttpStatusCode.BadRequest, ProblemJson(ValidationProblem));

        var result = await _sut.ForgotPassword(new ForgotPassword { Email = "x" });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Equal("The Email field is not a valid e-mail address.");
    }

    [Fact]
    public async Task ResetPassword_NoContent_IsSuccess()
    {
        _http.Expect(HttpMethod.Post, Api + "account/resetpassword")
            .WithJsonContent<ResetPasswordDto>(d => d.Email == "x@test.com" && d.Token == "t", TestJson.Web)
            .Respond(HttpStatusCode.NoContent);

        var result = await _sut.ResetPassword(new ResetPassword { Email = "x@test.com", Token = "t", Password = "p" });

        result.IsResetPasswordSuccessful.Should().BeTrue();
        _http.VerifyNoOutstandingExpectation();
    }

    public static TheoryData<HttpContent, string> ResetPasswordFailures => new()
    {
        { Json(new ResetPasswordResponseDto { Errors = ["Passwords must have at least one digit."] }), "Passwords must have at least one digit." },
        // Review finding: plain text threw a JsonException (nothing shown), an AuthResponseDto showed an empty red box.
        { new StringContent("Unspecified problem"), "Unspecified problem" },
        { Json(new AuthResponseDto { ErrorMessage = "Invalid Request" }), "Invalid Request" }
    };

    [Theory]
    [MemberData(nameof(ResetPasswordFailures))]
    public async Task ResetPassword_BadRequest_ReturnsAMessageWhateverTheBody(HttpContent content, string expected)
    {
        _http.When(HttpMethod.Post, Api + "account/resetpassword").Respond(HttpStatusCode.BadRequest, content);

        var result = await _sut.ResetPassword(new ResetPassword { Email = "x@test.com", Token = "t", Password = "p" });

        result.IsResetPasswordSuccessful.Should().BeFalse();
        result.Errors.Should().Equal(expected);
    }

    [Fact]
    public async Task EmailConfirmation_NoContent_IsSuccess_AndPassesEmailAndTokenInQueryString()
    {
        _http.Expect(HttpMethod.Get, Api + "account/emailconfirmation")
            .WithExactQueryString(new Dictionary<string, string> { ["email"] = "x@test.com", ["token"] = "a+b" })
            .Respond(HttpStatusCode.NoContent);

        var result = await _sut.EmailConfirmation("x@test.com", "a+b");

        result.Succeeded.Should().BeTrue();
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task EmailConfirmation_BadRequest_ReturnsTheServerMessage()
    {
        _http.When(HttpMethod.Get, Api + "account/emailconfirmation")
            .Respond(HttpStatusCode.BadRequest, "text/plain", "Invalid token.");

        var result = await _sut.EmailConfirmation("x@test.com", "bad");

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Equal("Invalid token.");
    }

    [Fact]
    public async Task LoginVerification_Success_StoresTokens()
    {
        var token = TestJwt.Create();
        _http.When(HttpMethod.Post, Api + "account/twostepverification")
            .Respond(HttpStatusCode.OK, Json(new AuthResponseDto { IsAuthSuccessful = true, Token = token, RefreshToken = "refresh" }));

        var result = await _sut.LoginVerification(new TwoFactorVerificationDto { Email = "x", TwoFactorToken = "123" });

        result.IsAuthSuccessful.Should().BeTrue();
        _localStorage.AccessToken.Should().Be(token);
        _localStorage.RefreshToken.Should().Be("refresh");
    }

    [Fact]
    public async Task LoginVerification_Failure_ReturnsServerResponse()
    {
        _http.When(HttpMethod.Post, Api + "account/twostepverification")
            .Respond(HttpStatusCode.Unauthorized, Json(new AuthResponseDto { ErrorMessage = "Invalid Token Verification" }));

        var result = await _sut.LoginVerification(new TwoFactorVerificationDto { Email = "x", TwoFactorToken = "000" });

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid Token Verification");
        VerifyNothingStored();
    }

    [Fact]
    public async Task FailureAlreadyShownByApiErrorHandler_IsThrownAsHandled_NotReturnedAsMessage()
    {
        _http.When(HttpMethod.Post, Api + "account/forgotpassword").Respond(request =>
        {
            ApiErrorReader.MarkHandled(request); // what ApiErrorHandler does after showing the 429 toast
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests) { RequestMessage = request };
        });

        var exception = await Record.ExceptionAsync(() => _sut.ForgotPassword(new ForgotPassword { Email = "x@test.com" }));

        exception.Should().BeOfType<ApiException>().Which.Handled.Should().BeTrue();
    }
}
