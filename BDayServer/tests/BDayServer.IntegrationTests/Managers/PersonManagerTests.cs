using BDayServer.IntegrationTests.Fixtures;
using BDayServer.Mapping;
using Contracts.DataTransferObjects.Person;
using Contracts.Exceptions;
using Core.Managers;
using Core.Managers.ManagerInterfaces;
using Core.Services;
using Core.Services.Interfaces;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository;
using Repository.DatabaseAccessInterfaces;
using Repository.Repositories;
using Repository.Services;

namespace BDayServer.IntegrationTests.Managers;

/// <summary>
/// End-to-end tests of <see cref="PersonManager"/> wired up like the application does,
/// writing to SQL Server (primary) and mirroring into PostgreSQL.
/// </summary>
[Trait("Category", "Docker")]
[Collection(DatabaseCollection.Name)]
public class PersonManagerTests(MigratedDatabasesFixture fixture)
{
    private static readonly DateOnly FarAway = DateOnly.FromDateTime(DateTime.Today.AddDays(100));

    private ServiceProvider BuildServices(string userName)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IGetUserProvider>(new TestUserProvider(userName));
        services.AddDbContext<DbRepositoryContext>(o =>
            o.UseSqlServer(fixture.SqlServer.ConnectionString));
        services.AddDbContext<PostgreDbRepositoryContext>(o =>
            o.UseNpgsql(fixture.PostgreSql.ConnectionString));
        services.AddIdentityCore<User>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<DbRepositoryContext>();
        services.AddAutoMapper(a => a.AddProfile<MappingProfile>());
        services.AddScoped<IRepositoryManager, RepositoryManager>();
        services.AddScoped<IPersonManager, PersonManager>();
        services.AddScoped<IPersonMirror, PostgresPersonMirror>();

