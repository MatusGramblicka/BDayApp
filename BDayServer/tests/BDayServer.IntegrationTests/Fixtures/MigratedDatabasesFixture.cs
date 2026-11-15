using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Repositories;

namespace BDayServer.IntegrationTests.Fixtures;

/// <summary>
/// Starts both database containers once, applies all migrations and shares them across the
/// <see cref="DatabaseCollection"/>. Tests isolate their data by creating their own users.
/// </summary>
public class MigratedDatabasesFixture : IAsyncLifetime
{
    public SqlServerContainerFixture SqlServer { get; } = new();

    public PostgreSqlContainerFixture PostgreSql { get; } = new();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(SqlServer.InitializeAsync(), PostgreSql.InitializeAsync());

        await using var sqlContext = SqlServer.CreateContext();
        await sqlContext.Database.MigrateAsync();

        await using var postgreContext = PostgreSql.CreateContext();
        await postgreContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(SqlServer.DisposeAsync(), PostgreSql.DisposeAsync());
    }

    /// <summary>Creates a user with a unique e-mail in the SQL Server identity store.</summary>
    public async Task<User> CreateSqlServerUserAsync()
    {
        var user = NewUser();
        await using var context = SqlServer.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    /// <summary>Creates a user with a unique e-mail in the PostgreSQL store.</summary>
    public async Task<User> CreatePostgreSqlUserAsync(User? template = null)
    {
        var user = template is null ? NewUser() : Clone(template);
        await using var context = PostgreSql.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static User NewUser()
    {
        // 46 characters - fits the 50 character limit configured for UserName/Email.
        var email = $"user-{Guid.NewGuid():N}@test.com";
        return new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            RefreshToken = string.Empty,
            // Unspecified kind - accepted by both datetime2 and PostgreSQL "timestamp without time zone".
            RefreshTokenExpiryTime = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }

    private static User Clone(User source) => new()
    {
        Id = source.Id,
        UserName = source.UserName,
        NormalizedUserName = source.NormalizedUserName,
        Email = source.Email,
        NormalizedEmail = source.NormalizedEmail,
        EmailConfirmed = source.EmailConfirmed,
        RefreshToken = source.RefreshToken,
        RefreshTokenExpiryTime = source.RefreshTokenExpiryTime,
        SecurityStamp = source.SecurityStamp
    };
}

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<MigratedDatabasesFixture>
{
    public const string Name = "Migrated databases";
}
