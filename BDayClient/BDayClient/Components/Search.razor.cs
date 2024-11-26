using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

public partial class Search
{
    private string SearchTerm { get; set; } = string.Empty;

    private Timer _timer = null!;

    [Parameter]
    public EventCallback<string> OnSearchChanged { get; set; }

    private void SearchChanged()
    {
        if (_timer is not null)
            _timer.Dispose();

        _timer = new Timer(OnTimerElapsed, null, 500, 0);
    }

    private void OnTimerElapsed(object sender)
    {
        OnSearchChanged.InvokeAsync(SearchTerm);

        _timer.Dispose();
    }
}