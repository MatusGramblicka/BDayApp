using System.Security.Claims;
using BDayClient.HttpRepository;
using Microsoft.AspNetCore.Components.Authorization;

namespace BDayClient.UnitTests.HttpRepository;

public class RefreshTokenServiceTests
{
    private readonly Mock<AuthenticationStateProvider> _authStateProvider = new();
    private readonly Mock<IAuthenticationService> _authService = new();
    private readonly RefreshTokenService _sut;

    public RefreshTokenServiceTests()
    {
        _sut = new RefreshTokenService(_authStateProvider.Object, _authService.Object);
        _authService.Setup(s => s.RefreshToken()).ReturnsAsync("new-jwt");
    }

    private void SetupTokenExpiringIn(TimeSpan expiresIn)
    {
        var exp = DateTimeOffset.UtcNow.Add(expiresIn).ToUnixTimeSeconds().ToString();
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("exp", exp)], "jwt"));
        _authStateProvider.Setup(p => p.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(user));
    }

    [Fact]
    public async Task TryRefreshToken_TokenStillValidForLong_DoesNotRefresh()
    {
        SetupTokenExpiringIn(TimeSpan.FromMinutes(10));

        var result = await _sut.TryRefreshToken();

        result.Should().BeEmpty();
        _authService.Verify(s => s.RefreshToken(), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-5)]
    public async Task TryRefreshToken_TokenAboutToExpireOrExpired_Refreshes(int minutes)
    {
        SetupTokenExpiringIn(TimeSpan.FromMinutes(minutes));

        var result = await _sut.TryRefreshToken();

        result.Should().Be("new-jwt");
    }
}
