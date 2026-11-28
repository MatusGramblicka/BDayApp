using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

public partial class EventSort
{
    /// <summary>Raised with the new OrderBy value; the empty string means "by month and day".</summary>
    [Parameter]
    public EventCallback<string> OnSortChanged { get; set; }

    private async Task ApplySort(ChangeEventArgs eventArgs)
        => await OnSortChanged.InvokeAsync(eventArgs.Value?.ToString() ?? string.Empty);
}
