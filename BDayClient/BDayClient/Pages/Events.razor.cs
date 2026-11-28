using BDayClient.Http;
using BDayClient.HttpRepository;
using Blazored.Toast.Services;
using Contracts.DataTransferObjects.Event;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class Events : IDisposable
{
    // Null while loading, so "loading" and "no events" look different.
    private List<EventDto>? _events;
    private MetaData _metaData = new();
    private bool _loadFailed;
    private CancellationTokenSource? _load;

    private readonly EventParameters _eventParameters = new() { OrderBy = "date asc" };

    [Inject]
    public IEventHttpRepository EventRepo { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await GetEvents();
    }

    private async Task SelectedPage(int page)
    {
        _eventParameters.PageNumber = page;
        await GetEvents();
    }

    private async Task GetEvents()
    {
        // A newer request (e.g. the next search term) cancels the one still running, so an older answer can never
        // arrive last and overwrite the newer result.
        _load?.Cancel();
        _load?.Dispose();
        var load = new CancellationTokenSource();
        _load = load;

        try
        {
            var pagingResponse = await EventRepo.GetEvents(_eventParameters, load.Token);

            _events = pagingResponse.Items;
            _metaData = pagingResponse.MetaData;
            _loadFailed = false;
        }
        catch (OperationCanceledException) when (load.IsCancellationRequested)
        {
            // Superseded by a newer request.
        }
        catch (ApiException ex)
        {
            _loadFailed = _events is null;
            ToastService.ShowApiError(ex);
        }
    }

    private async Task SetPageSize(int pageSize)
    {
        _eventParameters.PageSize = pageSize;
        _eventParameters.PageNumber = 1;

        await GetEvents();
    }

    private async Task SearchChanged(string searchTerm)
    {
        _eventParameters.PageNumber = 1;
        _eventParameters.SearchTerm = searchTerm;

        await GetEvents();
    }

    private async Task SortChanged(string orderBy)
    {
        _eventParameters.OrderBy = orderBy;

        await GetEvents();
    }

    private async Task DeleteEvent(Guid id)
    {
        var deleted = _events?.FirstOrDefault(e => e.Id == id);

        try
        {
            await EventRepo.DeleteEvent(id);
        }
        catch (ApiException ex)
        {
            ToastService.ShowApiError(ex);
            return;
        }

        ToastService.ShowSuccess(deleted is null ? "The event was deleted." : $"Event \"{deleted.Name}\" was deleted.");

        if (_eventParameters.PageNumber > 1 && _events?.Count == 1)
            _eventParameters.PageNumber--;

        await GetEvents();
    }

    public void Dispose()
    {
        _load?.Cancel();
        _load?.Dispose();
        _load = null;
    }
}
