using BDayServer.Extensions;
using BDayServer.UnitTests.TestUtilities;
using Core.Services;
using Entities.Configuration;
using Entities.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BDayServer.UnitTests.Extensions;

public class AuthenticationRegistrationTests
{
    private static Dictionary<string, string?> ValidSettings() => new()
    {
        ["JWTSettings:securityKey"] = new string('k', 32),
        ["JWTSettings:validIssuer"] = "https://issuer.test",
        ["JWTSettings:validAudience"] = "https://audience.test",
        ["JWTSettings:expiryInMinutes"] = "5"
    };

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterAuthentication(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AccessTokenIssuedAtLogin_IsAcceptedByTheBearerAuthentication()
    {
        using var provider = Build(ValidSettings());
        var userManager = MockUserManager.Create();
        userManager.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(["Viewer"]);
        var token = await new AuthenticationService(provider.GetRequiredService<IOptions<JwtConfiguration>>(),
            userManager.Object).GetToken(new User { Email = "a@test.com" });

        var bearer = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, bearer.TokenValidationParameters);

        result.IsValid.Should().BeTrue(result.Exception?.Message);
        bearer.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task AccessTokenSignedWithAnotherKey_IsRejected()
    {
        using var provider = Build(ValidSettings());
        var otherSettings = ValidSettings();
        otherSettings["JWTSettings:securityKey"] = new string('x', 32);
        using var otherProvider = Build(otherSettings);
        var userManager = MockUserManager.Create();
        userManager.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync([]);
        var forged = await new AuthenticationService(otherProvider.GetRequiredService<IOptions<JwtConfiguration>>(),
            userManager.Object).GetToken(new User { Email = "a@test.com" });

        var bearer = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(forged, bearer.TokenValidationParameters);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("JWTSettings:securityKey", "too-short")]
    [InlineData("JWTSettings:validIssuer", "")]
    [InlineData("JWTSettings:validAudience", null)]
    [InlineData("JWTSettings:expiryInMinutes", "0")]
    public void MissingOrWeakSettings_FailValidation(string key, string? value)
    {
        var settings = ValidSettings();
        settings[key] = value;
        using var provider = Build(settings);

        var act = () => provider.GetRequiredService<IOptions<JwtConfiguration>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }
}
