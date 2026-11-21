using BDayClient.Components;
using Entities.RequestFeatures;

namespace BDayClient.UnitTests.Components;

public class PaginationTests : BunitContext
{
    private readonly List<int> _selectedPages = [];

    private IRenderedComponent<Pagination> RenderPagination(int currentPage, int totalPages, int spread = 2) =>
        Render<Pagination>(ps => ps
            .Add(p => p.MetaData, new MetaData { CurrentPage = currentPage, TotalPages = totalPages, PageSize = 10 })
            .Add(p => p.Spread, spread)
            .Add(p => p.SelectedPage, page => _selectedPages.Add(page)));

    private static List<string> LinkTexts(IRenderedComponent<Pagination> cut) =>
        cut.FindAll("li").Select(li => li.TextContent.Trim()).ToList();

    [Fact]
    public void Renders_PreviousNextAndPagesWithinSpread()
    {
        var cut = RenderPagination(currentPage: 5, totalPages: 10, spread: 2);

        LinkTexts(cut).Should().Equal("Previous", "3", "4", "5", "6", "7", "Next");
    }

    [Fact]
    public void CurrentPage_IsMarkedActive()
    {
        var cut = RenderPagination(currentPage: 2, totalPages: 3);

        cut.FindAll("li.active").Should().ContainSingle().Which.TextContent.Trim().Should().Be("2");
    }

    [Fact]
    public void FirstPage_DisablesPrevious_LastPageDisablesNext()
    {
        var first = RenderPagination(currentPage: 1, totalPages: 3);
        first.FindAll("li")[0].ClassList.Should().Contain("disabled");
        first.FindAll("li")[^1].ClassList.Should().NotContain("disabled");

        var last = RenderPagination(currentPage: 3, totalPages: 3);
        last.FindAll("li")[0].ClassList.Should().NotContain("disabled");
        last.FindAll("li")[^1].ClassList.Should().Contain("disabled");
    }

    [Fact]
    public void ClickingAnotherPage_RaisesSelectedPage()
    {
        var cut = RenderPagination(currentPage: 2, totalPages: 5);

        cut.FindAll("li").Single(li => li.TextContent.Trim() == "4").Click();

        _selectedPages.Should().Equal(4);
        cut.Instance.MetaData.CurrentPage.Should().Be(4);
    }

    [Fact]
    public void ClickingNext_RaisesFollowingPage()
    {
        var cut = RenderPagination(currentPage: 2, totalPages: 5);

        cut.FindAll("li")[^1].Click();

        _selectedPages.Should().Equal(3);
    }

    [Fact]
    public void ClickingCurrentOrDisabledLink_DoesNothing()
    {
        var cut = RenderPagination(currentPage: 1, totalPages: 3);

        cut.FindAll("li").Single(li => li.TextContent.Trim() == "1").Click();
        cut.FindAll("li")[0].Click(); // disabled "Previous"

        _selectedPages.Should().BeEmpty();
    }
}
