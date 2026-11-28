using BDayServer.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace BDayServer.IntegrationTests.Migrations;

[Trait("Category", "Docker")]
public abstract class DatabaseMigrationTests<TFixture, TContext>(TFixture fixture) : IClassFixture<TFixture>
    where TFixture : class, IContainerDatabaseFixture<TContext>
    where TContext : DbContext
{
    protected TContext CreateContext() => fixture.CreateContext();

    [Fact]
    public async Task Database_CanBeCreatedByMigrations()
    {
        // Arrange
        await using var dbContext = CreateContext();

        // Act
        await dbContext.Database.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);
        var canConnect = await dbContext.Database.CanConnectAsync(TestContext.Current.CancellationToken);

        // Assert
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task Migrations_AreIdempotent()
    {
        // Arrange
        await using var dbContext = CreateContext();

        // Act
        var act = async () =>
        {
            await dbContext.Database.MigrateAsync();
            await dbContext.Database.MigrateAsync();
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PendingMigrations_DoNotExistAfterMigration()
    {
        // Arrange
        await using var dbContext = CreateContext();

        // Act
        await dbContext.Database.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        appliedMigrations.Should().NotBeEmpty();
        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public void Model_HasNoChangesMissingAMigration()
    {
        // Arrange
        using var dbContext = CreateContext();

        // Act
        var hasPendingModelChanges = dbContext.Database.HasPendingModelChanges();

        // Assert
        hasPendingModelChanges.Should().BeFalse(
            "the entity model was changed without adding a migration for {0}", typeof(TContext).Name);
    }
}
