using System.Security.Claims;
using BDayClient.AuthProvider;
using BDayClient.UnitTests.TestUtilities;
using Microsoft.AspNetCore.Components.Authorization;

namespace BDayClient.UnitTests.AuthProvider;

public class AuthStateProviderTests
{
    private readonly LocalStorageMock _localStorage = new();
    private readonly AuthStateProvider _sut;

    public AuthStateProviderTests()
    {
        _sut = new AuthStateProvider(new TokenStorage(_localStorage.Object));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetAuthenticationState_NoToken_IsAnonymous(string? token)
    {
        _localStorage.StoreTokens(token);

        var state = await _sut.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuthenticationState_WithToken_IsAuthenticatedWithNameAndRoles()
    {
        _localStorage.StoreTokens(TestJwt.Create("john@test.com", roles: "Administrator"));

        var state = await _sut.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeTrue();
        state.User.Identity.Name.Should().Be("john@test.com");
        state.User.IsInRole("Administrator").Should().BeTrue();
    }

    // Review finding: a damaged stored token threw inside GetAuthenticationStateAsync and broke the whole app.
    [Theory]
    [InlineData("not-a-jwt")]
    [InlineData("header.%%%.signature")]
    [InlineData("header.bm90IGpzb24.signature")] // payload "not json"
    public async Task GetAuthenticationState_DamagedToken_IsAnonymousAndRemoved(string token)
    {
        _localStorage.StoreTokens(token, "refresh");

        var state = await _sut.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
        _localStorage.AccessToken.Should().BeNull();
        _localStorage.RefreshToken.Should().BeNull();
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
