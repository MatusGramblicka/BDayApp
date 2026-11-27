namespace BDayClient.Components;

/// <summary>A day/month picker for an optional date, e.g. the birthday.</summary>
public sealed class DayMonthPickerNullable : DayMonthPickerBase<DateOnly?>
{
    protected override bool AllowEmpty => true;

    protected override DateOnly? ToDate(DateOnly? value) => value;

    protected override DateOnly? FromDate(DateOnly? date) => date;
}
