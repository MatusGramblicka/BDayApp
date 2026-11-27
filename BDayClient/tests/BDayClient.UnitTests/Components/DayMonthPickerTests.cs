using BDayClient.Components;

namespace BDayClient.UnitTests.Components;

public class DayMonthPickerTests : BunitContext
{
    // InputBase<T> needs a ValueExpression pointing at a model member.
    private sealed class Model
    {
        public DateOnly? Birthday { get; set; }
        public DateOnly NameDay { get; set; }
    }

    private readonly Model _model = new();

    private IRenderedComponent<DayMonthPickerNullable> RenderNullable(DateOnly? value)
    {
        _model.Birthday = value;
        return Render<DayMonthPickerNullable>(ps => ps
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, v => _model.Birthday = v)
            .Add(p => p.ValueExpression, () => _model.Birthday));
    }

    private IRenderedComponent<DayMonthPickerNonNullable> RenderNonNullable(DateOnly value)
    {
        _model.NameDay = value;
        return Render<DayMonthPickerNonNullable>(ps => ps
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, v => _model.NameDay = v)
            .Add(p => p.ValueExpression, () => _model.NameDay));
    }

    private static int DayOptions<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent =>
        cut.FindAll("select")[1].QuerySelectorAll("option").Count(o => o.GetAttribute("value") != "");

    private static string? SelectedMonth<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent =>
        cut.FindAll("select")[0].GetAttribute("value");

    [Fact]
    public void Nullable_NoValue_DisablesDaySelection()
    {
        var cut = RenderNullable(null);

        cut.FindAll("select")[1].HasAttribute("disabled").Should().BeTrue();
        DayOptions(cut).Should().Be(31);
    }

    [Fact]
    public void Nullable_SelectingMonthAndDay_SetsValueInFixedYear()
    {
        var cut = RenderNullable(null);

        cut.FindAll("select")[0].Change("2");
        cut.FindAll("select")[1].Change("29");

        _model.Birthday.Should().Be(new DateOnly(2000, 2, 29));
    }

    [Fact]
    public void Nullable_DayOptions_FollowSelectedMonth()
    {
        var cut = RenderNullable(null);

        cut.FindAll("select")[0].Change("4");

        DayOptions(cut).Should().Be(30);
    }

    [Fact]
    public void Nullable_SwitchingToShorterMonth_MovesTheDayToTheLastDayOfThatMonth()
    {
        var cut = RenderNullable(new DateOnly(2000, 1, 31));

        cut.FindAll("select")[0].Change("4");

        _model.Birthday.Should().Be(new DateOnly(2000, 4, 30));
    }

    [Fact]
    public void Nullable_ClearingMonth_ClearsValue()
    {
        var cut = RenderNullable(new DateOnly(2000, 5, 10));

        cut.FindAll("select")[0].Change("");

        _model.Birthday.Should().BeNull();
    }

    // Review finding: a month-only selection was lost on any parent re-render.
    [Fact]
    public void Nullable_MonthOnlySelection_SurvivesAParentRender()
    {
        var cut = RenderNullable(null);

        cut.FindAll("select")[0].Change("5");
        cut.Render(ps => ps.Add(p => p.Value, (DateOnly?)null));

        SelectedMonth(cut).Should().Be("5");
        cut.FindAll("select")[1].HasAttribute("disabled").Should().BeFalse();
        _model.Birthday.Should().BeNull("no day was chosen yet");
    }

    [Fact]
    public void Nullable_NewValueFromTheParent_IsShown()
    {
        var cut = RenderNullable(new DateOnly(2000, 5, 10));

        cut.Render(ps => ps.Add(p => p.Value, (DateOnly?)new DateOnly(2000, 8, 3)));

        SelectedMonth(cut).Should().Be("8");
        cut.FindAll("select")[1].GetAttribute("value").Should().Be("3");
    }

    [Fact]
    public void NonNullable_DefaultValue_IsInitialisedToTodayInFixedYear()
    {
        RenderNonNullable(default);

        var today = DateTime.Today;
        _model.NameDay.Should().Be(new DateOnly(2000, today.Month, today.Day));
    }

    [Fact]
    public void NonNullable_ExistingValue_IsKept_AndCanBeChanged()
    {
        var cut = RenderNonNullable(new DateOnly(2000, 7, 26));

        _model.NameDay.Should().Be(new DateOnly(2000, 7, 26));
        DayOptions(cut).Should().Be(31);

        cut.FindAll("select")[1].Change("4");

        _model.NameDay.Should().Be(new DateOnly(2000, 7, 4));
    }

    // Review finding: 31 January -> February cleared the day, nothing was saved, and the month snapped back.
    [Fact]
    public void NonNullable_SwitchingToShorterMonth_SavesTheLastDayOfThatMonth()
    {
        var cut = RenderNonNullable(new DateOnly(2000, 1, 31));

        cut.FindAll("select")[0].Change("2");
        cut.Render(ps => ps.Add(p => p.Value, _model.NameDay)); // the parent re-renders with the bound value

        _model.NameDay.Should().Be(new DateOnly(2000, 2, 29));
        SelectedMonth(cut).Should().Be("2");
        cut.FindAll("select")[1].GetAttribute("value").Should().Be("29");
    }

    [Fact]
    public void NonNullable_HasNoEmptyOptions()
    {
        var cut = RenderNonNullable(new DateOnly(2000, 7, 26));

        cut.FindAll("option").Should().NotContain(o => o.GetAttribute("value") == "");
        cut.FindAll("select")[1].HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Selects_HaveAccessibleNames_AndAdditionalAttributesReachTheGroup()
    {
        var cut = Render<DayMonthPickerNullable>(ps => ps
            .Add(p => p.Value, (DateOnly?)null)
            .Add(p => p.ValueChanged, v => _model.Birthday = v)
            .Add(p => p.ValueExpression, () => _model.Birthday)
            .AddUnmatched("aria-labelledby", "birthday-label"));

        cut.Find("[role=group]").GetAttribute("aria-labelledby").Should().Be("birthday-label");
        cut.FindAll("select").Select(s => s.GetAttribute("aria-label")).Should().Equal("Month", "Day");
    }
}
