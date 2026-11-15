using Entities.RequestFeatures;

namespace BDayServer.UnitTests.Entities;

public class PagedListTests
{
    private static IQueryable<int> Numbers(int count) => Enumerable.Range(1, count).AsQueryable();

    [Fact]
    public void ToPagedList_ReturnsRequestedPage()
    {
        var page = PagedList<int>.ToPagedList(Numbers(25), pageNumber: 2, pageSize: 10);

        page.Should().Equal(Enumerable.Range(11, 10));
    }

    [Fact]
    public void ToPagedList_LastPage_ContainsRemainder()
    {
        var page = PagedList<int>.ToPagedList(Numbers(25), pageNumber: 3, pageSize: 10);

        page.Should().Equal(21, 22, 23, 24, 25);
    }

    [Fact]
    public void ToPagedList_FillsMetaData()
    {
        var page = PagedList<int>.ToPagedList(Numbers(25), pageNumber: 2, pageSize: 10);

        page.MetaData.Should().BeEquivalentTo(new
        {
            CurrentPage = 2,
            TotalPages = 3,
            PageSize = 10,
            TotalCount = 25,
            HasPrevious = true,
            HasNext = true
        });
    }

    [Fact]
    public void MetaData_SinglePage_HasNoPreviousOrNext()
    {
        var page = PagedList<int>.ToPagedList(Numbers(5), pageNumber: 1, pageSize: 10);

        page.MetaData.HasPrevious.Should().BeFalse();
        page.MetaData.HasNext.Should().BeFalse();
        page.MetaData.TotalPages.Should().Be(1);
    }

    [Fact]
    public void ToPagedList_EmptySource_HasZeroPages()
    {
        var page = PagedList<int>.ToPagedList(Numbers(0), pageNumber: 1, pageSize: 10);

        page.Should().BeEmpty();
        page.MetaData.TotalPages.Should().Be(0);
        page.MetaData.TotalCount.Should().Be(0);
    }
}
