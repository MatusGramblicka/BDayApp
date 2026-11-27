using BDayServer.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BDayServer.UnitTests.Security;

public class ClientUriValidatorTests
{
    private static ClientUriValidator Validator(string environment = "Production", params string[] allowedOrigins) =>
        new(Options.Create(new ClientAppOptions { AllowedOrigins = allowedOrigins }),
            Mock.Of<IHostEnvironment>(e => e.EnvironmentName == environment));

    private static HttpRequest Request(string scheme = "https", string host = "app.test")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        return context.Request;
    }

    [Theory]
    [InlineData("https://app.test/emailconfirmation")]
    [InlineData("https://APP.test/resetpassword?x=1")]
    [InlineData("https://app.test:443/emailconfirmation")]
    public void SameHostAsTheRequest_IsAllowed(string clientUri)
    {
        Validator().IsAllowed(clientUri, Request()).Should().BeTrue();
    }

    [Theory]
    [InlineData("http", "app.test")]
    [InlineData("https", "app.test:443")]
    [InlineData("http", "app.test:80")]
    public void SameHost_IsAllowedWhateverSchemeAndPortTheRequestShows(string scheme, string host)
    {
        // Behind Azure's front end the request may arrive as plain http.
        Validator().IsAllowed("https://app.test/emailconfirmation", Request(scheme, host)).Should().BeTrue();
    }

    [Theory]
    [InlineData("https://evil.test/emailconfirmation")]
    [InlineData("https://app.test.evil.test/emailconfirmation")]
    [InlineData("https://evil.test/app.test")]
    [InlineData("https://app.test@evil.test/emailconfirmation")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/emailconfirmation")]
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElse_IsRejected(string? clientUri)
    {
        Validator().IsAllowed(clientUri, Request()).Should().BeFalse();
    }

    [Fact]
    public void ConfiguredOrigin_IsAllowed()
    {
        Validator("Production", "https://client.test/").IsAllowed("https://client.test/emailconfirmation", Request())
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public void LocalhostLinkFromARequestToAnotherHost_IsOnlyAllowedInDevelopment(string environment, bool expected)
    {
        Validator(environment).IsAllowed("https://localhost:7001/emailconfirmation", Request(host: "app.test"))
            .Should().Be(expected);
    }
}
