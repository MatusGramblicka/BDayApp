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
    private readonly TokenManager _sut;

    private readonly User _user = new()
    {
        Email = Email,
        RefreshToken = "current-refresh",
        RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
    };

    private readonly RefreshTokenDto _dto = new() { Token = "expired-jwt", RefreshToken = "current-refresh" };

    public TokenManagerTests()
    {
        _sut = new TokenManager(_userManager.Object, _authService.Object);

        _authService.Setup(a => a.GetPrincipalFromExpiredToken("expired-jwt"))
            .Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, Email)], "jwt")));
        _authService.Setup(a => a.GetToken(_user)).ReturnsAsync("new-jwt");
        _authService.Setup(a => a.GenerateRefreshToken()).Returns("new-refresh");
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.IsLockedOutAsync(_user)).ReturnsAsync(false);
        _userManager.Setup(m => m.UpdateAsync(_user)).ReturnsAsync(IdentityResult.Success);
    }

    private async Task ShouldBeRejected(RefreshTokenDto dto)
    {
        var act = () => _sut.Refresh(dto);

        (await act.Should().ThrowAsync<TokenOperationException>()).WithMessage(TokenManager.InvalidRequestMessage);
        _userManager.Verify(m => m.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_NullDto_Throws()
    {
        var act = () => _sut.Refresh(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Refresh_ValidRequest_IssuesANewTokenAndRotatesTheRefreshToken()
    {
        var (token, refreshToken) = await _sut.Refresh(_dto);

        token.Should().Be("new-jwt");
        refreshToken.Should().Be("new-refresh");
        _user.RefreshToken.Should().Be("new-refresh");
        _userManager.Verify(m => m.UpdateAsync(_user), Times.Once);
    }

    [Fact]
    public Task Refresh_WrongRefreshToken_IsRejected() =>
        ShouldBeRejected(_dto with { RefreshToken = "other" });

    [Fact]
    public Task Refresh_RevokedRefreshToken_IsRejectedEvenWhenTheRequestSendsAnEmptyOne()
    {
        _user.RefreshToken = string.Empty;

        return ShouldBeRejected(_dto with { RefreshToken = string.Empty });
    }

    [Fact]
    public Task Refresh_ExpiredRefreshToken_IsRejected()
    {
        _user.RefreshTokenExpiryTime = DateTime.UtcNow.AddSeconds(-1);

        return ShouldBeRejected(_dto);
    }

    [Fact]
    public Task Refresh_LockedOutUser_IsRejected()
    {
        _userManager.Setup(m => m.IsLockedOutAsync(_user)).ReturnsAsync(true);

        return ShouldBeRejected(_dto);
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
    public async Task Refresh_ConcurrentUpdateWins_DoesNotHandOutTokensThatWereNotStored()
    {
        _userManager.Setup(m => m.UpdateAsync(_user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure()));

        var act = () => _sut.Refresh(_dto);

        await act.Should().ThrowAsync<TokenOperationException>();
    }
}
