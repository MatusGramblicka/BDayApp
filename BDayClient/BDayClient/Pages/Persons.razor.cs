using BDayClient.Http;
using BDayClient.HttpRepository;
using Blazored.Toast.Services;
using Contracts.DataTransferObjects.Person;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class Persons : IDisposable
{
    // Null while loading, so "loading" and "no persons" look different.
    private List<PersonDto>? _persons;
    private MetaData _metaData = new();
    private bool _loadFailed;
    private CancellationTokenSource? _load;

    private readonly PersonParameters _personParameters = new();

    [Inject]
    public IPersonHttpRepository PersonRepo { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await GetPersons();
    }

    private async Task SelectedPage(int page)
    {
        _personParameters.PageNumber = page;
        await GetPersons();
    }

    private async Task GetPersons()
    {
        // A newer request (e.g. the next search term) cancels the one still running, so an older answer can never
        // arrive last and overwrite the newer result.
        _load?.Cancel();
        _load?.Dispose();
        var load = new CancellationTokenSource();
        _load = load;

        try
        {
            var pagingResponse = await PersonRepo.GetPersons(_personParameters, load.Token);

            _persons = pagingResponse.Items;
            _metaData = pagingResponse.MetaData;
            _loadFailed = false;
        }
        catch (OperationCanceledException) when (load.IsCancellationRequested)
        {
            // Superseded by a newer request.
        }
        catch (ApiException ex)
        {
            _loadFailed = _persons is null;
            ToastService.ShowApiError(ex);
        }
    }

    private async Task SetPageSize(int pageSize)
    {
        _personParameters.PageSize = pageSize;
        _personParameters.PageNumber = 1;

        await GetPersons();
    }

    private async Task SearchChanged(string searchTerm)
    {
        _personParameters.PageNumber = 1;
        _personParameters.SearchTerm = searchTerm;

        await GetPersons();
    }

    private async Task SortChanged(string orderBy)
    {
        _personParameters.OrderBy = orderBy;

        await GetPersons();
    }

    private async Task DeletePerson(Guid id)
    {
        var person = _persons?.FirstOrDefault(p => p.Id == id);

        try
        {
            await PersonRepo.DeletePerson(id);
        }
        catch (ApiException ex)
        {
            ToastService.ShowApiError(ex);
            return;
        }

        ToastService.ShowSuccess(person is null ? "The person was deleted." : $"Person \"{person.Name}\" was deleted.");

        if (_personParameters.PageNumber > 1 && _persons?.Count == 1)
            _personParameters.PageNumber--;

        await GetPersons();
    }

    public void Dispose()
    {
        _load?.Cancel();
        _load?.Dispose();
        _load = null;
    }
}
