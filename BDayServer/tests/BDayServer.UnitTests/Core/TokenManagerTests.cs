using System.Security.Claims;
using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.Auth;
using Contracts.Exceptions;
using Core.Managers;
using Core.Services.Interfaces;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace BDayServer.UnitTests.Core;

public class TokenManagerTests
{
    private const string Email = "john@test.com";

    private readonly Mock<UserManager<User>> _userManager = MockUserManager.Create();
    private readonly Mock<IAuthenticationService> _authService = new();
    private readonly Mock<IRefreshTokenStore> _refreshTokens = new();
    private readonly User _user = new() { Id = "user-1", Email = Email };
    private readonly RefreshTokenDto _dto = new() { Token = "expired-jwt", RefreshToken = "current-refresh" };
    private readonly TokenManager _sut;

    public TokenManagerTests()
    {
        _sut = new TokenManager(_userManager.Object, _authService.Object, _refreshTokens.Object);

        _authService.Setup(a => a.GetPrincipalFromExpiredToken("expired-jwt"))
            .Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, Email)], "jwt")));
        _authService.Setup(a => a.GetToken(_user)).ReturnsAsync("new-jwt");
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.IsLockedOutAsync(_user)).ReturnsAsync(false);
        _refreshTokens.Setup(s => s.RotateAsync(_user.Id, "current-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-refresh");
    }

    private async Task ShouldBeRejected(RefreshTokenDto dto)
    {
        var act = () => _sut.Refresh(dto);

        (await act.Should().ThrowAsync<TokenOperationException>()).WithMessage(TokenManager.InvalidRequestMessage);
        _authService.Verify(a => a.GetToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_NullDto_Throws()
    {
        var act = () => _sut.Refresh(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Refresh_ValidRequest_ReturnsANewAccessTokenAndTheReplacementRefreshToken()
    {
        var (token, refreshToken) = await _sut.Refresh(_dto);

        token.Should().Be("new-jwt");
        refreshToken.Should().Be("new-refresh");
    }

    [Fact]
    public Task Refresh_RefreshTokenTheStoreDoesNotAccept_IsRejected() =>
        ShouldBeRejected(_dto with { RefreshToken = "revoked-or-unknown" });

    [Fact]
    public async Task Refresh_LockedOutUser_IsRejectedWithoutTouchingItsTokens()
    {
        _userManager.Setup(m => m.IsLockedOutAsync(_user)).ReturnsAsync(true);

        await ShouldBeRejected(_dto);
        _refreshTokens.Verify(s => s.RotateAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public Task Refresh_UnknownUser_IsRejected()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync((User?)null);

        return ShouldBeRejected(_dto);
    }

    [Fact]
    public Task Refresh_AccessTokenWithABadSignature_IsRejected()
    {
        _authService.Setup(a => a.GetPrincipalFromExpiredToken("forged"))
            .Throws(new SecurityTokenSignatureKeyNotFoundException());

        return ShouldBeRejected(_dto with { Token = "forged" });
    }

    [Fact]
    public Task Refresh_MalformedAccessToken_IsRejected()
    {
        _authService.Setup(a => a.GetPrincipalFromExpiredToken("garbage")).Throws(new ArgumentException("IDX12741"));

        return ShouldBeRejected(_dto with { Token = "garbage" });
    }

    [Fact]
    public async Task Revoke_EndsTheLoginOfThatToken()
    {
        await _sut.Revoke(new RevokeTokenDto { RefreshToken = "current-refresh" });

        _refreshTokens.Verify(s => s.RevokeAsync("current-refresh", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Revoke_NullDto_Throws()
    {
        var act = () => _sut.Revoke(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
