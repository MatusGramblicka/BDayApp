using BDayClient.HttpInterceptor;
using BDayClient.HttpRepository;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class Persons : IDisposable
{
    public List<Person> PersonList { get; set; } = new();
    public MetaData MetaData { get; set; } = new();

    public PersonParameters _personParameters = new();

    [Inject]
    public IPersonHttpRepository PersonRepo { get; set; }

    [Inject]
    public HttpInterceptorService Interceptor { get; set; }

    private bool _alreadyDisposed;

    protected override async Task OnInitializedAsync()
    {
        Interceptor.RegisterEvent();
        Interceptor.RegisterBeforeSendEvent();
        await GetPersons();
    }

    private async Task SelectedPage(int page)
    {
        _personParameters.PageNumber = page;
        await GetPersons();
    }

    private async Task GetPersons()
    {
        var pagingResponse = await PersonRepo.GetPersons(_personParameters);

        PersonList = pagingResponse.Items;
        MetaData = pagingResponse.MetaData;
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
        await PersonRepo.DeletePerson(id);

        if (_personParameters.PageNumber > 1 && PersonList.Count == 1)
            _personParameters.PageNumber--;

        await GetPersons();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_alreadyDisposed)
            return;

        if (disposing)
        {
            Interceptor.DisposeEvent();
            _alreadyDisposed = true;
        }
    }

    ~Persons()
    {
        Dispose(disposing: false);
    }
}