using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BDayClient.Components
{
    public abstract class DayMonthPickerBase<T> : InputBase<T>
    {
        protected int? SelectedMonth { get; set; }
        protected int? SelectedDay { get; set; }
        protected int FixedYear { get; set; } = 2000;

        protected int DaysInSelectedMonth =>
            SelectedMonth.HasValue
                ? DateTime.DaysInMonth(FixedYear, SelectedMonth.Value)
                : 31;

        protected void OnMonthChanged(ChangeEventArgs e)
        {
            SelectedMonth = string.IsNullOrEmpty(e.Value?.ToString())
                ? null
                : int.Parse(e.Value!.ToString());

            if (SelectedDay.HasValue && SelectedDay.Value > DaysInSelectedMonth)
                SelectedDay = null;

            UpdateValue();
        }

        protected void OnDayChanged(ChangeEventArgs e)
        {
            SelectedDay = string.IsNullOrEmpty(e.Value?.ToString())
                ? null
                : int.Parse(e.Value!.ToString());

            UpdateValue();
        }

        protected abstract void UpdateValue();
    }
}
