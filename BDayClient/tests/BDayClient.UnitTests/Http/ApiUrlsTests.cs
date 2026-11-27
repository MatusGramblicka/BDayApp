using BDayClient.Http;

namespace BDayClient.UnitTests.Http;

public class ApiUrlsTests
{
    private readonly ApiUrls _sut = new("https://api.test");

    [Theory]
    [InlineData("https://api.test")]
    [InlineData("https://api.test/")]
    public void BaseAddress_WithOrWithoutTrailingSlash_GivesSingleSlashApiRoot(string baseAddress)
    {
        var urls = new ApiUrls(baseAddress);

        urls.ServerBase.ToString().Should().Be("https://api.test/");
        urls.ApiBase.ToString().Should().Be("https://api.test/api/");
    }

    [Fact]
    public void BaseAddress_WithPath_KeepsThePath()
    {
        new ApiUrls("https://host.test/app/").ApiBase.ToString().Should().Be("https://host.test/app/api/");
    }

    [Theory]
    [InlineData("https://api.test/api/account/login")]
    [InlineData("https://api.test/api/account/twostepverification")]
    [InlineData("https://api.test/api/Account/ForgotPassword")]
    [InlineData("https://api.test/api/token/refresh")]
    public void IsAuthEndpoint_AccountAndTokenRoutes_AreAuthEndpoints(string uri)
    {
        _sut.IsAuthEndpoint(new Uri(uri)).Should().BeTrue();
    }

    // The old interceptor matched "token"/"account" anywhere in the path.
    [Theory]
    [InlineData("https://api.test/api/persons?searchTerm=account")]
    [InlineData("https://api.test/api/persons/account")]
    [InlineData("https://api.test/api/users/accounts")]
    [InlineData("https://api.test/api/accounting/x")]
    [InlineData("https://api.test/api/tokens/x")]
    [InlineData("https://api.test/account/login")]          // not below /api/
    [InlineData("https://other.test/api/account/login")]    // another host
    public void IsAuthEndpoint_OtherPaths_AreNotAuthEndpoints(string uri)
    {
        _sut.IsAuthEndpoint(new Uri(uri)).Should().BeFalse();
    }

    [Theory]
    [InlineData("https://api.test/api/persons", true)]
    [InlineData("https://API.test/api/persons", true)]
    [InlineData("https://api.test/StaticFiles/Images/a.png", false)]
    [InlineData("https://other.test/api/persons", false)]
    [InlineData("http://api.test/api/persons", false)]
    public void IsApiRequest_OnlyForUrisBelowTheApiRoot(string uri, bool expected)
    {
        _sut.IsApiRequest(new Uri(uri)).Should().Be(expected);
    }

    [Fact]
    public void GetImageSource_RelativeUploadPath_IsResolvedAgainstTheServer()
    {
        _sut.GetImageSource("StaticFiles/Images/anna.png").Should().Be("https://api.test/StaticFiles/Images/anna.png");
        _sut.GetImageSource("/StaticFiles/Images/anna.png").Should().Be("https://api.test/StaticFiles/Images/anna.png");
    }

    [Fact]
    public void GetImageSource_AbsoluteUrlFromOlderRecords_IsKept()
    {
        _sut.GetImageSource("https://old-host.azurewebsites.net/StaticFiles/Images/anna.png")
            .Should().Be("https://old-host.azurewebsites.net/StaticFiles/Images/anna.png");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetImageSource_NoImage_IsNull(string? imageUrl)
    {
        _sut.GetImageSource(imageUrl).Should().BeNull();
    }
}
