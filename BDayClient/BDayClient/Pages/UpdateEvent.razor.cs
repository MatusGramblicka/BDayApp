using BDayClient.Http;
using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class UpdateEvent
{
    // Null while loading: the form is only rendered once there is an event to edit.
    private EventForUpdate? _eventForUpdate;
    private bool _loadFailed;
    private Guid? _loadedId;

    [Inject] public IEventHttpRepository EventRepo { get; set; } = default!;

    [Inject] public IToastService ToastService { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    [Parameter] public Guid Id { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedId == Id)
            return;

        _loadedId = Id;
        _eventForUpdate = null;
        _loadFailed = false;

        try
        {
            var eventDto = await EventRepo.GetEvent(Id);
            if (eventDto is null)
            {
                NavigationManager.NavigateTo("/404");
                return;
            }

            _eventForUpdate = EventForUpdate.FromDto(eventDto);
        }
        catch (ApiException ex)
        {
            _loadFailed = true;
            ToastService.ShowApiError(ex);
        }
    }

    private async Task Update()
    {
        if (_eventForUpdate is null)
            return;

        var eventForUpdateDto = _eventForUpdate.ToDto();

        try
        {
            await EventRepo.UpdateEvent(Id, eventForUpdateDto);
        }
        catch (ApiException ex)
        {
            ToastService.ShowApiError(ex);
            return;
        }

        ToastService.ShowSuccess($"Event \"{eventForUpdateDto.Name}\" was updated.");

        // The saved values are the new starting point: the form counts as untouched until the next change.
        _eventForUpdate = _eventForUpdate.Copy();
    }
}
