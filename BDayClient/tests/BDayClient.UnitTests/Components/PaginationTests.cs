using AngleSharp.Dom;
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

    private static IElement Button(IRenderedComponent<Pagination> cut, string text) =>
        cut.FindAll("li button").Single(button => button.TextContent.Trim() == text);

    [Fact]
    public void Renders_PreviousNextAndPagesWithinSpread()
    {
        var cut = RenderPagination(currentPage: 5, totalPages: 10, spread: 2);

        LinkTexts(cut).Should().Equal("Previous", "3", "4", "5", "6", "7", "Next");
    }

    [Fact]
    public void CurrentPage_IsMarkedActive_AndAsCurrentForScreenReaders()
    {
        var cut = RenderPagination(currentPage: 2, totalPages: 3);

        cut.FindAll("li.active").Should().ContainSingle().Which.TextContent.Trim().Should().Be("2");
        cut.FindAll("button[aria-current=page]").Should().ContainSingle().Which.TextContent.Trim().Should().Be("2");
    }

    [Fact]
    public void FirstPage_DisablesPrevious_LastPageDisablesNext()
    {
        var first = RenderPagination(currentPage: 1, totalPages: 3);
        first.FindAll("li")[0].ClassList.Should().Contain("disabled");
        Button(first, "Previous").HasAttribute("disabled").Should().BeTrue();
        first.FindAll("li")[^1].ClassList.Should().NotContain("disabled");

        var last = RenderPagination(currentPage: 3, totalPages: 3);
        last.FindAll("li")[0].ClassList.Should().NotContain("disabled");
        last.FindAll("li")[^1].ClassList.Should().Contain("disabled");
        Button(last, "Next").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Links_AreKeyboardReachableButtons()
    {
        var cut = RenderPagination(currentPage: 2, totalPages: 3);

        cut.FindAll("li").Should().OnlyContain(li => li.QuerySelector("button[type=button]") != null);
    }

    [Fact]
    public void ClickingAnotherPage_RaisesSelectedPage_WithoutChangingTheParentsMetaData()
    {
        var cut = RenderPagination(currentPage: 2, totalPages: 5);

        Button(cut, "4").Click();

        _selectedPages.Should().Equal(4);
        cut.Instance.MetaData.CurrentPage.Should().Be(2, "the parent passes new MetaData once the page is loaded");
    }

    [Fact]
    public void ClickingNext_RaisesFollowingPage()
    {
        var cut = RenderPagination(currentPage: 2, totalPages: 5);

        Button(cut, "Next").Click();

        _selectedPages.Should().Equal(3);
    }

    [Fact]
    public void ClickingCurrentPage_DoesNothing()
    {
        var cut = RenderPagination(currentPage: 1, totalPages: 3);

        Button(cut, "1").Click();

        _selectedPages.Should().BeEmpty();
    }
}
