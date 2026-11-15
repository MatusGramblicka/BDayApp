using System.Security.Claims;
using BDayClient.Features;
using BDayClient.UnitTests.TestUtilities;

namespace BDayClient.UnitTests.Features;

public class JwtParserTests
{
    [Fact]
    public void ParseClaimsFromJwt_ReturnsNameAndOtherClaims()
    {
        var claims = JwtParser.ParseClaimsFromJwt(TestJwt.Create("john@test.com")).ToList();

        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "john@test.com");
        claims.Should().Contain(c => c.Type == "iss" && c.Value == "https://issuer.test");
        claims.Should().Contain(c => c.Type == "exp");
    }

    [Fact]
    public void ParseClaimsFromJwt_SingleRole_IsOneRoleClaim()
    {
        var claims = JwtParser.ParseClaimsFromJwt(TestJwt.Create(roles: "Viewer")).ToList();

        claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
            .Should().Equal("Viewer");
    }

    [Fact]
    public void ParseClaimsFromJwt_RoleArray_IsSplitIntoSeparateClaims()
    {
        var claims = JwtParser.ParseClaimsFromJwt(TestJwt.Create(roles: new[] { "Viewer", "Administrator" })).ToList();

        claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
            .Should().BeEquivalentTo("Viewer", "Administrator");
    }

    [Fact]
    public void ParseClaimsFromJwt_NoRoles_HasNoRoleClaim()
    {
        var claims = JwtParser.ParseClaimsFromJwt(TestJwt.Create()).ToList();

        claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
    }

    [Theory]
    [InlineData("a@b.c")]          // payload lengths differ, so all base64 padding variants get exercised
    [InlineData("ab@b.c")]
    [InlineData("abc@b.c")]
    [InlineData("abcd@b.c")]
    public void GetLoggedUserName_ReturnsNameClaim(string name)
    {
        JwtParser.GetLoggedUserName(TestJwt.Create(name)).Should().Be(name);
    }

    [Fact]
    public void GetLoggedUserName_NoNameClaim_ReturnsEmpty()
    {
        JwtParser.GetLoggedUserName(TestJwt.Create(name: null)).Should().BeEmpty();
    }
}
