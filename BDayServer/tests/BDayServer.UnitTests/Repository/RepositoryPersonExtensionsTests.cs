using Contracts.DataTransferObjects.Person;
using Repository.Extensions;

namespace BDayServer.UnitTests.Repository;

public class RepositoryPersonExtensionsTests
{
    private static readonly PersonDto Anna = new()
    {
        Name = "Anna", Surname = "Zeleny",
        DayOfBirth = new DateOnly(1990, 3, 15), DayOfNameDay = new DateOnly(2000, 7, 26)
    };

    private static readonly PersonDto Boris = new()
    {
        Name = "Boris", Surname = "Adamec",
        DayOfBirth = new DateOnly(1985, 1, 20), DayOfNameDay = new DateOnly(2000, 5, 2)
    };

    private static readonly PersonDto Cyril = new()
    {
        Name = "Cyril", Surname = "Mraz",
        DayOfBirth = null, DayOfNameDay = new DateOnly(2000, 1, 5)
    };

    private static readonly PersonDto Dana = new()
    {
        Name = "Dana", Surname = "Kral",
        DayOfBirth = new DateOnly(2001, 3, 2), DayOfNameDay = new DateOnly(2000, 12, 31)
    };

    private static IQueryable<PersonDto> Persons() => new[] { Anna, Boris, Cyril, Dana }.AsQueryable();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Search_EmptyTerm_ReturnsAll(string? term)
    {
        Persons().Search(term).Should().HaveCount(4);
    }

    [Fact]
    public void Search_IsCaseInsensitive_AndTrimmed()
    {
        Persons().Search("  AN ").Should().BeEquivalentTo([Anna, Dana]);
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        Persons().Search("xyz").Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknownProperty")]
    [InlineData("dayOfBirth")]
    [InlineData("dayOfBirth asc")]
    public void Sort_ByBirthdayAscending_OrdersByMonthThenDay_WithMissingBirthdayLast(string orderBy)
    {
        var result = Persons().Sort(orderBy).ToList();

        result.Should().Equal(Boris, Dana, Anna, Cyril);
    }

    [Fact]
    public void Sort_ByBirthdayDescending_PutsMissingBirthdayFirst()
    {
        var result = Persons().Sort("dayOfBirth desc").ToList();

        result.Should().Equal(Cyril, Anna, Dana, Boris);
    }

    [Fact]
    public void Sort_ByNameDayAscending_IgnoresYear()
    {
        var result = Persons().Sort("dayOfNameDay").ToList();

        result.Should().Equal(Cyril, Boris, Anna, Dana);
    }

    [Fact]
    public void Sort_ByNameDayDescending()
    {
        var result = Persons().Sort("dayOfNameDay desc").ToList();

        result.Should().Equal(Dana, Anna, Boris, Cyril);
    }

    [Fact]
    public void Sort_ByOtherProperty_UsesDynamicLinq()
    {
        var result = Persons().Sort("surname desc").ToList();

        result.Should().Equal(Anna, Cyril, Dana, Boris);
    }
}
