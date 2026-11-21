using System.Collections.Concurrent;
using BDayClient.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BDayClient.UnitTests.Components;

public class FilterComponentsTests : BunitContext
{
    [Theory]
    [InlineData("name desc")]
    [InlineData("dayOfNameDay")]
    public void Sort_SelectionChange_RaisesOrderBy(string orderBy)
    {
        string? raised = null;
        var cut = Render<Sort>(ps => ps.Add(p => p.OnSortChanged, value => raised = value));

        cut.Find("select").Change(orderBy);

        raised.Should().Be(orderBy);
    }

    [Fact]
    public void Sort_DefaultSelection_IsBirthdayAscending()
    {
        var cut = Render<Sort>();

        cut.Find("option[selected]").GetAttribute("value").Should().Be("dayOfBirth");
    }

    [Fact]
    public void Sort_PlaceholderValue_IsIgnored()
    {
        var raised = false;
        var cut = Render<Sort>(ps => ps.Add(p => p.OnSortChanged, _ => raised = true));

        cut.Find("select").Change("-1");

        raised.Should().BeFalse();
    }

    [Theory]
    [InlineData("2", 2)]
    [InlineData("20", 20)]
    public void PageSizeDropDown_SelectionChange_RaisesPageSize(string selected, int expected)
    {
        int? raised = null;
        var cut = Render<PageSizeDropDown>(ps => ps.Add(p => p.SelectedPageSize, size => raised = size));

        cut.Find("select").Change(selected);

        raised.Should().Be(expected);
    }

    // The Search component debounces on a System.Threading.Timer, which raises the callback without
    // triggering a render - so bUnit's WaitForAssertion would never re-check. Poll instead.
    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(25);
    }

    private static void TypeAndRelease(IRenderedComponent<Search> cut, string text)
    {
        var input = cut.Find("input");
        input.Input(text);
        input.KeyUp(new KeyboardEventArgs());
    }

    [Fact]
    public async Task Search_RaisesTermAfterDebounce()
    {
        var raised = new ConcurrentQueue<string>();
        var cut = Render<Search>(ps => ps.Add(p => p.OnSearchChanged, term => raised.Enqueue(term)));

        TypeAndRelease(cut, "ann");

        raised.Should().BeEmpty("the search is debounced");
        await WaitUntilAsync(() => !raised.IsEmpty, TimeSpan.FromSeconds(3));
        raised.Should().Equal("ann");
    }

    [Fact]
    public async Task Search_TypingQuickly_RaisesOnlyLatestTerm()
    {
        var raised = new ConcurrentQueue<string>();
        var cut = Render<Search>(ps => ps.Add(p => p.OnSearchChanged, term => raised.Enqueue(term)));

        TypeAndRelease(cut, "a");
        TypeAndRelease(cut, "an");

        await WaitUntilAsync(() => !raised.IsEmpty, TimeSpan.FromSeconds(3));
        await Task.Delay(700); // make sure the first (cancelled) timer doesn't fire late
        raised.Should().Equal("an");
    }
}
