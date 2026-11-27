using System.Collections.Concurrent;
using BDayClient.Components;

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
    public void Sort_EmptyValue_IsIgnored()
    {
        var raised = false;
        var cut = Render<Sort>(ps => ps.Add(p => p.OnSortChanged, _ => raised = true));

        cut.Find("select").Change("");

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

    [Fact]
    public void PageSizeDropDown_InvalidValue_IsIgnoredInsteadOfThrowing()
    {
        int? raised = null;
        var cut = Render<PageSizeDropDown>(ps => ps.Add(p => p.SelectedPageSize, size => raised = size));

        cut.Find("select").Change("abc");

        raised.Should().BeNull();
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(25);
    }

    // The search reacts to the input event, so pasting and autofill search too (not only key presses).
    [Fact]
    public async Task Search_RaisesTermAfterDebounce()
    {
        var raised = new ConcurrentQueue<string>();
        var cut = Render<Search>(ps => ps.Add(p => p.OnSearchChanged, term => raised.Enqueue(term)));

        cut.Find("input").Input("ann");

        raised.Should().BeEmpty("the search is debounced");
        await WaitUntilAsync(() => !raised.IsEmpty, TimeSpan.FromSeconds(3));
        raised.Should().Equal("ann");
    }

    [Fact]
    public async Task Search_TypingQuickly_RaisesOnlyLatestTerm()
    {
        var raised = new ConcurrentQueue<string>();
        var cut = Render<Search>(ps => ps.Add(p => p.OnSearchChanged, term => raised.Enqueue(term)));

        cut.Find("input").Input("a");
        cut.Find("input").Input("an");

        await WaitUntilAsync(() => !raised.IsEmpty, TimeSpan.FromSeconds(3));
        await Task.Delay(Search.DebounceDelay + TimeSpan.FromMilliseconds(200)); // the cancelled delay must not fire late
        raised.Should().Equal("an");
    }

    // Review finding: the pending timer fired after the component was gone.
    [Fact]
    public async Task Search_Disposed_DoesNotRaiseThePendingSearch()
    {
        var raised = new ConcurrentQueue<string>();
        var cut = Render<Search>(ps => ps.Add(p => p.OnSearchChanged, term => raised.Enqueue(term)));

        cut.Find("input").Input("ann");
        await DisposeComponentsAsync();

        await Task.Delay(Search.DebounceDelay + TimeSpan.FromMilliseconds(300));
        raised.Should().BeEmpty();
    }

    [Fact]
    public void Search_InputHasAccessibleName()
    {
        var cut = Render<Search>();

        cut.Find("input").GetAttribute("aria-label").Should().Be("Search by person name");
    }
}
