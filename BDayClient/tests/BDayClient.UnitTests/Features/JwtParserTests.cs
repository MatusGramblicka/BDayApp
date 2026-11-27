using System.Security.Claims;
using System.Text;
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

    // Review finding: a role array with one element kept its quotes and brackets.
    [Fact]
    public void ParseClaimsFromJwt_RoleArrayWithOneElement_IsOneCleanRoleClaim()
    {
        var claims = JwtParser.ParseClaimsFromJwt(TestJwt.Create(roles: new[] { "Administrator" })).ToList();

        claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
            .Should().Equal("Administrator");
    }

    [Fact]
    public void ParseClaimsFromJwt_NoRoles_HasNoRoleClaim()
    {
        var claims = JwtParser.ParseClaimsFromJwt(TestJwt.Create()).ToList();

        claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
    }

    // Review finding: the base64url payload was decoded as plain base64, which fails for '-' and '_'.
    [Theory]
    [InlineData("a?")]
    [InlineData("ab?")]
    [InlineData("abc~")]
    [InlineData("Matúš Gramblička")]
    [InlineData("Ľubomír Šťastný")]
    public void ParseClaimsFromJwt_PayloadWithBase64UrlCharacters_IsParsed(string name)
    {
        var claims = JwtParser.ParseClaimsFromJwt(TestJwt.Create(name)).ToList();

        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == name);
    }

    [Fact]
    public void TestJwt_UsesBase64UrlCharacters_ForTheNamesAbove()
    {
        // Guards the test above: at least one of its names must produce '-' or '_' in the payload.
        new[] { "a?", "ab?", "abc~", "Matúš Gramblička", "Ľubomír Šťastný" }
            .Select(name => TestJwt.Create(name).Split('.')[1])
            .Should().Contain(payload => payload.Contains('-') || payload.Contains('_'));
    }

    [Theory]
    [InlineData("a@b.c")] // payload lengths differ, so all base64 padding variants get exercised
    [InlineData("ab@b.c")]
    [InlineData("abc@b.c")]
    [InlineData("abcd@b.c")]
    public void ParseClaimsFromJwt_AllPaddingVariants_AreParsed(string name)
    {
        JwtParser.ParseClaimsFromJwt(TestJwt.Create(name))
            .Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == name);
    }

    [Theory]
    [InlineData("no-dots")]
    [InlineData("header.%%%.signature")]
    [InlineData("header.WzEsMl0.signature")] // payload [1,2] is not an object
    public void ParseClaimsFromJwt_MalformedToken_ThrowsFormatException(string token)
    {
        var parse = () => JwtParser.ParseClaimsFromJwt(token);

        parse.Should().Throw<FormatException>();
        JwtParser.TryParseClaims(token, out var claims).Should().BeFalse();
        claims.Should().BeNull();
    }

    [Fact]
    public void GetExpiry_ReadsTheExpClaim()
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(7);

        var expiry = JwtParser.GetExpiry(TestJwt.Create(expires: expires));

        expiry.Should().BeCloseTo(expires, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("""{"sub":"john"}""")]
    [InlineData("""{"exp":"soon"}""")]
    public void GetExpiry_NoUsableExpClaim_IsNull(string payloadJson)
    {
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        JwtParser.GetExpiry($"header.{payload}.signature").Should().BeNull();
    }

    [Fact]
    public void GetExpiry_MalformedToken_IsNull()
    {
        JwtParser.GetExpiry("not-a-jwt").Should().BeNull();
    }
}
