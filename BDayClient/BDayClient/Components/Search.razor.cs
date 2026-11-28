using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

/// <summary>A search box that raises <see cref="OnSearchChanged"/> once typing has paused for <see cref="DebounceDelay"/>.</summary>
public partial class Search : IDisposable
{
    public static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(500);

    private CancellationTokenSource? _debounce;

    [Parameter]
    public string Placeholder { get; set; } = "Search by person name";

    private string SearchTerm { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> OnSearchChanged { get; set; }

    // Runs after every change of the input (typing, pasting, autofill, clearing); only the last one within the delay searches.
    private async Task SearchTermChanged()
    {
        _debounce?.Cancel();
        _debounce?.Dispose();

        var debounce = new CancellationTokenSource();
        _debounce = debounce;

        try
        {
            await Task.Delay(DebounceDelay, debounce.Token);
        }
        catch (OperationCanceledException)
        {
            return; // superseded by a newer change, or the component was disposed
        }

        // The delay can finish just before a newer change cancels it; then the newer change searches.
        if (!ReferenceEquals(debounce, _debounce))
            return;

        await OnSearchChanged.InvokeAsync(SearchTerm);
    }

    public void Dispose()
    {
        _debounce?.Cancel();
        _debounce?.Dispose();
        _debounce = null;
    }
}
