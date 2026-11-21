using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Repository.Repositories;
using Testcontainers.MsSql;

namespace BDayServer.IntegrationTests.Fixtures;

/// <summary>
/// SQL Server container hosting the primary <see cref="DbRepositoryContext"/> (Azure SQL in production).
/// </summary>
public class SqlServerContainerFixture : IContainerDatabaseFixture<DbRepositoryContext>
{
    public const string MigrationsAssembly = "BDayServer";

    private static readonly string SqlServerImage =
        (Environment.GetEnvironmentVariable("DOCKER_HUB_PROXY_PREFIX") ?? "mcr.microsoft.com/")
        + "mssql/server:2022-latest";

    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage(SqlServerImage)
        .WithPassword("BDayServer_Tests1!")
        .Build();

    public string ConnectionString => _msSqlContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        await using var _ = await DatabaseConnectionRetry.OpenConnectionWithRetryAsync<SqlConnection, SqlException>(
            ConnectionString,
            "sqlserver-connection",
            connectionString => new SqlConnection(connectionString));
    }

    public async Task DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }

    public DbRepositoryContext CreateContext() => CreateContext(userName: null);

    /// <param name="userName">User whose data is visible through the global query filters.</param>
    public DbRepositoryContext CreateContext(string? userName) =>
        new(CreateOptions(ConnectionString), new TestUserProvider(userName));

    public static DbContextOptions<DbRepositoryContext> CreateOptions(string connectionString) =>
        new DbContextOptionsBuilder<DbRepositoryContext>()
            .UseSqlServer(connectionString, options => options.MigrationsAssembly(MigrationsAssembly))
            .Options;
}
