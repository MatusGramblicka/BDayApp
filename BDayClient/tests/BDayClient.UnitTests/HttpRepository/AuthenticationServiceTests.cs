using System.Net;
using System.Net.Http.Json;
using BDayClient.AuthProvider;
using BDayClient.Pocos;
using BDayClient.UnitTests.TestUtilities;
using Blazored.LocalStorage;
using Contracts.DataTransferObjects.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using RichardSzalay.MockHttp;
using AuthenticationService = BDayClient.HttpRepository.AuthenticationService;

namespace BDayClient.UnitTests.HttpRepository;

public class AuthenticationServiceTests
{
    private const string Api = "https://api.test/api/";

    private readonly MockHttpMessageHandler _http = new();
    private readonly Mock<ILocalStorageService> _localStorage = new();
    private readonly HttpClient _client;
    private readonly AuthStateProvider _authStateProvider;
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        _client = _http.ToHttpClient();
        _client.BaseAddress = new Uri(Api);
        _authStateProvider = new AuthStateProvider(new HttpClient(), _localStorage.Object);
        _sut = new AuthenticationService(_client, _authStateProvider, _localStorage.Object, new TestNavigationManager());
    }

    private static HttpContent Json(object value) => JsonContent.Create(value);

    [Fact]
    public async Task Login_Success_StoresTokensNotifiesAndSetsHeader()
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
        _localStorage.Verify(s => s.SetItemAsync("authToken", token, It.IsAny<CancellationToken>()));
        _localStorage.Verify(s => s.SetItemAsync("refreshToken", "refresh", It.IsAny<CancellationToken>()));
        _client.DefaultRequestHeaders.Authorization!.Parameter.Should().Be(token);
        (await notified!).User.Identity!.IsAuthenticated.Should().BeTrue();
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Login_Unauthorized_ReturnsServerResponseWithoutStoringAnything()
    {
        _http.When(HttpMethod.Post, Api + "account/login")
            .Respond(HttpStatusCode.Unauthorized, Json(new AuthResponseDto { ErrorMessage = "Invalid Authentication" }));

        var result = await _sut.Login(new UserForAuthentication { Email = "a", Password = "b" });

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid Authentication");
        _localStorage.Verify(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _client.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task Login_TwoStepRequired_ReturnsResponseWithoutStoringTokens()
    {
        _http.When(HttpMethod.Post, Api + "account/login")
            .Respond(HttpStatusCode.OK, Json(new AuthResponseDto { Is2StepVerificationRequired = true, Provider = "Email" }));

        var result = await _sut.Login(new UserForAuthentication { Email = "a", Password = "b" });

        result.Is2StepVerificationRequired.Should().BeTrue();
        result.Provider.Should().Be("Email");
        _localStorage.Verify(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Logout_ClearsTokensAndHeader()
    {
        _client.DefaultRequestHeaders.Authorization = new("bearer", "old");
        Task<AuthenticationState>? notified = null;
        _authStateProvider.AuthenticationStateChanged += t => notified = t;

        await _sut.Logout();

        _localStorage.Verify(s => s.RemoveItemAsync("authToken", It.IsAny<CancellationToken>()));
        _localStorage.Verify(s => s.RemoveItemAsync("refreshToken", It.IsAny<CancellationToken>()));
        _client.DefaultRequestHeaders.Authorization.Should().BeNull();
        (await notified!).User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshToken_SendsStoredTokensAndStoresNewOnes()
    {
        _localStorage.Setup(s => s.GetItemAsync<string>("authToken", It.IsAny<CancellationToken>())).ReturnsAsync("old-jwt");
        _localStorage.Setup(s => s.GetItemAsync<string>("refreshToken", It.IsAny<CancellationToken>())).ReturnsAsync("old-refresh");
        _http.Expect(HttpMethod.Post, Api + "token/refresh")
            .WithJsonContent<RefreshTokenDto>(d => d.Token == "old-jwt" && d.RefreshToken == "old-refresh", TestJson.Web)
            .Respond(HttpStatusCode.OK, Json(new AuthResponseDto { Token = "new-jwt", RefreshToken = "new-refresh" }));

        var token = await _sut.RefreshToken();

        token.Should().Be("new-jwt");
        _localStorage.Verify(s => s.SetItemAsync("authToken", "new-jwt", It.IsAny<CancellationToken>()));
        _localStorage.Verify(s => s.SetItemAsync("refreshToken", "new-refresh", It.IsAny<CancellationToken>()));
        _client.DefaultRequestHeaders.Authorization!.Parameter.Should().Be("new-jwt");
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RegisterUser_SendsConfirmationUriBasedOnAppBase()
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

    [Fact]
    public async Task ForgotPassword_ReturnsStatusCode_AndSendsResetUri()
    {
        _http.Expect(HttpMethod.Post, Api + "account/forgotpassword")
            .WithJsonContent<ForgotPasswordDto>(d => d.ClientUri == TestNavigationManager.Base + "resetpassword", TestJson.Web)
            .Respond(HttpStatusCode.OK);

        var status = await _sut.ForgotPassword(new ForgotPassword { Email = "x@test.com" });

        status.Should().Be(HttpStatusCode.OK);
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task EmailConfirmation_PassesEmailAndTokenInQueryString()
    {
        _http.Expect(HttpMethod.Get, Api + "account/emailconfirmation")
            .WithExactQueryString(new Dictionary<string, string> { ["email"] = "x@test.com", ["token"] = "a+b" })
            .Respond(HttpStatusCode.OK);

        var status = await _sut.EmailConfirmation("x@test.com", "a+b");

        status.Should().Be(HttpStatusCode.OK);
        _http.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task LoginVerification_Success_StoresTokens()
    {
        var token = TestJwt.Create();
        _http.When(HttpMethod.Post, Api + "account/twostepverification")
            .Respond(HttpStatusCode.OK, Json(new AuthResponseDto { Token = token, RefreshToken = "refresh" }));

        var result = await _sut.LoginVerification(new TwoFactorVerificationDto { Email = "x", TwoFactorToken = "123" });

        result.IsAuthSuccessful.Should().BeTrue();
        _localStorage.Verify(s => s.SetItemAsync("authToken", token, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task LoginVerification_Failure_ReturnsServerResponse()
    {
        _http.When(HttpMethod.Post, Api + "account/twostepverification")
            .Respond(HttpStatusCode.Unauthorized, Json(new AuthResponseDto { ErrorMessage = "Invalid Token Verification" }));

        var result = await _sut.LoginVerification(new TwoFactorVerificationDto { Email = "x", TwoFactorToken = "000" });

        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid Token Verification");
    }
}
