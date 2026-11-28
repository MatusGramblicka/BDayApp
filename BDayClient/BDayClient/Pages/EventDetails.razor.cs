using BDayClient.Http;
using BDayClient.HttpRepository;
using Blazored.Toast.Services;
using Contracts.DataTransferObjects.Event;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class EventDetails
{
    // Null while loading, so no placeholder card is shown before the data arrives.
    private EventDto? _event;
    private bool _loadFailed;
    private Guid? _loadedId;

    [Inject]
    public IEventHttpRepository EventRepo { get; set; } = default!;

    [Inject]
    public IToastService ToastService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ApiUrls ApiUrls { get; set; } = default!;

    [Parameter]
    public Guid Id { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedId == Id)
            return;

        _loadedId = Id;
        _event = null;
        _loadFailed = false;

        try
        {
            _event = await EventRepo.GetEvent(Id);
            if (_event is null)
                NavigationManager.NavigateTo("/404");
        }
        catch (ApiException ex)
        {
            _loadFailed = true;
            ToastService.ShowApiError(ex);
        }
    }
}
