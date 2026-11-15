using Microsoft.EntityFrameworkCore;

namespace BDayServer.IntegrationTests.Fixtures;

public interface IContainerDatabaseFixture<out TContext> : IAsyncLifetime
    where TContext : DbContext
{
    string ConnectionString { get; }

    TContext CreateContext();
}
