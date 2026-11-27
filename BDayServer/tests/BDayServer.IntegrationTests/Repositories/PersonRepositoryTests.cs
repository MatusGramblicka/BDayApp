using BDayServer.IntegrationTests.Fixtures;
using Entities.Models;
using Entities.RequestFeatures;
using Repository;

namespace BDayServer.IntegrationTests.Repositories;

/// <summary>
/// Runs the person repository against a real SQL Server so that the LINQ translation
/// (DateOnly month/day ordering, dynamic ordering, paging, per-user query filters) is verified.
/// </summary>
[Trait("Category", "Docker")]
[Collection(DatabaseCollection.Name)]
public class PersonRepositoryTests(MigratedDatabasesFixture fixture)
{
    private async Task<(User Owner, Dictionary<string, Person> Persons)> SeedAsync()
    {
        var owner = await fixture.CreateSqlServerUserAsync();

        var persons = new[]
        {
            NewPerson(owner, "Anna", "Zeleny", new DateOnly(1990, 3, 15), new DateOnly(2000, 7, 26)),
            NewPerson(owner, "Boris", "Adamec", new DateOnly(1985, 1, 20), new DateOnly(2000, 5, 2)),
            NewPerson(owner, "Cyril", "Mraz", null, new DateOnly(2000, 1, 5)),
            NewPerson(owner, "Dana", "Kral", new DateOnly(2001, 3, 2), new DateOnly(2000, 12, 31))
        };

        await using var context = fixture.SqlServer.CreateContext(owner.UserName);
        var manager = new RepositoryManager(context);
        foreach (var person in persons)
            manager.Person.CreatePerson(person);
        await manager.SaveAsync();

        return (owner, persons.ToDictionary(p => p.Name));
    }

    private static Person NewPerson(User owner, string name, string surname, DateOnly? birth, DateOnly nameDay) => new()
    {
        Name = name,
        Surname = surname,
        DayOfBirth = birth,
        DayOfNameDay = nameDay,
        UserId = owner.Id
    };

    private async Task<PagedList<Contracts.DataTransferObjects.Person.PersonDto>> QueryAsync(User owner,
        PersonParameters parameters)
    {
        await using var context = fixture.SqlServer.CreateContext(owner.UserName);
        return await new RepositoryManager(context).Person.GetAllPersonsAsync(parameters);
    }

    [Fact]
    public async Task CreatePerson_GeneratesIdAndPersists()
    {
        var (owner, persons) = await SeedAsync();

        await using var context = fixture.SqlServer.CreateContext(owner.UserName);
        var loaded = await new RepositoryManager(context).Person.GetPersonAsync(persons["Anna"].Id);

        persons["Anna"].Id.Should().NotBeEmpty();
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Anna");
        loaded.DayOfBirth.Should().Be(new DateOnly(1990, 3, 15));
    }

