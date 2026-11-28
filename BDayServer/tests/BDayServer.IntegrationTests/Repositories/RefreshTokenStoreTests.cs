using BDayServer.IntegrationTests.Fixtures;
using Core.Services;
using Microsoft.EntityFrameworkCore;

namespace BDayServer.IntegrationTests.Repositories;

/// <summary>Refresh tokens against a real SQL Server: one row per login, hashed, removed together with the user.</summary>
[Trait("Category", "Docker")]
[Collection(DatabaseCollection.Name)]
public class RefreshTokenStoreTests(MigratedDatabasesFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task EachLoginHasItsOwnToken_AndTheTokensGoWithTheUser()
    {
        var user = await fixture.CreateSqlServerUserAsync();

        await using (var context = fixture.SqlServer.CreateContext())
        {
            var store = new RefreshTokenStore(context, TimeProvider.System);
            var phone = await store.IssueAsync(user.Id, Ct);
            var laptop = await store.IssueAsync(user.Id, Ct);

            (await store.RotateAsync(user.Id, phone, Ct)).Should().NotBeNull();
            (await store.RotateAsync(user.Id, laptop, Ct)).Should().NotBeNull();
        }

        await using (var context = fixture.SqlServer.CreateContext())
        {
            (await context.RefreshTokens.CountAsync(t => t.UserId == user.Id, Ct)).Should().Be(4);

            await context.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync(Ct);

            (await context.RefreshTokens.CountAsync(t => t.UserId == user.Id, Ct)).Should().Be(0);
        }
    }
}
