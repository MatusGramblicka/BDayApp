namespace BDayClient.Components
{
    public partial class DayMonthPickerNonNullable
    {
        protected override void OnParametersSet()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var initialMonth = today.Month;
            var initialDay = Math.Min(today.Day, DateTime.DaysInMonth(FixedYear, today.Month));

            if (CurrentValue == default)
            {
                SelectedMonth = initialMonth;
                SelectedDay = initialDay;
                CurrentValue = new DateOnly(FixedYear, SelectedMonth.Value, SelectedDay.Value);
            }
            else
            {
                SelectedMonth = CurrentValue.Month;
                SelectedDay = CurrentValue.Day;
            }
        }

        protected override void UpdateValue()
        {
            if (SelectedMonth.HasValue && SelectedDay.HasValue)
            {
                CurrentValue = new DateOnly(FixedYear, SelectedMonth.Value, SelectedDay.Value);
                // notify EditContext so validation/UI updates immediately
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }
        }

        protected override bool TryParseValueFromString(
            string? value,
            out DateOnly result,
            out string? validationErrorMessage)
        {
            result = default;
            validationErrorMessage = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                // We never expect empty strings because the component is always in a valid state.
                // Return current value to keep model consistent.
                result = CurrentValue == default
                    ? new DateOnly(FixedYear, SelectedMonth ?? 1, SelectedDay ?? 1)
                    : CurrentValue;
                return true;
            }

            var parts = value.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var day) &&
                int.TryParse(parts[1], out var month))
            {
                try
                {
                    result = new DateOnly(FixedYear, month, day);
                    return true;
                }
                catch
                {
                    validationErrorMessage = "Invalid day/month.";
                    return false;
                }
            }

            validationErrorMessage = "Expected format: dd.MM";
            return false;
        }

        protected override string? FormatValueAsString(DateOnly value)
            => value == default ? null : $"{value.Day:D2}.{value.Month:D2}";
    }
}
