using System.Security.Claims;
using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.Auth;
using Contracts.Exceptions;
using Core.Managers;
using Core.Services.Interfaces;
using Entities.Models;
using Microsoft.AspNetCore.Identity;

namespace BDayServer.UnitTests.Core;

public class TokenManagerTests
{
    private const string Email = "john@test.com";

    private readonly Mock<UserManager<User>> _userManager = MockUserManager.Create();
    private readonly Mock<IAuthenticationService> _authService = new();
    private readonly TokenManager _sut;

    private readonly User _user = new()
    {
        Email = Email,
        RefreshToken = "current-refresh",
        RefreshTokenExpiryTime = DateTime.Now.AddDays(1)
    };

    private readonly RefreshTokenDto _dto = new() { Token = "expired-jwt", RefreshToken = "current-refresh" };

    public TokenManagerTests()
    {
        _sut = new TokenManager(_userManager.Object, _authService.Object);

        _authService.Setup(a => a.GetPrincipalFromExpiredToken("expired-jwt"))
            .Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, Email)], "jwt")));
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(_user);
    }

    [Fact]
    public async Task Refresh_NullDto_Throws()
    {
        var act = () => _sut.Refresh(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Refresh_TokenWithoutName_Throws()
    {
        _authService.Setup(a => a.GetPrincipalFromExpiredToken("expired-jwt"))
            .Returns(new ClaimsPrincipal(new ClaimsIdentity()));

        var act = () => _sut.Refresh(_dto);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Refresh_UnknownUser_Throws()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync((User?)null);

        var act = () => _sut.Refresh(_dto);

        await act.Should().ThrowAsync<TokenOperationException>();
    }

    [Fact]
    public async Task Refresh_RefreshTokenMismatch_Throws()
    {
        var act = () => _sut.Refresh(_dto with { RefreshToken = "stolen" });

        await act.Should().ThrowAsync<TokenOperationException>();
    }

    [Fact]
    public async Task Refresh_RefreshTokenExpired_Throws()
    {
        _user.RefreshTokenExpiryTime = DateTime.Now.AddMinutes(-1);

        var act = () => _sut.Refresh(_dto);

        await act.Should().ThrowAsync<TokenOperationException>();
    }

    [Fact]
    public async Task Refresh_Valid_RotatesRefreshTokenAndReturnsNewPair()
    {
        _authService.Setup(a => a.GetToken(_user)).ReturnsAsync("new-jwt");
        _authService.Setup(a => a.GenerateRefreshToken()).Returns("new-refresh");

        var (token, refresh) = await _sut.Refresh(_dto);

        token.Should().Be("new-jwt");
        refresh.Should().Be("new-refresh");
        _user.RefreshToken.Should().Be("new-refresh");
        _userManager.Verify(m => m.UpdateAsync(_user), Times.Once);
    }
}
