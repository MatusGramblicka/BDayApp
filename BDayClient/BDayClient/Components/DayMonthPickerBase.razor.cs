using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BDayClient.Features;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

/// <summary>
/// Picks a day and a month with two selects. The value is a <see cref="DateOnly"/> in <see cref="DayMonth.StorageYear"/>.
/// </summary>
public abstract partial class DayMonthPickerBase<TValue>
{
    private bool _synced;
    private TValue? _syncedValue;

    protected int? SelectedMonth { get; private set; }
    protected int? SelectedDay { get; private set; }

    /// <summary>Whether "no date" is a valid value (the selects then offer an empty option).</summary>
    protected abstract bool AllowEmpty { get; }

    protected int DaysInSelectedMonth =>
        SelectedMonth is { } month ? DateTime.DaysInMonth(DayMonth.StorageYear, month) : 31;

    protected abstract DateOnly? ToDate(TValue? value);

    protected abstract TValue FromDate(DateOnly? date);

    protected override void OnParametersSet()
    {
        // Re-read the selects only when the bound value really changed. A parent re-render passes the same value again,
        // and re-reading it would throw away a selection that is not a complete date yet (a month without a day).
        if (_synced && EqualityComparer<TValue>.Default.Equals(CurrentValue, _syncedValue))
            return;

        var date = ToDate(CurrentValue);
        if (date is null && !AllowEmpty)
        {
            // A required date never stays unset: start with today.
            date = DayMonth.InStorageYear(DateOnly.FromDateTime(DateTime.Today));
            CurrentValue = FromDate(date);
        }

        SelectedMonth = date?.Month;
        SelectedDay = date?.Day;
        _syncedValue = CurrentValue;
        _synced = true;
    }

    protected void OnMonthChanged(ChangeEventArgs e)
    {
        if (!TryReadSelection(e, maximum: 12, out var month))
            return;

        SelectedMonth = month;

        if (SelectedMonth is null)
            SelectedDay = null;
        else if (SelectedDay > DaysInSelectedMonth)
            SelectedDay = DaysInSelectedMonth; // keep a date when switching to a shorter month: 31 January -> 29 February

        UpdateValue();
    }

    protected void OnDayChanged(ChangeEventArgs e)
    {
        if (!TryReadSelection(e, maximum: DaysInSelectedMonth, out var day))
            return;

        SelectedDay = day;
        UpdateValue();
    }

    // The value is only parsed from text when CurrentValueAsString is set, which these selects never do,
    // but InputBase requires it. Accepts "dd.MM".
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        validationErrorMessage = null;

        if (string.IsNullOrWhiteSpace(value) && AllowEmpty)
        {
            result = FromDate(null);
            return true;
        }

        if (DateOnly.TryParseExact($"{value?.Trim()}.{DayMonth.StorageYear}", "d.M.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            result = FromDate(date);
            return true;
        }

        result = default;
        validationErrorMessage = "Expected a day and month in the format dd.MM.";
        return false;
    }

    protected override string? FormatValueAsString(TValue? value) =>
        ToDate(value) is { } date ? $"{date.Day:D2}.{date.Month:D2}" : null;

    private void UpdateValue()
    {
        DateOnly? date = SelectedMonth is { } month && SelectedDay is { } day
            ? new DateOnly(DayMonth.StorageYear, month, day)
            : null;

        // Setting CurrentValue also notifies the EditContext, so validation runs once per change.
        if (date is not null || AllowEmpty)
            CurrentValue = FromDate(date);

        _syncedValue = CurrentValue;
    }

    // The empty option is a valid choice only when the value may be empty; anything outside 1..maximum is ignored.
    private bool TryReadSelection(ChangeEventArgs e, int maximum, out int? selection)
    {
        var text = e.Value?.ToString();
        if (string.IsNullOrEmpty(text))
        {
            selection = null;
            return AllowEmpty;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) &&
            number >= 1 && number <= maximum)
        {
            selection = number;
            return true;
        }

        selection = null;
        return false;
    }
}
