using BDayClient.AuthProvider;
using BDayClient.HttpRepository;

namespace BDayClient.UnitTests.HttpRepository;

public class RefreshTokenServiceTests
{
    private readonly LocalStorageMock _localStorage = new();
    private readonly Mock<IAuthenticationService> _authService = new();
    private readonly RefreshTokenService _sut;

    public RefreshTokenServiceTests()
    {
        _sut = new RefreshTokenService(new TokenStorage(_localStorage.Object), _authService.Object);
        _authService.Setup(s => s.RefreshToken()).ReturnsAsync("new-jwt");
    }

    private string StoreTokenExpiringIn(TimeSpan expiresIn)
    {
        var token = TestJwt.Create(expires: DateTimeOffset.UtcNow.Add(expiresIn));
        _localStorage.StoreTokens(token);
        return token;
    }

    [Fact]
    public async Task TokenStillValidForLong_IsReturnedWithoutRefresh()
    {
        var token = StoreTokenExpiringIn(TimeSpan.FromMinutes(10));

        var result = await _sut.GetAccessTokenAsync();

        result.Should().Be(token);
        _authService.Verify(s => s.RefreshToken(), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-5)]
    public async Task TokenAboutToExpireOrExpired_IsRefreshed(int minutes)
    {
        StoreTokenExpiringIn(TimeSpan.FromMinutes(minutes));

        var result = await _sut.GetAccessTokenAsync();

        result.Should().Be("new-jwt");
        _authService.Verify(s => s.RefreshToken(), Times.Once);
    }

    // Review finding: with no token, Convert.ToInt64(null) made anonymous users look expired and posted empty tokens.
    [Fact]
    public async Task NotLoggedIn_ReturnsEmptyWithoutRefresh()
    {
        var result = await _sut.GetAccessTokenAsync();

        result.Should().BeEmpty();
        _authService.Verify(s => s.RefreshToken(), Times.Never);
    }

    [Fact]
    public async Task TokenWithoutExpClaim_IsReturnedWithoutRefresh()
    {
        const string tokenWithoutExp = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJqb2huIn0.signature"; // {"sub":"john"}
        _localStorage.StoreTokens(tokenWithoutExp);

        var result = await _sut.GetAccessTokenAsync();

        result.Should().Be(tokenWithoutExp);
        _authService.Verify(s => s.RefreshToken(), Times.Never);
    }

    [Fact]
    public async Task ConcurrentCalls_ShareOneRefresh()
    {
        StoreTokenExpiringIn(TimeSpan.FromMinutes(1));
        var refresh = new TaskCompletionSource<string>();
        _authService.Setup(s => s.RefreshToken()).Returns(refresh.Task);

        var calls = Enumerable.Range(0, 3).Select(_ => _sut.GetAccessTokenAsync()).ToList();
        refresh.SetResult("new-jwt");
        var results = await Task.WhenAll(calls);

        results.Should().OnlyContain(token => token == "new-jwt");
        _authService.Verify(s => s.RefreshToken(), Times.Once);
    }

    [Fact]
    public async Task AfterARefreshCompleted_TheNextExpiringTokenIsRefreshedAgain()
    {
        // A refresh that completes synchronously must not stay cached as "in flight".
        StoreTokenExpiringIn(TimeSpan.FromMinutes(1));
        _authService.SetupSequence(s => s.RefreshToken())
            .ReturnsAsync("first")
            .ReturnsAsync("second");

        var first = await _sut.GetAccessTokenAsync();
        StoreTokenExpiringIn(TimeSpan.FromMinutes(1));
        var second = await _sut.GetAccessTokenAsync();

        first.Should().Be("first");
        second.Should().Be("second");
        _authService.Verify(s => s.RefreshToken(), Times.Exactly(2));
    }

    [Fact]
    public async Task FailedRefresh_ReturnsEmpty()
    {
        StoreTokenExpiringIn(TimeSpan.FromMinutes(1));
        _authService.Setup(s => s.RefreshToken()).ReturnsAsync(string.Empty); // RefreshToken logged the user out

        var result = await _sut.GetAccessTokenAsync();

        result.Should().BeEmpty();
    }
}
