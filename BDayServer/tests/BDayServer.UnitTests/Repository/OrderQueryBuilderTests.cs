using Contracts.DataTransferObjects.Person;
using Repository.Extensions.Utility;

namespace BDayServer.UnitTests.Repository;

public class OrderQueryBuilderTests
{
    [Fact]
    public void CreateOrderQuery_SingleProperty_DefaultsToAscending()
    {
        var result = OrderQueryBuilder.CreateOrderQuery<PersonDto>("name");

        result.Should().Be("Name ascending");
    }

    [Fact]
    public void CreateOrderQuery_DescSuffix_ProducesDescending()
    {
        var result = OrderQueryBuilder.CreateOrderQuery<PersonDto>("surname desc");

        result.Should().Be("Surname descending");
    }

    [Fact]
    public void CreateOrderQuery_IsCaseInsensitive_AndUsesRealPropertyName()
    {
        var result = OrderQueryBuilder.CreateOrderQuery<PersonDto>("DAYOFBIRTH");

        result.Should().Be("DayOfBirth ascending");
    }

    [Fact]
    public void CreateOrderQuery_MultipleProperties_AreCommaSeparated()
    {
        var result = OrderQueryBuilder.CreateOrderQuery<PersonDto>("name desc,surname");

        result.Should().Be("Name descending, Surname ascending");
    }

    [Fact]
    public void CreateOrderQuery_UnknownProperty_IsIgnored()
    {
        var result = OrderQueryBuilder.CreateOrderQuery<PersonDto>("doesNotExist,name");

        result.Should().Be("Name ascending");
    }

    [Theory]
    [InlineData("doesNotExist")]
    [InlineData(",")]
    [InlineData("   ")]
    public void CreateOrderQuery_NoValidProperties_ReturnsEmpty(string query)
    {
        var result = OrderQueryBuilder.CreateOrderQuery<PersonDto>(query);

        result.Should().BeEmpty();
    }
}