        return services.BuildServiceProvider();
    }

    /// <summary>The PostgreSQL mirror has a FK to its own AspNetUsers table, so the user must exist in both.</summary>
    private async Task<User> CreateUserInBothDatabasesAsync()
    {
        var user = await fixture.CreateSqlServerUserAsync();
        await fixture.CreatePostgreSqlUserAsync(user);
        return user;
    }

    private static PersonForCreationDto NewPerson(string name = "Anna") => new()
    {
        Name = name,
        Surname = "Novak",
        DayOfBirth = FarAway.AddYears(-30),
        DayOfNameDay = FarAway
    };

    private async Task<Person?> FindInPostgreSqlAsync(Guid id)
    {
        await using var context = fixture.PostgreSql.CreateContext();
        return await context.Persons.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
    }

    [Fact]
    public async Task CreatePerson_IsStoredInBothDatabasesWithTheSameId()
    {
        var user = await CreateUserInBothDatabasesAsync();
        await using var services = BuildServices(user.UserName!);
        using var scope = services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IPersonManager>();

        var created = await manager.CreatePersonAsync(NewPerson());

        created.Id.Should().NotBeEmpty();
        (await manager.GetPersonAsync(created.Id))!.Name.Should().Be("Anna");

        var mirrored = await FindInPostgreSqlAsync(created.Id);
        mirrored.Should().NotBeNull();
        mirrored!.UserId.Should().Be(user.Id);
        mirrored.Name.Should().Be("Anna");
        mirrored.DayOfBirth.Should().Be(created.DayOfBirth);
    }

    [Fact]
    public async Task UpdatePerson_UpdatesBothDatabases()
    {
        var user = await CreateUserInBothDatabasesAsync();
        await using var services = BuildServices(user.UserName!);

        Guid id;
        using (var scope = services.CreateScope())
        {
            id = (await scope.ServiceProvider.GetRequiredService<IPersonManager>().CreatePersonAsync(NewPerson())).Id;
        }

        using (var scope = services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPersonManager>().UpdatePersonAsync(id, new PersonForUpdateDto
            {
                Name = "Annabel",
                Surname = "Kral",
                DayOfBirth = null,
                DayOfNameDay = FarAway
            });
        }

        using (var scope = services.CreateScope())
        {
            var fromPrimary = await scope.ServiceProvider.GetRequiredService<IPersonManager>().GetPersonAsync(id);
            fromPrimary!.Name.Should().Be("Annabel");
            fromPrimary.DayOfBirth.Should().BeNull();
        }

        var mirrored = await FindInPostgreSqlAsync(id);
        mirrored!.Name.Should().Be("Annabel");
        mirrored.Surname.Should().Be("Kral");
        mirrored.DayOfBirth.Should().BeNull();
    }

    [Fact]
    public async Task DeletePerson_RemovesFromBothDatabases()
    {
        var user = await CreateUserInBothDatabasesAsync();
        await using var services = BuildServices(user.UserName!);

        Guid id;
        using (var scope = services.CreateScope())
        {
            id = (await scope.ServiceProvider.GetRequiredService<IPersonManager>().CreatePersonAsync(NewPerson())).Id;
        }

        using (var scope = services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPersonManager>().DeletePersonAsync(id);
        }

        using (var scope = services.CreateScope())
        {
            (await scope.ServiceProvider.GetRequiredService<IPersonManager>().GetPersonAsync(id)).Should().BeNull();
        }

        (await FindInPostgreSqlAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task GetPersons_ReturnsOnlyCurrentUsersPersons()
    {
        var owner = await CreateUserInBothDatabasesAsync();
        var other = await CreateUserInBothDatabasesAsync();

        await using (var ownerServices = BuildServices(owner.UserName!))
        using (var scope = ownerServices.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<IPersonManager>();
            await manager.CreatePersonAsync(NewPerson("Anna"));
            await manager.CreatePersonAsync(NewPerson("Boris"));
        }

        await using (var otherServices = BuildServices(other.UserName!))
        using (var scope = otherServices.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPersonManager>().CreatePersonAsync(NewPerson("Cyril"));
        }

        await using var services = BuildServices(owner.UserName!);
        using var readScope = services.CreateScope();
        var persons = await readScope.ServiceProvider.GetRequiredService<IPersonManager>()
            .GetPersonsAsync(new PersonParameters());

        persons.Select(p => p.Name).Should().BeEquivalentTo("Anna", "Boris");
    }

    [Fact]
    public async Task DeletePerson_OfAnotherUser_IsRejected()
    {
        var owner = await CreateUserInBothDatabasesAsync();
        var stranger = await CreateUserInBothDatabasesAsync();

        Guid id;
        await using (var ownerServices = BuildServices(owner.UserName!))
        using (var scope = ownerServices.CreateScope())
        {
            id = (await scope.ServiceProvider.GetRequiredService<IPersonManager>().CreatePersonAsync(NewPerson())).Id;
        }

        await using var strangerServices = BuildServices(stranger.UserName!);
        using var strangerScope = strangerServices.CreateScope();
        var act = () => strangerScope.ServiceProvider.GetRequiredService<IPersonManager>().DeletePersonAsync(id);

        await act.Should().ThrowAsync<PersonNotExistException>();
        (await FindInPostgreSqlAsync(id)).Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePerson_ForAUserWhoIsNotInPostgreSqlYet_CopiesTheUserFirst()
    {
        // Users are only created in SQL Server when they register; their first person used to fail on the mirror's FK.
        var user = await fixture.CreateSqlServerUserAsync();
        await using var services = BuildServices(user.UserName!);
        using var scope = services.CreateScope();

        var created = await scope.ServiceProvider.GetRequiredService<IPersonManager>().CreatePersonAsync(NewPerson());

        await using var context = fixture.PostgreSql.CreateContext();
        var copiedUser = await context.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        copiedUser.Email.Should().Be(user.Email);
        copiedUser.PasswordHash.Should().BeNull();
        (await FindInPostgreSqlAsync(created.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task UpdatePerson_MissingInPostgreSql_IsCopiedAgain()
    {
        var user = await CreateUserInBothDatabasesAsync();
        await using var services = BuildServices(user.UserName!);
        using var scope = services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IPersonManager>();
        var created = await manager.CreatePersonAsync(NewPerson());
        await using (var context = fixture.PostgreSql.CreateContext())
            await context.Persons.Where(p => p.Id == created.Id).ExecuteDeleteAsync();

        await manager.UpdatePersonAsync(created.Id, new PersonForUpdateDto { Name = "Hana", DayOfNameDay = FarAway });

        (await FindInPostgreSqlAsync(created.Id))!.Name.Should().Be("Hana");
    }

    [Fact]
    public async Task Mirror_DeleteUser_RemovesTheUserAndTheirPersonsFromPostgreSql()
    {
        var user = await CreateUserInBothDatabasesAsync();
        await using var services = BuildServices(user.UserName!);
        Guid personId;
        using (var scope = services.CreateScope())
            personId = (await scope.ServiceProvider.GetRequiredService<IPersonManager>()
                .CreatePersonAsync(NewPerson())).Id;

        using (var scope = services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IPersonMirror>().DeleteUserAsync(user.Id);

        (await FindInPostgreSqlAsync(personId)).Should().BeNull();
        await using var context = fixture.PostgreSql.CreateContext();
        (await context.Users.AnyAsync(u => u.Id == user.Id)).Should().BeFalse();
    }
}
