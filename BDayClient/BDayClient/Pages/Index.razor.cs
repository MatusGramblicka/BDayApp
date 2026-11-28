using BDayClient.Http;
using BDayClient.HttpRepository;
using Blazored.Toast.Services;
using Contracts.DataTransferObjects.Person;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class Index
{
    // Null while loading.
    private List<PersonDto>? _persons;
    private bool _loadFailed;

    [Inject]
    public IPersonHttpRepository PersonRepo { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // All of them: upcoming birthdays can be on any page of the list.
            _persons = await PersonRepo.GetAllPersons();
        }
        catch (ApiException ex)
        {
            _loadFailed = true;
            ToastService.ShowApiError(ex);
        }
    }
}
