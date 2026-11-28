using Core.Services;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Repository.Repositories;
using Repository.Services;

namespace BDayServer.UnitTests.Core;

public sealed class RefreshTokenStoreTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 11, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Start);
    private readonly DbRepositoryContext _db = new(new DbContextOptionsBuilder<DbRepositoryContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options, Mock.Of<IGetUserProvider>());

    private readonly RefreshTokenStore _sut;

    public RefreshTokenStoreTests()
    {
        _sut = new RefreshTokenStore(_db, _clock);
    }

    public void Dispose() => _db.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private RefreshToken? Stored(string token) =>
        _db.RefreshTokens.AsNoTracking().SingleOrDefault(t => t.TokenHash == RefreshTokenStore.Hash(token));

    [Fact]
    public async Task Issue_StoresOnlyAHashOfTheTokenForSevenDays()
    {
        var token = await _sut.IssueAsync("user-1", Ct);

        Convert.FromBase64String(token).Should().HaveCount(32);
        var stored = Stored(token)!;
        stored.UserId.Should().Be("user-1");
        stored.TokenHash.Should().HaveLength(64).And.NotContain(token);
        stored.ExpiresAt.Should().Be(Start.UtcDateTime.AddDays(7));
        (await _db.RefreshTokens.AnyAsync(t => t.TokenHash == token, Ct)).Should().BeFalse();
    }

    [Fact]
    public async Task Issue_KeepsTheOtherLoginsOfTheUser()
    {
        var phone = await _sut.IssueAsync("user-1", Ct);
        var laptop = await _sut.IssueAsync("user-1", Ct);

        (await _sut.RotateAsync("user-1", phone, Ct)).Should().NotBeNull();
        (await _sut.RotateAsync("user-1", laptop, Ct)).Should().NotBeNull();
    }

    [Fact]
    public async Task Issue_ForgetsTheUsersEndedLogins()
    {
        var old = await _sut.IssueAsync("user-1", Ct);
        _clock.Advance(TimeSpan.FromDays(8));

        await _sut.IssueAsync("user-1", Ct);

        Stored(old).Should().BeNull();
    }

    [Fact]
    public async Task Rotate_ReplacesTheTokenButKeepsTheEndOfTheLogin()
    {
        var first = await _sut.IssueAsync("user-1", Ct);
        _clock.Advance(TimeSpan.FromDays(1));

        var second = await _sut.RotateAsync("user-1", first, Ct);

        second.Should().NotBeNull().And.NotBe(first);
        Stored(second!)!.ExpiresAt.Should().Be(Start.UtcDateTime.AddDays(7));
        Stored(first)!.ReplacedAt.Should().Be(Start.UtcDateTime.AddDays(1));
    }

    [Fact]
    public async Task Rotate_ReplacedTokenWithinTheGracePeriod_IsAccepted()
    {
        // Two tabs of one browser share the stored token and may refresh at the same moment.
        var first = await _sut.IssueAsync("user-1", Ct);
        await _sut.RotateAsync("user-1", first, Ct);
        _clock.Advance(TimeSpan.FromSeconds(30));

        (await _sut.RotateAsync("user-1", first, Ct)).Should().NotBeNull();
    }

    [Fact]
    public async Task Rotate_ReplacedTokenAfterTheGracePeriod_IsRejected()
    {
        var first = await _sut.IssueAsync("user-1", Ct);
        var second = await _sut.RotateAsync("user-1", first, Ct);
        _clock.Advance(RefreshTokenStore.ReplacedTokenGracePeriod + TimeSpan.FromSeconds(1));

        (await _sut.RotateAsync("user-1", first, Ct)).Should().BeNull();
        (await _sut.RotateAsync("user-1", second!, Ct)).Should().NotBeNull();
    }

    [Fact]
    public async Task Rotate_TokenOfAnotherUser_IsRejected()
    {
        var token = await _sut.IssueAsync("user-1", Ct);

        (await _sut.RotateAsync("user-2", token, Ct)).Should().BeNull();
    }

    [Fact]
    public async Task Rotate_EndedLogin_IsRejected()
    {
        var token = await _sut.IssueAsync("user-1", Ct);
        _clock.Advance(RefreshTokenStore.LoginLifetime);

        (await _sut.RotateAsync("user-1", token, Ct)).Should().BeNull();
    }

    [Fact]
    public async Task Rotate_RevokedToken_IsRejectedEvenWithinTheGracePeriod()
    {
        var token = await _sut.IssueAsync("user-1", Ct);

        await _sut.RevokeAsync(token, Ct);

        (await _sut.RotateAsync("user-1", token, Ct)).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
    public async Task Rotate_EmptyOrUnknownToken_IsRejected(string token)
    {
        await _sut.IssueAsync("user-1", Ct);

        (await _sut.RotateAsync("user-1", token, Ct)).Should().BeNull();
    }

    [Fact]
    public async Task RevokeAll_EndsEveryLoginOfThatUserOnly()
    {
        var phone = await _sut.IssueAsync("user-1", Ct);
        var laptop = await _sut.IssueAsync("user-1", Ct);
        var otherUser = await _sut.IssueAsync("user-2", Ct);

        await _sut.RevokeAllAsync("user-1", Ct);

        (await _sut.RotateAsync("user-1", phone, Ct)).Should().BeNull();
        (await _sut.RotateAsync("user-1", laptop, Ct)).Should().BeNull();
        (await _sut.RotateAsync("user-2", otherUser, Ct)).Should().NotBeNull();
    }

    [Fact]
    public async Task Revoke_UnknownToken_DoesNothing()
    {
        await _sut.Invoking(s => s.RevokeAsync("unknown", Ct)).Should().NotThrowAsync();
    }
}
