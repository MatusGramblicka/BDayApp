using Entities.RequestFeatures;

namespace BDayServer.UnitTests.Entities;

public class RequestParametersTests
{
    [Fact]
    public void PersonParameters_HaveSensibleDefaults()
    {
        var parameters = new PersonParameters();

        parameters.PageNumber.Should().Be(1);
        parameters.PageSize.Should().Be(10);
        parameters.OrderBy.Should().Be("dayOfBirth ascending");
        parameters.SearchTerm.Should().BeNull();
    }

    [Fact]
    public void EventParameters_DefaultOrderIsDate()
    {
        new EventParameters().OrderBy.Should().Be("date ascending");
    }

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(50, 50)]
    [InlineData(51, 50)]
    [InlineData(1000, 50)]
    public void PageSize_IsClampedBetweenOneAndFifty(int requested, int expected)
    {
        new PersonParameters { PageSize = requested }.PageSize.Should().Be(expected);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(3, 3)]
    public void PageNumber_IsAtLeastOne(int requested, int expected)
    {
        new PersonParameters { PageNumber = requested }.PageNumber.Should().Be(expected);
    }
}
