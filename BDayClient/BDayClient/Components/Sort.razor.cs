using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

public partial class Sort
{
    [Parameter]
    public EventCallback<string> OnSortChanged { get; set; }

    private async Task ApplySort(ChangeEventArgs eventArgs)
    {
        if (eventArgs.Value?.ToString() is { Length: > 0 } orderBy)
            await OnSortChanged.InvokeAsync(orderBy);
    }
}
