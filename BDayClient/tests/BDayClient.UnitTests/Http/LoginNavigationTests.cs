using BDayClient.Http;

namespace BDayClient.UnitTests.Http;

public class LoginNavigationTests
{
    private readonly TestNavigationManager _navigation = new();

    [Theory]
    [InlineData("/persons", "/login?returnUrl=%2Fpersons")]
    [InlineData("/updatePerson/5?x=1", "/login?returnUrl=%2FupdatePerson%2F5%3Fx%3D1")]
    [InlineData("/", "/login")]
    [InlineData("/logout", "/login")]
    public void NavigateToLogin_ReturnsToTheCurrentPage(string currentPage, string expected)
    {
        _navigation.NavigateTo(currentPage);
        _navigation.NavigatedTo.Clear();

        _navigation.NavigateToLogin();

        _navigation.NavigatedTo.Should().Equal(expected);
    }

    [Fact]
    public void NavigateToLogin_OnTheLoginPage_StaysThere()
    {
        _navigation.NavigateTo("/login?returnUrl=%2Fusers");
        _navigation.NavigatedTo.Clear();

        _navigation.NavigateToLogin();

        _navigation.NavigatedTo.Should().BeEmpty();
    }

    [Theory]
    [InlineData("/persons", "/persons")]
    [InlineData("/updatePerson/5?x=1", "/updatePerson/5?x=1")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("https://evil.test/", "/")]
    [InlineData("//evil.test/", "/")]
    [InlineData("/\\evil.test/", "/")]
    [InlineData("/\t/evil.test/", "/")]
    [InlineData("persons", "/")]
    [InlineData("/login", "/")]
    [InlineData("/logout", "/")]
    public void GetSafeReturnUrl_OnlyAllowsPagesOfThisApp(string? returnUrl, string expected)
    {
        LoginNavigation.GetSafeReturnUrl(returnUrl).Should().Be(expected);
    }
}
