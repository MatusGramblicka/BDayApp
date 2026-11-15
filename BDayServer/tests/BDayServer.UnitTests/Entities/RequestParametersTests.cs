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
        parameters.ValidAgeRange.Should().BeTrue();
    }

    [Fact]
    public void EventParameters_DefaultOrderIsDate()
    {
        new EventParameters().OrderBy.Should().Be("date ascending");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(50, 50)]
    [InlineData(51, 50)]
    [InlineData(1000, 50)]
    public void PageSize_IsCappedAtFifty(int requested, int expected)
    {
        var parameters = new PersonParameters { PageSize = requested };

        parameters.PageSize.Should().Be(expected);
    }

    [Theory]
    [InlineData(10u, 20u, true)]
    [InlineData(20u, 20u, false)]
    [InlineData(30u, 20u, false)]
    public void ValidAgeRange_RequiresMaxGreaterThanMin(uint min, uint max, bool expected)
    {
        new PersonParameters { MinAge = min, MaxAge = max }.ValidAgeRange.Should().Be(expected);
    }
}
