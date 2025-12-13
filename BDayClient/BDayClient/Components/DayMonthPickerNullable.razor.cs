namespace BDayClient.Components
{
    public partial class DayMonthPickerNullable
    {
        protected override void OnParametersSet()
        {
            if (CurrentValue.HasValue)
            {
                SelectedMonth = CurrentValue.Value.Month;
                SelectedDay = CurrentValue.Value.Day;
            }
            else
            {
                SelectedMonth = null;
                SelectedDay = null;
            }
        }

        protected override void UpdateValue()
        {
            if (SelectedMonth.HasValue && SelectedDay.HasValue)
                CurrentValue = new DateOnly(FixedYear, SelectedMonth.Value, SelectedDay.Value);
            else
                CurrentValue = null;
        }

        protected override bool TryParseValueFromString(string? value, out DateOnly? result, out string? validationErrorMessage)
        {
            result = null;
            validationErrorMessage = null;

            if (string.IsNullOrWhiteSpace(value))
                return true;

            var parts = value.Split('.');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var day) &&
                int.TryParse(parts[1], out var month))
            {
                result = new DateOnly(FixedYear, month, day);
                return true;
            }

            validationErrorMessage = "Expected format: dd.MM";
            return false;
        }

        protected override string? FormatValueAsString(DateOnly? value)
            => value is null ? null : $"{value.Value.Day:D2}.{value.Value.Month:D2}";
    }
}
