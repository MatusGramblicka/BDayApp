using BDayClient.Components;

namespace BDayClient.UnitTests.Components;

public class DayMonthPickerTests : TestContext
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
        return RenderComponent<DayMonthPickerNullable>(ps => ps
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, v => _model.Birthday = v)
            .Add(p => p.ValueExpression, () => _model.Birthday));
    }

    private IRenderedComponent<DayMonthPickerNonNullable> RenderNonNullable(DateOnly value)
    {
        _model.NameDay = value;
        return RenderComponent<DayMonthPickerNonNullable>(ps => ps
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, v => _model.NameDay = v)
            .Add(p => p.ValueExpression, () => _model.NameDay));
    }

    private static int DayOptions<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent =>
        cut.FindAll("select")[1].QuerySelectorAll("option").Count(o => o.GetAttribute("value") != "");

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
    public void Nullable_SwitchingToShorterMonth_ClearsInvalidDay()
    {
        var cut = RenderNullable(new DateOnly(2000, 1, 31));

        cut.FindAll("select")[0].Change("2");

        _model.Birthday.Should().BeNull();
    }

    [Fact]
    public void Nullable_ClearingMonth_ClearsValue()
    {
        var cut = RenderNullable(new DateOnly(2000, 5, 10));

        cut.FindAll("select")[0].Change("");

        _model.Birthday.Should().BeNull();
    }

    [Fact]
    public void NonNullable_DefaultValue_IsInitialisedToTodayInFixedYear()
    {
        RenderNonNullable(default);

        var today = DateTime.Today;
        var expectedDay = Math.Min(today.Day, DateTime.DaysInMonth(2000, today.Month));
        _model.NameDay.Should().Be(new DateOnly(2000, today.Month, expectedDay));
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
}
