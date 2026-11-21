using Microsoft.EntityFrameworkCore;
using Npgsql;
using Repository.Repositories;
using Testcontainers.PostgreSql;

namespace BDayServer.IntegrationTests.Fixtures;

/// <summary>
/// PostgreSQL container hosting the <see cref="PostgreDbRepositoryContext"/> (used by the e-mail scheduler).
/// </summary>
public class PostgreSqlContainerFixture : IContainerDatabaseFixture<PostgreDbRepositoryContext>
{
    public const string MigrationsAssembly = "BDayServer";

    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("bdayserver_tests")
        .WithUsername("bdayserver")
        .WithPassword("bdayserver")
        .Build();

    public string ConnectionString => _postgreSqlContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        await using var _ = await DatabaseConnectionRetry.OpenConnectionWithRetryAsync<NpgsqlConnection, NpgsqlException>(
            ConnectionString,
            "postgresql-connection",
            connectionString => new NpgsqlConnection(connectionString));
    }

    public async Task DisposeAsync()
    {
        await _postgreSqlContainer.DisposeAsync();
    }

    public PostgreDbRepositoryContext CreateContext() => new(CreateOptions(ConnectionString));

    public static DbContextOptions<PostgreDbRepositoryContext> CreateOptions(string connectionString) =>
        new DbContextOptionsBuilder<PostgreDbRepositoryContext>()
            .UseNpgsql(connectionString, options => options.MigrationsAssembly(MigrationsAssembly))
            .Options;
}
