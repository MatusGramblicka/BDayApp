using BDayClient.Http;
using BDayClient.HttpRepository;
using Blazored.Toast.Services;
using Contracts.DataTransferObjects.Person;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class PersonDetails
{
    // Null while loading, so no placeholder card is shown before the data arrives.
    private PersonDto? _person;
    private bool _loadFailed;
    private Guid? _loadedId;

    [Inject]
    public IPersonHttpRepository PersonRepo { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ApiUrls ApiUrls { get; set; } = default!;

    [Parameter]
    public Guid PersonId { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedId == PersonId)
            return;

        _loadedId = PersonId;
        _person = null;
        _loadFailed = false;

        try
        {
            _person = await PersonRepo.GetPerson(PersonId);
            if (_person is null)
                NavigationManager.NavigateTo("/404");
        }
        catch (ApiException ex)
        {
            _loadFailed = true;
            ToastService.ShowApiError(ex);
        }
    }
}
