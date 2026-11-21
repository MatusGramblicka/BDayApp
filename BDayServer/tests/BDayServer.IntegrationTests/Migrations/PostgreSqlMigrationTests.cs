using BDayServer.IntegrationTests.Fixtures;
using Repository.Repositories;

namespace BDayServer.IntegrationTests.Migrations;

public class PostgreSqlMigrationTests(PostgreSqlContainerFixture fixture)
    : DatabaseMigrationTests<PostgreSqlContainerFixture, PostgreDbRepositoryContext>(fixture);
