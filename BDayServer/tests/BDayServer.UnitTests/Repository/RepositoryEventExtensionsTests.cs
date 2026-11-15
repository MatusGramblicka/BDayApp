using Entities.Models;
using Repository.Extensions;

namespace BDayServer.UnitTests.Repository;

public class RepositoryEventExtensionsTests
{
    private static readonly Event Wedding = new() { Name = "Wedding", Date = new DateTime(2015, 6, 20) };
    private static readonly Event Graduation = new() { Name = "Graduation", Date = new DateTime(2020, 2, 10) };
    private static readonly Event Anniversary = new() { Name = "Anniversary", Date = new DateTime(1999, 11, 1) };

    private static IQueryable<Event> Events() => new[] { Wedding, Graduation, Anniversary }.AsQueryable();

    [Fact]
    public void Search_FiltersByNameCaseInsensitive()
    {
        Events().Search("WED").Should().ContainSingle().Which.Should().Be(Wedding);
    }

    [Fact]
    public void Search_EmptyTerm_ReturnsAll()
    {
        Events().Search(null).Should().HaveCount(3);
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
    public void Sort_Default_OrdersByMonthThenDay(string orderBy)
    {
        Events().Sort(orderBy).Should().Equal(Graduation, Wedding, Anniversary);
    }

    [Fact]
    public void Sort_ByName_UsesDynamicLinq()
    {
        Events().Sort("name desc").Should().Equal(Wedding, Graduation, Anniversary);
    }
}
