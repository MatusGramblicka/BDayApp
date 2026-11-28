using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

public partial class PageSizeDropDown
{
    [Parameter]
    public EventCallback<int> SelectedPageSize { get; set; }

    private async Task OnPageSizeChange(ChangeEventArgs eventArgs)
    {
        if (int.TryParse(eventArgs.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                out var pageSize) && pageSize > 0)
            await SelectedPageSize.InvokeAsync(pageSize);
    }
}
