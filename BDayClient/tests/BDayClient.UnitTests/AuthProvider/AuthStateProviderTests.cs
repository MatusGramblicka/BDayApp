using System.Security.Claims;
using BDayClient.AuthProvider;
using BDayClient.UnitTests.TestUtilities;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace BDayClient.UnitTests.AuthProvider;

public class AuthStateProviderTests
{
    private readonly HttpClient _httpClient = new();
    private readonly Mock<ILocalStorageService> _localStorage = new();
    private readonly AuthStateProvider _sut;

    public AuthStateProviderTests()
    {
        _sut = new AuthStateProvider(_httpClient, _localStorage.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetAuthenticationState_NoToken_IsAnonymous(string? token)
    {
        _localStorage.Setup(s => s.GetItemAsync<string>("authToken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token!);

        var state = await _sut.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
        _httpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task GetAuthenticationState_WithToken_IsAuthenticatedAndSetsBearerHeader()
    {
        var token = TestJwt.Create("john@test.com", roles: "Administrator");
        _localStorage.Setup(s => s.GetItemAsync<string>("authToken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var state = await _sut.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeTrue();
        state.User.Identity.Name.Should().Be("john@test.com");
        state.User.IsInRole("Administrator").Should().BeTrue();
        _httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("bearer");
        _httpClient.DefaultRequestHeaders.Authorization.Parameter.Should().Be(token);
    }

    [Fact]
    public async Task NotifyUserAuthentication_RaisesAuthenticatedState()
    {
        Task<AuthenticationState>? raised = null;
        _sut.AuthenticationStateChanged += t => raised = t;

        _sut.NotifyUserAuthentication(TestJwt.Create("jane@test.com"));

        raised.Should().NotBeNull();
        var user = (await raised!).User;
        user.Identity!.IsAuthenticated.Should().BeTrue();
        user.FindFirst(ClaimTypes.Name)!.Value.Should().Be("jane@test.com");
    }

    [Fact]
    public async Task NotifyUserLogout_RaisesAnonymousState()
    {
        Task<AuthenticationState>? raised = null;
        _sut.AuthenticationStateChanged += t => raised = t;

        _sut.NotifyUserLogout();

        (await raised!).User.Identity!.IsAuthenticated.Should().BeFalse();
    }
}