    [Fact]
    public async Task GetAllPersons_OnlyReturnsPersonsOfCurrentUser()
    {
        var (owner, _) = await SeedAsync();
        var (otherOwner, _) = await SeedAsync();

        var result = await QueryAsync(owner, new PersonParameters { OrderBy = "" });

        result.MetaData.TotalCount.Should().Be(4);
        (await QueryAsync(otherOwner, new PersonParameters())).MetaData.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task GetPerson_OfAnotherUser_ReturnsNull()
    {
        var (_, persons) = await SeedAsync();
        var stranger = await fixture.CreateSqlServerUserAsync();

        await using var context = fixture.SqlServer.CreateContext(stranger.UserName);
        var repository = new RepositoryManager(context).Person;

        (await repository.GetPersonAsync(persons["Anna"].Id)).Should().BeNull();
        (await repository.GetPersonAsync(persons["Anna"].Id, trackChanges: false)).Should().BeNull();
    }

    [Fact]
    public async Task GetAllPersons_DefaultOrder_IsByBirthdayMonthAndDay_WithMissingBirthdayLast()
    {
        var (owner, _) = await SeedAsync();

        var result = await QueryAsync(owner, new PersonParameters());

        result.Select(p => p.Name).Should().Equal("Boris", "Dana", "Anna", "Cyril");
    }

    [Fact]
    public async Task GetAllPersons_OrderByBirthdayDescending()
    {
        var (owner, _) = await SeedAsync();

        var result = await QueryAsync(owner, new PersonParameters { OrderBy = "dayOfBirth desc" });

        result.Select(p => p.Name).Should().Equal("Cyril", "Anna", "Dana", "Boris");
    }

    [Fact]
    public async Task GetAllPersons_OrderByNameDay()
    {
        var (owner, _) = await SeedAsync();

        var result = await QueryAsync(owner, new PersonParameters { OrderBy = "dayOfNameDay" });

        result.Select(p => p.Name).Should().Equal("Cyril", "Boris", "Anna", "Dana");
    }

    [Fact]
    public async Task GetAllPersons_OrderBySurnameDescending_UsesDynamicOrdering()
    {
        var (owner, _) = await SeedAsync();

        var result = await QueryAsync(owner, new PersonParameters { OrderBy = "surname desc" });

        result.Select(p => p.Name).Should().Equal("Anna", "Cyril", "Dana", "Boris");
    }

    [Fact]
    public async Task GetAllPersons_SearchTerm_IsCaseInsensitive()
    {
        var (owner, _) = await SeedAsync();

        var result = await QueryAsync(owner, new PersonParameters { SearchTerm = " AN " });

        result.Select(p => p.Name).Should().BeEquivalentTo("Anna", "Dana");
        result.MetaData.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllPersons_Paging_ReturnsSliceAndMetadata()
    {
        var (owner, _) = await SeedAsync();

        var result = await QueryAsync(owner, new PersonParameters { PageNumber = 2, PageSize = 3 });

        result.Select(p => p.Name).Should().Equal("Cyril");
        result.MetaData.Should().BeEquivalentTo(new
        {
            CurrentPage = 2,
            TotalPages = 2,
            PageSize = 3,
            TotalCount = 4,
            HasPrevious = true,
            HasNext = false
        });
    }

    [Fact]
    public async Task DeletePerson_RemovesRow()
    {
        var (owner, persons) = await SeedAsync();

        await using (var context = fixture.SqlServer.CreateContext(owner.UserName))
        {
            var manager = new RepositoryManager(context);
            var tracked = await manager.Person.GetPersonAsync(persons["Boris"].Id, trackChanges: true);
            manager.Person.DeletePerson(tracked!);
            await manager.SaveAsync();
        }

        await using var verifyContext = fixture.SqlServer.CreateContext(owner.UserName);
        (await new RepositoryManager(verifyContext).Person.GetPersonAsync(persons["Boris"].Id)).Should().BeNull();
    }

    [Fact]
    public async Task GetAllPersons_SearchTerm_AlsoMatchesTheSurname()
    {
        var (owner, _) = await SeedAsync();

        var result = await QueryAsync(owner, new PersonParameters { SearchTerm = "adam" });

        result.Select(p => p.Name).Should().Equal("Boris");
    }

    [Fact]
    public async Task GetAllPersons_SameBirthday_IsOrderedByName_SoPagesDoNotOverlap()
    {
        var owner = await fixture.CreateSqlServerUserAsync();
        await using (var context = fixture.SqlServer.CreateContext(owner.UserName))
        {
            var manager = new RepositoryManager(context);
            foreach (var name in new[] { "Eva", "Adam", "Ema", "Olga" })
                manager.Person.CreatePerson(NewPerson(owner, name, "Twin", new DateOnly(1990, 6, 1),
                    new DateOnly(2000, 1, 1)));
            await manager.SaveAsync();
        }

        var names = new List<string>();
        for (var page = 1; page <= 4; page++)
            names.AddRange((await QueryAsync(owner, new PersonParameters { PageNumber = page, PageSize = 1 }))
                .Select(p => p.Name));

        names.Should().Equal("Adam", "Ema", "Eva", "Olga");
    }

    [Fact]
    public async Task GetAllPersons_PageBeyondTheEnd_IsEmptyButCountsAllPersons()
    {
        var (owner, _) = await SeedAsync();

        var result = await QueryAsync(owner, new PersonParameters { PageNumber = 5, PageSize = 10 });

        result.Should().BeEmpty();
        result.MetaData.TotalCount.Should().Be(4);
        result.MetaData.HasNext.Should().BeFalse();
    }
}
