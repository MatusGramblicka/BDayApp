using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BDayServer.UnitTests.TestUtilities;
using Core.Services;
using Entities.Configuration;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BDayServer.UnitTests.Core;

public class AuthenticationServiceTests
{
    private static readonly JwtConfiguration JwtSettings = new()
    {
        SecurityKey = "unit-test-security-key-that-is-long-enough-for-hs256",
        ValidIssuer = "https://issuer.test",
        ValidAudience = "https://audience.test",
        ExpiryInMinutes = 5
    };

    private readonly Mock<UserManager<User>> _userManager = MockUserManager.Create();
    private readonly User _user = new() { Email = "john@test.com", UserName = "john@test.com" };

    public AuthenticationServiceTests()
    {
        _userManager.Setup(m => m.GetRolesAsync(_user)).ReturnsAsync(["Viewer", "Administrator"]);
    }

    private AuthenticationService CreateSut(JwtConfiguration? settings = null) =>
        new(Options.Create(settings ?? JwtSettings), _userManager.Object);

    [Fact]
    public async Task GetToken_ContainsIssuerAudienceAndExpiry()
    {
        var token = await CreateSut().GetToken(_user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be(JwtSettings.ValidIssuer);
        jwt.Audiences.Should().ContainSingle().Which.Should().Be(JwtSettings.ValidAudience);
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromMinutes(1));
        jwt.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public async Task GetToken_ContainsNameAndRoleClaims()
    {
        var sut = CreateSut();
        var token = await sut.GetToken(_user);

        var principal = sut.GetPrincipalFromExpiredToken(token);

        principal.Identity!.Name.Should().Be(_user.Email);
        principal.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Should().BeEquivalentTo("Viewer", "Administrator");
    }

    [Fact]
    public async Task GetPrincipalFromExpiredToken_AcceptsExpiredToken()
    {
        var expiredSettings = new JwtConfiguration
        {
            SecurityKey = JwtSettings.SecurityKey,
            ValidIssuer = JwtSettings.ValidIssuer,
            ValidAudience = JwtSettings.ValidAudience,
            ExpiryInMinutes = -10
        };
        var sut = CreateSut(expiredSettings);
        var token = await sut.GetToken(_user);

        var principal = sut.GetPrincipalFromExpiredToken(token);

        principal.Identity!.Name.Should().Be(_user.Email);
    }

    [Fact]
    public async Task GetPrincipalFromExpiredToken_TokenSignedWithOtherKey_Throws()
    {
        var otherKeySettings = new JwtConfiguration
        {
            SecurityKey = "a-completely-different-key-which-is-also-long-enough",
            ValidIssuer = JwtSettings.ValidIssuer,
            ValidAudience = JwtSettings.ValidAudience,
            ExpiryInMinutes = 5
        };
        var foreignToken = await CreateSut(otherKeySettings).GetToken(_user);

        var act = () => CreateSut().GetPrincipalFromExpiredToken(foreignToken);

        act.Should().Throw<SecurityTokenException>();
    }

    [Fact]
    public async Task GetPrincipalFromExpiredToken_WrongIssuer_Throws()
    {
        var otherIssuer = new JwtConfiguration
        {
            SecurityKey = JwtSettings.SecurityKey,
            ValidIssuer = "https://evil.test",
            ValidAudience = JwtSettings.ValidAudience,
            ExpiryInMinutes = 5
        };
        var foreignToken = await CreateSut(otherIssuer).GetToken(_user);

        var act = () => CreateSut().GetPrincipalFromExpiredToken(foreignToken);

        act.Should().Throw<SecurityTokenInvalidIssuerException>();
    }

    [Fact]
    public void GenerateRefreshToken_Returns32RandomBytesAsBase64()
    {
        var sut = CreateSut();

        var first = sut.GenerateRefreshToken();
        var second = sut.GenerateRefreshToken();

        Convert.FromBase64String(first).Should().HaveCount(32);
        first.Should().NotBe(second);
    }
}
