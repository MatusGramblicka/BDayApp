namespace BDayClient.Components;

/// <summary>A day/month picker for a required date, e.g. the nameday. An unset value starts as today.</summary>
public sealed class DayMonthPickerNonNullable : DayMonthPickerBase<DateOnly>
{
    protected override bool AllowEmpty => false;

    protected override DateOnly? ToDate(DateOnly value) => value == default ? null : value;

    protected override DateOnly FromDate(DateOnly? date) => date ?? default;
}
