using BDayServer.IntegrationTests.Fixtures;
using Repository.Repositories;

namespace BDayServer.IntegrationTests.Migrations;

public class SqlServerMigrationTests(SqlServerContainerFixture fixture)
    : DatabaseMigrationTests<SqlServerContainerFixture, DbRepositoryContext>(fixture);
