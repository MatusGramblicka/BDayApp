using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

public partial class PageSizeDropDown
{
    [Parameter]
    public EventCallback<int> SelectedPageSize { get; set; }

    private async Task OnPageSizeChange(ChangeEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs));

        if (eventArgs.Value is null)
            throw new ArgumentNullException(nameof(eventArgs.Value));

        await SelectedPageSize.InvokeAsync(int.Parse(eventArgs.Value.ToString()));
    }
}